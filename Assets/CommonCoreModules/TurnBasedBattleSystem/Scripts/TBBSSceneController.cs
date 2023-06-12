using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSSceneController : BaseSceneController
    {
        [Header("References"), SerializeField]
        private TBBSUIController UIController =  null;


        public Dictionary<string, BattlerController> Battlers { get; private set; } = new Dictionary<string, BattlerController>();
        public List<BattleAction> ActionQueue { get; private set; } = new List<BattleAction>();

        private BattleAction CurrentAction;

        //TODO this will need to be a state machine. Possibly to handle "decision phase" vs "action phase" but certainly to handle Intro->Battle->Outro

        public override void Start()
        {
            base.Start();

            //TODO get battle definition from metastate (or default?)
            var battleDefinition = TBBSUtils.GetBattleDefinitionFromMetaState();

            //TODO kickoff battle start process
            //TODO will need to load battle setup/battle definition from somewhere
            //TODO eventually need to create stage from prefab here
            //TODO load or (for now) link up battlers
        }

        public override void Update()
        {
            base.Update();

            CurrentAction?.Update();

            //TODO pass control to UI then AI if action queue is empty
        }

        private BattleContext CreateContext()
        {
            return new BattleContext()
            {
                SceneController = this,
                UIController = UIController,
                ActionQueue = ActionQueue,
                CompleteCallback = SignalActionComplete
                //TODO others?
            };
        }

        private void SignalActionComplete()
        {
            //TODO move to next action
        }

        private void SignalPlayerGetActionsComplete()
        {
            //TODO move to AI stage, possibly trigger some scripting, then begin next action
        }

        //TODO script hooks everywhere!
        
    }

    public enum BattlePhase
    {
        Intro, Decision, Action, Outro
    }

    public enum DecisionSubPhase
    {
        PreConditionCheck,
        PlayerInput,
        AI,
        Reorder
    }
}


