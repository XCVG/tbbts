using CommonCore.StringSub;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{

    public abstract class BattleAction
    {
        protected BattleContext Context;

        public virtual bool IsReorderable { get; }
        public virtual int Priority { get; }

        public virtual void Start(BattleContext context)
        {
            Context = context;
            Debug.Log($"Start BattleAction->{GetType().Name} (priority: {Priority})");
        }

        public virtual void Update()
        {
            //nop
        }

        protected IEnumerator CoBattlerDeathSequence(ParticipantData participant, BattlerController battler)
        {
            Debug.LogWarning($"{participant.Name} death sequence (not implemented)");
            //TODO
            yield break;
        }

    }

    public class FleeAction : BattleAction
    {

    }

    public abstract class BaseAttackAction : BattleAction
    {
        public string AttackingParticipant { get; set; }
        public string DefendingParticipant { get; set; }
        public int AttackPriority { get; set; }

        public override bool IsReorderable => true;

        public override int Priority => AttackPriority; //I don't like this, we'll see if it lasts
    }

    public class GuardAction : BattleAction
    {
        public string GuardingParticipant { get; set; }

        public override int Priority => int.MaxValue;
        public override bool IsReorderable => true;

        //TODO play animation on battler, add Guard condition to guarding participant

        public override void Start(BattleContext context)
        {
            base.Start(context);

            context.SceneController.StartCoroutine(CoDoGuard());
        }

        private IEnumerator CoDoGuard()
        {
            var participantData = Context.SceneController.ParticipantData[GuardingParticipant];
            var battler = Context.SceneController.Battlers[GuardingParticipant];

            Context.UIController.HideOverlay();
            Context.UIController.ShowMessage(participantData.DisplayName + " guards!");

            bool animDone = false;
            battler.PlayAnimation("Guard", () => { animDone = true; }, new BattlerAnimationArgs()
            {
                AnimationTimescale = 1
            });
            while (!animDone)
                yield return null;

            yield return null;
            Context.UIController.ClearMessage();
            Context.UIController.ShowOverlay();
            Context.UIController.RepaintOverlay();
            Context.CompleteCallback();
        }

        private void AddConditionToParticipant()
        {
            Context.SceneController.ParticipantData[GuardingParticipant].Conditions.Add(new TBBSGuardCondition());
        }
    }

    //TODO probably move this over to another file
    //and, like, y'know, implement it
    public class SimpleAttackAction : BaseAttackAction
    {
        public string Move { get; set; }

        private MoveDefinition MoveDefinition;

        public override void Start(BattleContext context)
        {
            base.Start(context);

            Debug.Log($"attacker: {AttackingParticipant} | target: {DefendingParticipant} | move: {Move}");

            //breaks and iunno why
            MoveDefinition = context.SceneController.MoveDefinitions[Move];
            context.SceneController.StartCoroutine(CoDoSimpleAttack());
        }

        private IEnumerator CoDoSimpleAttack()
        {
            Debug.Log("CoDoSimpleAttack");

            var attackingParticipant = Context.SceneController.ParticipantData[AttackingParticipant];
            var attackingBattler = Context.SceneController.Battlers[AttackingParticipant];

            //get defending participant, if applicable
            IList<TargetData> targets = new List<TargetData>();
            if (!string.IsNullOrEmpty(DefendingParticipant) && (MoveDefinition.Target == MoveTarget.SingleAlly || MoveDefinition.Target == MoveTarget.SingleEnemy || MoveDefinition.Target == MoveTarget.SingleParticipant))
            {
                //WIP if defender is already dead handling
                //TODO move this into a method
                var defendingParticipant = Context.SceneController.ParticipantData.GetOrDefault(DefendingParticipant);
                if(defendingParticipant == null || defendingParticipant.Health <= 0)
                {
                    if(MoveDefinition.AlreadyDeadAction == MoveAlreadyDeadAction.Retarget)
                    {
                        Debug.Log("Retargeting SimpleAttackAction because defending participant is dead");
                        // find a new target based on targeting rules
                        List<KeyValuePair<string, ParticipantData>> targetableEnemies = GetTargetableEnemySet(attackingParticipant).ToList();

                        if (targetableEnemies.Count == 0)
                        {
                            Debug.Log("Skipping SimpleAttackAction because no participant could be found to retarget to");

                            Context.CompleteCallback();
                            yield break;
                        }

                        var selected = targetableEnemies[UnityEngine.Random.Range(0, targetableEnemies.Count)];
                        defendingParticipant = selected.Value;
                        Debug.Log("Retargeted SimpleAttackAction to" + selected.Key);

                    }
                    else if(MoveDefinition.AlreadyDeadAction == MoveAlreadyDeadAction.Skip)
                    {
                        Debug.Log("Skipping SimpleAttackAction because defending participant is dead");

                        Context.CompleteCallback();
                        yield break;
                    }
                }
                targets.Add(CreateTargetData(defendingParticipant));
            }            
            else if (MoveDefinition.Target == MoveTarget.AllEnemies || MoveDefinition.Target == MoveTarget.AllAllies || MoveDefinition.Target == MoveTarget.AllParticipants)
            {                
                var defendingParticipants = GetTargetableEnemySet(attackingParticipant, !MoveDefinition.HasFlag(MoveFlag.ApplyGroupAttackOnDeadTargets), !MoveDefinition.HasFlag(MoveFlag.ApplyGroupAttackOnNotarget));
                foreach(var defendingParticipant in defendingParticipants)
                {
                    targets.Add(CreateTargetData(defendingParticipant.Value));
                }

                if(targets.Count == 0)
                {
                    Debug.Log("Skipping SimpleAttackAction because all possible participants in group are dead or untargetable");
                    Context.CompleteCallback();
                    yield break;
                }
            }
            else if (MoveDefinition.Target == MoveTarget.Self)
            {
                targets.Add(CreateTargetData(attackingParticipant));
            }

            //show message based on lookup from term to TBBS_TERMS
            string term = Sub.Replace(string.IsNullOrEmpty(MoveDefinition.Term) ? "attacks" : MoveDefinition.Term, "TBBS_TERMS");
            string message = $"{attackingParticipant.DisplayName} {term}!";

            Context.UIController.HideOverlay();
            Context.UIController.ShowMessage(message);

            //use mana points if applicable
            attackingParticipant.Magic -= MoveDefinition.MagicUse;

            int numRepeats = 1; //TODO get this from 
            for(int i = 0; i < numRepeats; i++)
            {
                //TODO will need to handle repeats eventually but will get to that later
                //repeats will happen approximately here, after target is selected but before damage is calculated                

                if(targets.Count == 0 || (!MoveDefinition.HasFlag(MoveFlag.ApplyGroupAttackOnDeadTargets) && !targets.Any(t => t.IsAlive)))
                {
                    Debug.Log($"Aborting on {i} repeat because all targets in group are all dead or nonexistent!");
                    break;
                }

                if(i > 0 && (MoveDefinition.Target == MoveTarget.Self || MoveDefinition.Target == MoveTarget.SingleAlly || MoveDefinition.Target == MoveTarget.SingleEnemy || MoveDefinition.Target == MoveTarget.SingleParticipant) && (targets.Count == 0 || !targets[0].IsAlive) && !MoveDefinition.HasFlag(MoveFlag.RepeatOnDeadTarget))
                {
                    Debug.Log($"Aborting on {i} repeat because target is dead!");
                    break;
                }

                //calculate damage, even for dead targets
                foreach (var target in targets)
                {
                    target.PendingDamage = TBBSUtils.CalculateDamage(MoveDefinition, attackingParticipant, target.Participant);
                    if (MoveDefinition.HasFlag(MoveFlag.IsHealingMove))
                        target.PendingDamage = Mathf.Abs(target.PendingDamage) * -1;
                }

                //WIP signal battler to play animation (calculate target points based on move definition options here)
                bool playEffectAtMidpoint = MoveDefinition.HasFlag(MoveFlag.PlayEffectAtMidpoint);
                bool animateMotion = false;
                Vector3 animTargetPos = Vector3.zero;
                switch (MoveDefinition.MotionHint)
                {
                    //utilize GetAttackOffsetVector (maybe different for hittarget and jumphittarget) 
                    case MoveMotionHint.JumpHitTarget:
                    case MoveMotionHint.HitTarget:
                        //we may eventually give options for group attack target
                        if(targets.Count >= 1)
                        {
                            animTargetPos = targets[0].Battler.GetTargetPoint() + (targets[0].Battler.transform.position - targets[0].Battler.GetTargetPoint());
                            animTargetPos += Vector3.Scale(attackingBattler.GetAttackOffsetVector(), new Vector3(1, 0, 1)); 
                            animateMotion = true;
                        }
                        break;
                    case MoveMotionHint.JumpInPlace:
                        animTargetPos = attackingBattler.transform.position + (Vector3.up * 1f);
                        animateMotion = true;
                        break;
                }
                bool animDone = false;
                attackingBattler.PlayAnimation(MoveDefinition.Animation, () => { animDone = true; }, new BattlerAnimationArgs()
                {
                    AnimationTimescale = MoveDefinition.AnimationTimescale,
                    InitialEffect = MoveDefinition.AttackEffect,
                    LateEffect = playEffectAtMidpoint ? MoveDefinition.HitEffect : "",
                    SoundEffect = MoveDefinition.SoundEffect,
                    PlayEffectAtMidpoint = playEffectAtMidpoint,
                    AnimateMotion = animateMotion,
                    TargetPosition = animTargetPos
                    //TODO pass flag for JumpHitTarget
                });
                while (!animDone) //wait for battler animation to finish
                    yield return null;

                StringBuilder endMessage = new StringBuilder();
                int startedAnimationCount = 0, completedAnimationCount = 0;
                foreach(var target in targets)
                {
                    if (!target.IsAlive && !MoveDefinition.HasFlag(MoveFlag.ApplyGroupAttackOnDeadTargets))
                        continue;

                    float previousHealth = target.Participant.Health;

                    //apply damage
                    target.Participant.Health -= target.PendingDamage;

                    if(previousHealth > 0 && target.Participant.Health <= 0)
                    {
                        target.KilledDuringAction = true;
                        target.PendingDeathAnimation = true;
                    }

                    //TODO hit/react animation should probably move here and be handled by a call to defending battler(s)

                    //end message (x took y damage) ?
                    endMessage.AppendFormat("{0} took {1:F0} damage\n", target.Participant.DisplayName, target.PendingDamage);

                    target.PendingDamage = 0;
                }

                //display end message and wait for (all?) hit anim to complete
                //TODO should we show overlay here?
                Context.UIController.ShowMessage(endMessage.ToString());

                while (startedAnimationCount > completedAnimationCount)
                    yield return null;

                //TODO probably fixed or variable hold time for effects as well

                //handle dead participants
                foreach(var target in targets)
                {
                    if(target.PendingDeathAnimation)
                    {
                        yield return CoBattlerDeathSequence(target.Participant, target.Battler);
                        target.PendingDeathAnimation = false;
                    }
                }

                Context.UIController.ClearMessage();

                // need to handle MoveRepeatType and MoveAlreadyDeadAction after all (only for single-target moves!)
                if (numRepeats > 1 && (MoveDefinition.RepeatType == MoveRepeatType.RandomTarget || MoveDefinition.RepeatType == MoveRepeatType.DifferentTarget || (MoveDefinition.AlreadyDeadAction == MoveAlreadyDeadAction.Retarget && MoveDefinition.RepeatType == MoveRepeatType.SameTarget && !targets[0].IsAlive)) && MoveDefinition.Target == MoveTarget.SingleAlly || MoveDefinition.Target == MoveTarget.SingleEnemy || MoveDefinition.Target == MoveTarget.SingleParticipant)
                {
                    var targetableEnemies = GetTargetableEnemySet(attackingParticipant).ToList();

                    if(MoveDefinition.RepeatType == MoveRepeatType.DifferentTarget && targetableEnemies.Count > 1)
                    {
                        int lastTargetIndex = targetableEnemies.FindIndex(kvp => targets[0].Participant.Name == kvp.Key);
                        if (lastTargetIndex >= 0)
                            targetableEnemies.RemoveAt(lastTargetIndex);
                    }

                    var selected = targetableEnemies[UnityEngine.Random.Range(0, targetableEnemies.Count)];
                    targets[0] = CreateTargetData(selected.Value);
                    Debug.Log("Retargeted SimpleAttackAction to" + selected.Key + " for repeat");
                }

                yield return null;
            }

            yield return null;

            Context.UIController.ClearMessage();
            Context.UIController.ShowOverlay();
            Context.UIController.RepaintOverlay();            
            Context.CompleteCallback();
        }

        private TargetData CreateTargetData(string participantName)
        {
            var participant = Context.SceneController.ParticipantData.GetOrDefault(participantName);
            var battler = Context.SceneController.Battlers[participantName];

            return new TargetData() { Battler = battler, Participant = participant };
        }

        private TargetData CreateTargetData(ParticipantData participant)
        {
            var battler = Context.SceneController.Battlers[participant.Name];

            return new TargetData() { Battler = battler, Participant = participant };
        }

        private IEnumerable<KeyValuePair<string, ParticipantData>> GetTargetableEnemySet(ParticipantData attackingParticipant, bool mustBeAlive = true, bool mustBeTargetable = true)
        {
            IEnumerable<KeyValuePair<string, ParticipantData>> targetableEnemies;
            if (MoveDefinition.Target == MoveTarget.SingleEnemy || MoveDefinition.Target == MoveTarget.AllEnemies)
            {
                BattleParticipant.ControlledByType enemyControlledByType = attackingParticipant.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player ? BattleParticipant.ControlledByType.AI : BattleParticipant.ControlledByType.Player;

                targetableEnemies = Context.SceneController.ParticipantData
                           .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == enemyControlledByType);
            }
            else if (MoveDefinition.Target == MoveTarget.SingleAlly || MoveDefinition.Target == MoveTarget.AllAllies)
            {
                targetableEnemies = Context.SceneController.ParticipantData
                           .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == attackingParticipant.BattleParticipant.ControlledBy);
            }
            else
            {
                targetableEnemies = Context.SceneController.ParticipantData;
            }

            if (mustBeAlive)
                targetableEnemies = targetableEnemies.Where(kvp => kvp.Value.Health > 0);

            if (mustBeTargetable)
                targetableEnemies = targetableEnemies.Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting));

            return targetableEnemies;
        }

        private class TargetData
        {
            public ParticipantData Participant { get; set; }
            public BattlerController Battler { get; set; }
            public bool IsAlive => Participant.Health > 0;

            public float PendingDamage { get; set; }            
            public bool PendingDeathAnimation { get; set; }
            public bool KilledDuringAction { get; set; }
            
        }
    }

    public class ConditionUpdateAction : BattleAction
    {
        public override void Start(BattleContext context)
        {
            base.Start(context);

            Debug.Log("ConditionUpdateAction");

            var activeParticipants = Context.SceneController.ParticipantData.Where(kvp => kvp.Value.Health > 0).ToList();
            foreach(var participant in activeParticipants)
            {
                var conditions = participant.Value.Conditions;
                for(int i = conditions.Count - 1 ; i >= 0; i--)
                {
                    var condition = conditions[i] as TBBSConditionBase;
                    if(condition != null)
                    {
                        condition.ElapsedTurns++;
                        if(condition.RemoveAfterNumTurns == -1 || (condition.RemoveAfterNumTurns > 0 && condition.ElapsedTurns > condition.RemoveAfterNumTurns))
                        {
                            conditions.RemoveAt(i);
                            //TODO would be nice to show messages for at least some conditions wearing off but it's not critical
                        }
                    }                    
                }
            }

            context.UIController.RepaintOverlay(); //force overlay repaint because conditions have changed

            context.CompleteCallback(); //safe-ish
        }

    }

}
