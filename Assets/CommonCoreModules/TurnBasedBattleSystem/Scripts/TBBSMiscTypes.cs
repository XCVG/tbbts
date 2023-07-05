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
        public bool ShowOverlay { get; set; } = true;

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

    //effectively a view model
    public class ParticipantData
    {
        public CharacterModel CharacterModel { get; set; }
        public BattleParticipant BattleParticipant { get; set; }

        //it's all awkward as shit because I changed my mind on how this was supposed to work about 3 times

        public string DisplayName { get; set; }

        //I think CharacterViewModel/BattlerData is redundant and we only need one or the other, but not 100% decided on that yet
        //ie we could just put the stuff we would have put there in here, since we have to apply manually anyway

        //"consumable" stats block
        public float Energy { get; set; }
        public float MaxEnergy { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Shields { get; set; }
        public float MaxShields { get; set; }
        public float Magic { get; set; }
        public float MaxMagic { get; set; }

        //TODO other stats, like agility/attack/etc from DerivedStats?

        public IReadOnlyList<string> Moves { get; set; } //should probably be private/protected set
        public IReadOnlyDictionary<TBBSStatType, float> Stats { get; set; } //should probably be private/protected set?

        public List<Condition> Conditions { get; set; } = new List<Condition>();

        public void LoadValuesFromCharacterModel()
        {
            Energy = CharacterModel.Energy;
            MaxEnergy = CharacterModel.DerivedStats.MaxEnergy;
            Health = CharacterModel.Health;
            MaxHealth = CharacterModel.DerivedStats.MaxHealth;
            Shields = CharacterModel.Shields;
            MaxShields = CharacterModel.DerivedStats.ShieldParams.MaxShields;
            Magic = CharacterModel.Magic;
            MaxMagic = CharacterModel.DerivedStats.MaxMagic;

            Moves = CharacterModel.GetMoveset();

            //copy TBBS stats
            foreach(int i in Enum.GetValues(typeof(TBBSStatType)))
            {
                var stats = new Dictionary<TBBSStatType, float>();
                if(CharacterModel.DerivedStats.Stats.TryGetValue(i, out int statVal))
                {
                    stats[(TBBSStatType)i] = statVal;
                }
                else
                {
                    stats[(TBBSStatType)i] = 1; //default is 1, because reasons
                }
                Stats = stats;
            }

            Conditions.AddRange(CharacterModel.Conditions);

            //TODO we may want to bring damage types into TBBS at some point
            //TODO we will eventually have to handle inventory and conditions here
        }

        public void SaveValuesToCharacterModel()
        {
            CharacterModel.Energy = Energy;
            CharacterModel.Health = Health;
            CharacterModel.Shields = Shields;
            CharacterModel.Magic = Magic;

            //TODO we will eventually have to handle inventory and conditions here

            CharacterModel.Conditions.Clear();
            CharacterModel.Conditions.AddRange(Conditions);
        }

    }

}