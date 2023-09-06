using CommonCore.LockPause;
using CommonCore.RpgGame.Rpg;
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

        public int TurnCount { get; private set; }

        public BattleDefinition BattleDefinition { get; private set; }
        private BattleAction CurrentAction;
        private bool CurrentActionStarted = false;

        private BattlePhase CurrentPhase;
        private DecisionSubPhase CurrentDecisionSubPhase;

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
                    //TODO handle pre-condition-check
                    CurrentDecisionSubPhase = DecisionSubPhase.PlayerInput;
                    UIController.PromptPlayerAndGetActions(SignalPlayerGetActionsComplete);
                    break;
                case BattlePhase.Action:
                    if(!CurrentActionStarted && CurrentAction != null)
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
            bool advanced = false;
            UIController.ShowMessage("Battle Start!", () =>
            {
                advanced = true;
            });
            while(!advanced)
            {
                yield return null;
            }
            EnterPhase(BattlePhase.Decision);
        }

        private IEnumerator CoOutro()
        {
            yield return null;
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

        private void DoDecisionPhaseAI()
        {
            //TODO all the AI
            Debug.Log("DoDecisionPhaseAI");

            var aiParticipants = ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.AI).ToList();
            foreach(var p in aiParticipants)
            {
                //will probably go with a very simple random chance at least initially
            }

        }

        private void DoDecisionPhaseReorder()
        {
            Debug.Log("DoDecisionPhaseReorder");

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


