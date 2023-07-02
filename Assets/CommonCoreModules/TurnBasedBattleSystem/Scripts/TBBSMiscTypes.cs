using CommonCore.RpgGame.Rpg;
using CommonCore.RpgGame.State;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{

    /// <summary>
    /// Context of a battle passed into Actions
    /// </summary>
    public class BattleContext
    {
        //fast references to scene controller, UI controller, etc
        public TBBSSceneController SceneController { get; set; }
        public TBBSUIController UIController { get; set; }

        //I think these will all be accessible through SceneController, but prefer these accessors because convenience and API stability
        public IList<BattleAction> ActionQueue { get; set; }
        public int TurnCount { get; set; }

        //call this to complete the action and return control back to the scene controller
        public Action CompleteCallback { get; set; }
        
    }

    /// <summary>
    /// Context of a battle passed into PromptPlayerAndGetActions
    /// </summary>
    /*
    public class GetActionsContext
    {
        public IList<BattleAction> ActionQueue { get; set; }
        public int TurnCount { get; set; }
        public BattleDefinition BattleDefinition { get; set; }
        public Action CompleteCallback { get; set; }
    }
    */
    //decided not to go with this, at least not yet

    /// <summary>
    /// Arguments passed to battler for animations
    /// </summary>
    public class BattlerAnimationArgs
    {
        public Vector3 Target { get; set; }

    }

    /// <summary>
    /// Defines everything needed to set up a battle
    /// </summary>
    public class BattleDefinition
    {
        public const string DefaultBattleDefinitionKey = "TBBSBattleDefinition";

        public string Stage { get; set; }

        public IDictionary<string, BattleParticipant> Participants { get; set; } = new Dictionary<string, BattleParticipant>();

        public bool CommitCharacterModelsAtEnd { get; set; }
        public bool GameOverIfBattleLost { get; set; }

        public IList<MicroscriptNode> WinMicroscript { get; set; }
        public IList<MicroscriptNode> LoseMicroscript { get; set; }

        //will need to be set explicitly by caller for "return to last scene"
        public string NextScene { get; set; }

        //TODO specify escape allowance here

    }

    public class BattleParticipant
    {
        public CharacterModelSourceType CharacterModelSource { get; set; }
        public string CharacterModelName { get; set; }

        public ControlledByType ControlledBy { get; set; }

        public string Battler { get; set; }
        public Vector3 BattlerPosition { get; set; }
        public Vector3 BattlerRotation { get; set; } //xyz oiler angles

        public string DisplayName { get; set; }

        public enum CharacterModelSourceType
        {
            InitializeNew, FromParty, FromPlayer
        }

        public enum ControlledByType
        {
            None, Player, AI
        }
    }


    public class CharacterViewModel
    {
        public CharacterModel CharacterModel { get; set; }
    }



}