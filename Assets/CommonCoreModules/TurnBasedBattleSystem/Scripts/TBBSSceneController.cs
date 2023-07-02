using CommonCore.LockPause;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSSceneController : BaseSceneController
    {
        [Header("References"), SerializeField]
        private TBBSUIController UIController =  null;

        public GameObject Stage { get; private set; }
        public Dictionary<string, BattlerController> Battlers { get; private set; } = new Dictionary<string, BattlerController>();

        public Dictionary<string, BattlerData> BattlerData { get; private set; } = new Dictionary<string, BattlerData>();
        public List<BattleAction> ActionQueue { get; private set; } = new List<BattleAction>();

        public int TurnCount { get; private set; }

        public BattleDefinition BattleDefinition { get; private set; }
        private BattleAction CurrentAction;

        //TODO this will need to be a state machine. Possibly to handle "decision phase" vs "action phase" but certainly to handle Intro->Battle->Outro
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
        }

        private void SignalPlayerGetActionsComplete()
        {
            //TODO move to AI stage, possibly trigger some scripting, then begin next action
            Debug.LogWarning("SignalPlayerGetActionsComplete");
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
                var bd = new BattlerData()
                {
                    BattleParticipant = participant.Value
                    //TODO more later
                };
                BattlerData.Add(participant.Key, bd);
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
            foreach(var bd in BattlerData)
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

    }

    public class BattlerData
    {
        public BattleParticipant BattleParticipant { get; set; }

        //I think CharacterViewModel/BattlerData is redundant and we only need one or the other, but not 100% decided on that yet
        //ie we could just put the stuff we would have put there in here, since we have to apply manually anyway
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


