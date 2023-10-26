using CommonCore.Audio;
using CommonCore.RpgGame.Rpg;
using CommonCore.RpgGame.State;
using CommonCore.World;
using Newtonsoft.Json;
using PseudoExtensibleEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public Vector3 TargetPosition { get; set; }

        //public string Animation { get; set; }
        public float AnimationTimescale { get; set; } = 1f;

        public string InitialEffect { get; set; }
        public string LateEffect { get; set; }

        public string SoundEffect { get; set; }

        public bool AnimateMotion { get; set; }
        public bool PlayEffectAtMidpoint { get; set; }

        public Action MidpointCallback { get; set; }

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

    public enum ParticipantTargetingPolicy
    {
        Random,
        PreferPlayer,
        PreferWeakest,
        PreferStrongest
    }

    //effectively a view model
    public class ParticipantData
    {
        public string Name { get; set; }

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

        public CharacterMoveset MoveSet { get; set; } //should probably be private/protected set
        public ParticipantTargetingPolicy TargetingPolicy { get; set; }
        public IReadOnlyDictionary<TBBSStatType, float> Stats { get; set; } //should probably be private/protected set?
        public IReadOnlyDictionary<int, float> DamageResistance { get; set; }

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

            MoveSet = CharacterModel.GetMoveset();
            //Debug.Log(MoveSet.ToDetailString());
            TargetingPolicy = CharacterModel.GetTargetingPolicy();
            //Debug.Log(TargetingPolicy);

            //copy TBBS stats
            var stats = new Dictionary<TBBSStatType, float>();
            foreach (int i in Enum.GetValues(typeof(TBBSStatType)))
            {                
                if(CharacterModel.DerivedStats.Stats.TryGetValue(i, out int statVal))
                {
                    stats[(TBBSStatType)i] = statVal;
                }
                else
                {
                    stats[(TBBSStatType)i] = 1; //default is 1, because reasons
                }                
            }
            Stats = stats;

            DamageResistance = new Dictionary<int, float>(CharacterModel.DerivedStats.DamageResistance);

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

    // data block generated by preconditioncheck on battle end
    public class BattleEndData
    {
        public bool PlayerWon { get; set; }
    }

    //scripting context objects

    public class TBBSOnPreOutroContext
    {
        public BattleEndData BattleEndData { get; set; }
        public bool SkipDefaultHandling { get; set; }
        public string MessageOverride { get; set; }
        public string NextSceneOverride { get; set; }
    }

    public enum MoveTarget
    {
        None,
        Self,
        SingleEnemy,
        SingleAlly,
        SingleParticipant,
        AllEnemies,
        AllAllies,
        AllParticipants
    }

    public enum MoveRepeatType
    {
        Single,
        SameTarget,
        RandomTarget,
        DifferentTarget
    }

    public enum MoveFlag
    {
        Unknown,
        PrecedeAll, //for quick attacks etc
        AnimateFirstOnly, //if set, will not animate repeats
        UseSpecialAttack,
        UseSpecialDefense, //uses special defense OF TARGET to calculate damage ON TARGET
        IsHealingMove,
        IsGuardMove,
        PlayEffectAtMidpoint,
        ContinueAnimationFromMidpoint,
        RepeatOnDeadTarget,
        ApplyGroupAttackOnDeadTargets,
        ApplyGroupAttackOnNotarget,
        SkipPainAnimation
    }

    public enum MoveDamageCalculation
    {
        None,
        Normal, //max(1, 4 * power * atk - (guarding ? 4 : 2) * def)
        IgnoreDR,
        ExactPower
    }

    public enum MoveAlreadyDeadAction
    {
        Continue,
        Retarget,
        Skip
    }

    public enum MoveMotionHint
    {
        None = 0,
        HitTarget,
        JumpInPlace,
        JumpHitTarget
    }

    public enum MoveHitEffectPositionHint
    {
        Target = 0,
        CenterStage,
        CenterTargetGroup
    }

    public class MoveDefinition
    {
        //hot modifying this is possible since it doesn't use private set, but I'm too lazy to write a constructor and initial set isn't supported in Unity
        //but we reload moves for every battle so it doesn't matter much anyway

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public MoveTarget Target { get; set; }

        //repeat data
        [JsonProperty]
        public MoveRepeatType RepeatType { get; set; }
        [JsonProperty]
        public int RepeatCount { get; set; }        

        [JsonProperty]
        public float Power { get; set; }
        [JsonProperty]
        public float Randomness { get; set; } //possibly will vary based on DamageCalculation but generally proportional to Power
        [JsonProperty]
        public float Speed { get; set; }
        [JsonProperty]
        public float MagicUse { get; set; }
        [JsonProperty, JsonConverter(typeof(PxEnumConverter), typeof(DefaultDamageTypes))]
        public int DamageType { get; set; }

        [JsonProperty]
        public IList<MoveFlag> Flags { get; set; }

        [JsonProperty]
        public MoveDamageCalculation DamageCalculation { get; set; }
        [JsonProperty]
        public MoveAlreadyDeadAction AlreadyDeadAction { get; set; }

        //presentation data (stringly typed garbage)
        [JsonProperty]
        public string Animation { get; set; }
        [JsonProperty]
        public float AnimationTimescale { get; set; } = 1f;
        [JsonProperty]
        public MoveMotionHint MotionHint { get; set; }
        [JsonProperty]
        public string AttackEffect { get; set; }
        [JsonProperty]
        public string HitEffect { get; set; }
        [JsonProperty]
        public MoveHitEffectPositionHint HitEffectPositionHint { get; set; }
        [JsonProperty]
        public string SoundEffect { get; set; }        
        [JsonProperty]
        public string Term { get; set; }

        [JsonProperty]
        public IDictionary<string, object> ExtraData { get; set; }

        public bool HasFlag(MoveFlag flag)
        {
            return (Flags != null) && Flags.Contains(flag);
        }

    }

    public class CharacterMoveset
    {
        [JsonProperty]
        public IList<CharacterMoveEntry> Moves { get; set; }

        public string ToDetailString()
        {
            return string.Join(',', Moves.Select(m => $"{{{m.Move}}}"));
        }
    }

    public enum CharacterMoveFlag
    {
        Unknown = 0,
        OnlyIfHpBelowThreshold,
        OnlyIfHpAboveThreshold,
        IsSpecial
    }

    public class CharacterMoveEntry
    {
        [JsonProperty]
        public string Move { get; set; }
        [JsonProperty]
        public float Weight { get; set; } = 1.0f;
        [JsonProperty]
        public IList<CharacterMoveFlag> Flags { get; set; }
        [JsonProperty]
        public float Threshold { get; set; } //depends on flags

        public bool HasFlag(CharacterMoveFlag flag)
        {
            return (Flags != null) && Flags.Contains(flag);
        }
    }

}