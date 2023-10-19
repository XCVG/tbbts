using CommonCore.LockPause;
using CommonCore.RpgGame.Rpg;
using CommonCore.Scripting;
using CommonCore.State;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSSceneController : BaseSceneController
    {
        [Header("References"), SerializeField]
        private TBBSUIController UIController =  null;

        public GameObject Stage { get; private set; }
        public Dictionary<string, BattlerController> Battlers { get; private set; } = new Dictionary<string, BattlerController>();

        public Dictionary<string, ParticipantData> ParticipantData { get; private set; } = new Dictionary<string, ParticipantData>();
        public List<BattleAction> ActionQueue { get; private set; } = new List<BattleAction>();

        public Dictionary<string, MoveDefinition> MoveDefinitions { get; private set; } = new Dictionary<string, MoveDefinition>();

        public int TurnCount { get; private set; }

        public BattleDefinition BattleDefinition { get; private set; }
        private BattleAction CurrentAction;
        private bool CurrentActionStarted = false;

        private BattlePhase CurrentPhase;
        private DecisionSubPhase CurrentDecisionSubPhase;

        private BattleEndData BattleEndData;

        public override void Awake()
        {
            if (!CCBase.Initialized) //should maybe move this up to base class logic?
            {
                enabled = false;
                return;
            }

            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            MoveDefinitions = TBBSUtils.GetMoveDefinitions();

            //get battle definition from metastate
            BattleDefinition = TBBSUtils.GetBattleDefinitionFromMetaState();

            if(BattleDefinition == null)
            {
                BattleDefinition = TBBSUtils.GenerateDefaultBattleDefinition();
            }
            LoadBattlerData();

            SpawnStage();
            SpawnBattlers();
            CurrentPhase = BattlePhase.Intro;

            //TODO should probably set MenuGameStateLocked

            StartCoroutine(CoIntro());
        }

        public override void Update()
        {
            base.Update();

            if (LockPauseModule.IsPaused())
                return;

            UpdatePhase();
        }

        private BattleContext CreateContext()
        {
            return new BattleContext()
            {
                SceneController = this,
                UIController = UIController,
                ActionQueue = ActionQueue,
                CompleteCallback = SignalActionComplete,
                TurnCount = TurnCount
                //TODO others?
            };
        }

        private void SignalActionComplete()
        {
            //TODO move to next action
            Debug.LogWarning("SignalActionComplete");

            CurrentActionStarted = false;
            CurrentAction = null;

            if (ActionQueue.Count > 0)
            {
                CurrentAction = ActionQueue[0];
                ActionQueue.RemoveAt(0);
            }
            else
            {
                //TODO go to next phase
                throw new NotImplementedException();
            }            

        }

        private void SignalPlayerGetActionsComplete()
        {
            //TODO move to AI stage, possibly trigger some scripting, then begin next action
            Debug.LogWarning("SignalPlayerGetActionsComplete");
            CurrentDecisionSubPhase = DecisionSubPhase.AI;
        }

        private void EnterPhase(BattlePhase newPhase)
        {
            Debug.Log("Entering battle phase: " + newPhase);

            //TODO enter next phase, doing initial setup or whatever
            switch (newPhase)
            {
                case BattlePhase.Undefined:
                    break;
                case BattlePhase.Intro:
                    break;
                case BattlePhase.Decision:
                    CurrentDecisionSubPhase = DecisionSubPhase.PreConditionCheck;
                    if(DoDecisionPhasePreConditionCheck())
                    {
                        CurrentDecisionSubPhase = DecisionSubPhase.PlayerInput;
                        UIController.PromptPlayerAndGetActions(SignalPlayerGetActionsComplete);
                    }                    
                    break;
                case BattlePhase.Action:
                    if(ActionQueue.Count == 0)
                    {
                        throw new NotImplementedException("no actions in queue at beginning of action phase!");
                    }
                    else if(CurrentAction == null)
                    {
                        CurrentAction = ActionQueue[0];
                        ActionQueue.RemoveAt(0);
                    }

                    //if(!CurrentActionStarted && CurrentAction != null)
                    //{
                    //    CurrentAction.Start(CreateContext());
                    //    CurrentActionStarted = true;
                    //}
                    break;
                case BattlePhase.Outro:
                    StartCoroutine(CoOutro());
                    break;
                default:
                    break;
            }

            CurrentPhase = newPhase;
        }

        private void UpdatePhase()
        {
            switch (CurrentPhase)
            {
                case BattlePhase.Undefined:
                    break;
                case BattlePhase.Intro:
                    break;
                case BattlePhase.Decision:
                    if(CurrentDecisionSubPhase == DecisionSubPhase.AI)
                    {
                        DoDecisionPhaseAI();
                        CurrentDecisionSubPhase = DecisionSubPhase.Reorder;
                    }
                    else if(CurrentDecisionSubPhase == DecisionSubPhase.Reorder)
                    {
                        DoDecisionPhaseReorder();
                        EnterPhase(BattlePhase.Action);
                    }
                    break;
                case BattlePhase.Action:
                    CurrentAction?.Update();
                    if (!CurrentActionStarted && CurrentAction != null)
                    {
                        CurrentAction.Start(CreateContext());
                        CurrentActionStarted = true;
                    }
                    break;
                case BattlePhase.Outro:
                    break;
                default:
                    break;
            }
        }

        //TODO script hooks everywhere!

        //intro and outro handling (currently hardcoded)
        private IEnumerator CoIntro()
        {
            yield return null;

            yield return UIController.ShowMessageAndWait("Battle Start!");

            foreach(var battler in Battlers.Values)
            {
                battler.SetIdleAnimation("Idle", new BattlerAnimationArgs());
            }

            EnterPhase(BattlePhase.Decision);
        }

        private IEnumerator CoOutro()
        {
            yield return null;

            var ctx = new TBBSOnPreOutroContext()
            {
                BattleEndData = BattleEndData
            };
            ScriptingModule.CallNamedHooked("TBBSOnPreOutro", this, ctx);
            bool didShowCustomMessage = false;
            if(!string.IsNullOrEmpty(ctx.MessageOverride))
            {
                yield return UIController.ShowMessageAndWait(ctx.MessageOverride);
                didShowCustomMessage = true;
            }

            if(!ctx.SkipDefaultHandling)
            {
                if (BattleEndData.PlayerWon)
                {
                    if (!didShowCustomMessage)
                        yield return UIController.ShowMessageAndWait("Congratulations! You have won the battle!");
                    if (BattleDefinition.WinMicroscript != null && BattleDefinition.WinMicroscript.Count > 0)
                    {
                        foreach (var ms in BattleDefinition.WinMicroscript)
                        {
                            ms.Execute();
                        }
                    }
                }
                else
                {
                    if (!didShowCustomMessage)
                        yield return UIController.ShowMessageAndWait("You lost!");
                    if (BattleDefinition.LoseMicroscript != null && BattleDefinition.LoseMicroscript.Count > 0)
                    {
                        foreach (var ms in BattleDefinition.LoseMicroscript)
                        {
                            ms.Execute();
                        }
                    }
                    if (BattleDefinition.GameOverIfBattleLost)
                    {
                        SharedUtils.ShowGameOver();
                        yield break;
                    }
                }

                if (BattleDefinition.CommitCharacterModelsAtEnd)
                {
                    foreach (var participant in ParticipantData)
                    {
                        switch (participant.Value.BattleParticipant.CharacterModelSource)
                        {
                            case BattleParticipant.CharacterModelSourceType.FromParty:
                            case BattleParticipant.CharacterModelSourceType.FromPlayer:
                                //this should "just work", I think
                                Debug.Log("Saving values to character model for " + participant.Key);
                                participant.Value.SaveValuesToCharacterModel();
                                break;
                        }

                    }
                }
            }            

            SharedUtils.ChangeScene(ctx.NextSceneOverride ?? BattleDefinition.NextScene);
        }
        
        //setup stuff below

        private void LoadBattlerData()
        {
            foreach(var participant in BattleDefinition.Participants)
            {
                Debug.Log("Adding battle participant: " + participant.Key);
                //get characterModel and load stats
                CharacterModel characterModel;
                switch (participant.Value.CharacterModelSource)
                {
                    case BattleParticipant.CharacterModelSourceType.InitializeNew:
                        characterModel = TBBSUtils.LoadCharacterModel(participant.Value.CharacterModelName);
                        break;
                    case BattleParticipant.CharacterModelSourceType.FromParty:
                        characterModel = GameState.Instance.Party[participant.Value.CharacterModelName];
                        break;
                    case BattleParticipant.CharacterModelSourceType.FromPlayer:
                        characterModel = GameState.Instance.PlayerRpgState;
                        break;
                    default:
                        characterModel = new CharacterModel(); //probably not safe
                        break;
                }
                var bd = new ParticipantData()
                {
                    CharacterModel = characterModel,
                    BattleParticipant = participant.Value,
                    DisplayName = participant.Value.DisplayName ?? characterModel.DisplayName
                };
                bd.LoadValuesFromCharacterModel();
                ParticipantData.Add(participant.Key, bd);
                Debug.Log("Added battle participant: " + participant.Key);
            }
        }

        //TODO error handling wow

        private void SpawnStage()
        {
            if(!string.IsNullOrEmpty(BattleDefinition.Stage))
            {
                var stagePrefab = CoreUtils.LoadResource<GameObject>("TurnBasedBattles/Stages/" + BattleDefinition.Stage);
                Stage = GameObject.Instantiate(stagePrefab, CoreUtils.GetWorldRoot());
                Debug.Log($"Spawned stage (prefab: {BattleDefinition.Stage})");
            }
        }

        private void SpawnBattlers()
        {
            foreach(var bd in ParticipantData)
            {
                var battlerPrefab = CoreUtils.LoadResource<GameObject>("TurnBasedBattles/Battlers/" + bd.Value.BattleParticipant.Battler);
                var battler = GameObject.Instantiate(battlerPrefab, bd.Value.BattleParticipant.BattlerPosition, Quaternion.Euler(bd.Value.BattleParticipant.BattlerRotation), CoreUtils.GetWorldRoot());
                battler.name = bd.Key;
                var bc = battler.GetComponent<BattlerController>();
                bc.Init(this);
                Battlers.Add(bd.Key, bc);
                Debug.Log($"Spawned battler {bd.Key} (prefab: {bd.Value.BattleParticipant.Battler})");
            }
        }

        //decision phase handlers

        private bool DoDecisionPhasePreConditionCheck()
        {
            Debug.Log("DoDecisionPhasePreConditionCheck");

            //at least for PoC, we only need to check two conditions:
            //-if there are no player-controlled battlers with health > 0, lose battle
            bool playerStillAlive = ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                .Where(kvp => kvp.Value.Health > 0)
                .Any();
            if(!playerStillAlive)
            {
                BattleEndData = new BattleEndData() { PlayerWon = false };
                EnterPhase(BattlePhase.Outro);
                return false;
            }
            //-if there are no ai-controlled battlers with health > 0, win battle
            bool aiStillAlive = ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.AI)
                .Where(kvp => kvp.Value.Health > 0)
                .Any();
            if (!aiStillAlive)
            {
                BattleEndData = new BattleEndData() { PlayerWon = true };
                EnterPhase(BattlePhase.Outro);
                return false;
            }

            return true; //return true to continue decision phase as normal
        }

        private void DoDecisionPhaseAI()
        {
            var aiParticipants = ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.AI)
                .Where(kvp => kvp.Value.Health > 0)
                .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockActions))
                .ToList();
            foreach(var p in aiParticipants)
            {
                //will probably go with a very simple random chance at least initially
                Debug.Log($"Participant {p.Key} choosing action");

                //use move weights to decide move...
                IList<CharacterMoveEntry> moves = new List<CharacterMoveEntry>(p.Value.MoveSet.Moves);

                //limit moveset based on flags and p's HP threshold
                for(int i = moves.Count - 1; i >= 0; i--)
                {
                    var move = moves[i];
                    if(move.HasFlag(CharacterMoveFlag.OnlyIfHpAboveThreshold))
                    {
                        if (p.Value.Health < move.Threshold)
                            moves.RemoveAt(i);
                    }
                    else if(move.HasFlag(CharacterMoveFlag.OnlyIfHpBelowThreshold))
                    {
                        if (p.Value.Health > move.Threshold)
                            moves.RemoveAt(i);
                    }
                }

                //Debug.Log(moves.ToNiceString(m => m.Move));
                double[] cumulative = new double[moves.Count];
                double currTotal = 0;
                for(int i = 0; i < cumulative.Length; i++)
                {
                    currTotal += moves[i].Weight;
                    cumulative[i] = currTotal;
                }
                //Debug.Log(cumulative.ToNiceString());
                double value = CoreUtils.Random.NextDouble() * cumulative[cumulative.Length - 1];
                int index = 0;
                for(; index < cumulative.Length; index++)
                {
                    if (cumulative[index] >= value)
                        break;
                }
                var cMove = moves[index];

                if(cMove.Move.Equals("Guard", StringComparison.OrdinalIgnoreCase))
                {
                    ActionQueue.Add(new GuardAction() { GuardingParticipant = p.Key });
                    Debug.Log($"Participant {p.Key} chose to Guard");
                    continue;
                }

                //...and participant character model targeting policy to decide target
                string targetParticipant = null;
                var moveData = MoveDefinitions[cMove.Move];
                //Debug.Log(moveData.Target);
                switch (moveData.Target)
                {
                    case MoveTarget.Self:
                        targetParticipant = p.Key;
                        break;
                    case MoveTarget.SingleEnemy:
                        {
                            string preferredTarget = null;
                            //attempt to get preferred target
                            //Debug.Log(p.Value.TargetingPolicy);
                            switch (p.Value.TargetingPolicy)
                            {
                                case ParticipantTargetingPolicy.PreferPlayer:
                                    var player = ParticipantData
                                        .Where(kvp => kvp.Value.CharacterModel.IsPlayer)
                                        .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                                        .Where(kvp => kvp.Value.Health > 0)
                                        .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                                        .Select(kvp => kvp.Key)
                                        .FirstOrDefault();
                                    if (!string.IsNullOrEmpty(player))
                                        preferredTarget = player;
                                    break;
                                case ParticipantTargetingPolicy.PreferWeakest:
                                    var weakest = ParticipantData
                                        .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                                        .Where(kvp => kvp.Value.Health > 0)
                                        .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                                        .OrderBy(kvp => kvp.Value.Health)
                                        .Select(kvp => kvp.Key)
                                        .FirstOrDefault();
                                    if (!string.IsNullOrEmpty(weakest))
                                        preferredTarget = weakest;
                                    break;
                                case ParticipantTargetingPolicy.PreferStrongest:
                                    var strongest = ParticipantData
                                        .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                                        .Where(kvp => kvp.Value.Health > 0)
                                        .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                                        .OrderBy(kvp => kvp.Value.Health)
                                        .Reverse()
                                        .Select(kvp => kvp.Key)
                                        .FirstOrDefault();
                                    if (!string.IsNullOrEmpty(strongest))
                                        preferredTarget = strongest;
                                    break;
                            }

                            if(preferredTarget == null)
                            {
                                var targetableEnemies = ParticipantData
                                   .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                                   .Where(kvp => kvp.Value.Health > 0)
                                   .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                                   .ToList();
                                //Debug.Log("targetable:" + targetableEnemies.ToNiceString(p => p.Key));
                                preferredTarget = targetableEnemies[UnityEngine.Random.Range(0,targetableEnemies.Count)].Key;
                            }

                            targetParticipant = preferredTarget;
                        }                        
                        break;
                    case MoveTarget.SingleAlly:
                        var targetableAllies = ParticipantData
                           .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.AI)
                           .Where(kvp => kvp.Value.Health > 0)
                           .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                           .Where(kvp => kvp.Key != p.Key)
                           .ToList();
                        targetParticipant = targetableAllies[CoreUtils.Random.Next(targetableAllies.Count)].Key;
                        break;
                    case MoveTarget.SingleParticipant:
                        var targetableParticipants = ParticipantData
                           .Where(kvp => kvp.Value.Health > 0)
                           .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                           .Where(kvp => kvp.Key != p.Key)
                           .ToList();
                        targetParticipant = targetableParticipants[CoreUtils.Random.Next(targetableParticipants.Count)].Key;
                        break;
                }

                //actually create action
                var action = new SimpleAttackAction() { 
                    AttackingParticipant = p.Key, 
                    DefendingParticipant =  targetParticipant,
                    AttackPriority = (int)p.Value.Stats.GetOrDefault(TBBSStatType.Agility),
                    Move = cMove.Move
                };
                Debug.Log($"Participant {p.Key} attacking {targetParticipant} with {cMove.Move}");
                ActionQueue.Add(action);

            }

        }

        private void DoDecisionPhaseReorder()
        {
            Debug.Log("DoDecisionPhaseReorder");

            ScriptingModule.CallNamedHooked("TBBSOnPreReorder", this, ActionQueue);

            //reordered flee to the beginning (unless isReorderable is false)
            int fleeActionIdx = ActionQueue.FindIndex(a => a is  FleeAction);
            if(fleeActionIdx >= 0)
            {
                var fleeAction = ActionQueue[fleeActionIdx];
                ActionQueue.RemoveAt(fleeActionIdx);
                ActionQueue.Insert(0, fleeAction);
            }

            //reorder guard actions ahead of attack actions (unless isReorderable is false)
            //reorder guard actions by priority
            List<BattleAction> guardActions = new List<BattleAction>();
            int firstActionIdx = int.MaxValue;
            for(int i = ActionQueue.Count - 1; i >= 0; i--)
            {
                var action = ActionQueue[i];
                if(action is GuardAction && action.IsReorderable)
                {
                    guardActions.Insert(0, action);
                    ActionQueue.RemoveAt(i);
                    firstActionIdx = i;
                }
                if (action is BaseAttackAction)
                    firstActionIdx = i;
            }
            if(guardActions.Count > 0)
            {
                guardActions = guardActions.OrderByDescending(b => b.Priority).ToList();
                for(int i = 0; i < guardActions.Count;i++)
                {
                    ActionQueue.Insert(firstActionIdx + i, guardActions[i]);
                }
            }

            //reorder attack actions by priority
            List<BattleAction> attackActions = new List<BattleAction>();
            int firstAttackActionIdx = int.MaxValue;
            for (int i = ActionQueue.Count - 1; i >= 0; i--)
            {
                var action = ActionQueue[i];
                if (action is BaseAttackAction && action.IsReorderable)
                {
                    attackActions.Insert(0, action);
                    ActionQueue.RemoveAt(i);
                    firstAttackActionIdx = i;
                }
            }
            if(attackActions.Count > 0)
            {
                attackActions = attackActions.OrderByDescending(b => b.Priority).ToList();
                for (int i = 0; i < attackActions.Count; i++)
                {
                    ActionQueue.Insert(firstAttackActionIdx + i, attackActions[i]);
                }
            }

            //ensure a ConditionUpdateAction exists and is at the end
            int conditionUpdateActionIdx = ActionQueue.FindIndex(a => a is ConditionUpdateAction);
            if (conditionUpdateActionIdx >= 0)
            {
                var conditionUpdateAction = ActionQueue[conditionUpdateActionIdx];
                ActionQueue.RemoveAt(conditionUpdateActionIdx);
                ActionQueue.Add(conditionUpdateAction);
            }
            else
            {
                var conditionUpdateAction = new ConditionUpdateAction();
                ActionQueue.Add(conditionUpdateAction);
            }

            ScriptingModule.CallNamedHooked("TBBSOnPostReorder", this, ActionQueue);

        }

    }    

    public enum BattlePhase
    {
        Undefined,
        Intro,
        Decision,
        Action,
        Outro
    }

    public enum DecisionSubPhase
    {
        Undefined,
        PreConditionCheck,
        PlayerInput,
        AI,
        Reorder
    }
}


