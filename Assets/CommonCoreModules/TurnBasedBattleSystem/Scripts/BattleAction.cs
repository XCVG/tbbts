using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            //TODO calculate damage (noting that this may be healing)
            //ie call TBBSUtils.CalculateDamage for each victim

            //TODO show message based on lookup from term to TBBS_TERMS

            Context.UIController.HideOverlay();

            //TODO will need to handle repeats eventually but will get to that later

            //TODO signal battler to play animation (calculate target points based on move definition options here)
            //also handle playing of the sound effect here

            //TODO wait for battler animation to finish

            //TODO apply damage (noting that it may affect multiple participants)

            yield break;

            Context.UIController.ClearMessage();
            Context.UIController.ShowOverlay();
            Context.UIController.RepaintOverlay();            
            Context.CompleteCallback();
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
        }

    }

}
