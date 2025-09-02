using CommonCore.RpgGame.Rpg;
using CommonCore.State;
using CommonCore.World;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace CommonCore.TurnBasedBattleSystem
{
    public static class TBBSUtils
    {
        public static readonly string CharacterModelResourcePath = "Data/TurnBasedBattles/CharacterModels";
        public static readonly string CharacterModelMovesetKey = "TBBSMoveset";
        public static readonly string CharacterModelTargetingPolicyKey = "TBBSTargetingPolicy";
        public static readonly string MoveModelResourcePath = "Data/TurnBasedBattles/Moves";

        public static BattleDefinition GetBattleDefinitionFromMetaState()
        {
            if (MetaState.Instance.GameData.TryGetValue(BattleDefinition.DefaultBattleDefinitionKey, out object rawValue) && rawValue is BattleDefinition bd)
                return bd;

            return null;
        }

        public static void SaveBattleDefinitionToMetaState(BattleDefinition battleDefinition)
        {
            MetaState.Instance.GameData[BattleDefinition.DefaultBattleDefinitionKey] = battleDefinition;
        }

        public static CharacterMoveset GetMoveset(this CharacterModel characterModel)
        {
            if(characterModel.ExtraData.TryGetValue(CharacterModelMovesetKey, out object rawCollection) && rawCollection is CharacterMoveset ms)
            {
                return ms;
            }

            return new CharacterMoveset() { Moves = new List<CharacterMoveEntry>()
                {
                    new CharacterMoveEntry() { Move = "Attack", Weight = 1},
                    new CharacterMoveEntry() { Move = "Guard", Weight = 1}
                }
            };
        }

        public static ParticipantTargetingPolicy GetTargetingPolicy(this CharacterModel characterModel)
        {
            if (characterModel.ExtraData.TryGetValue(CharacterModelTargetingPolicyKey, out object rawValue) && Enum.TryParse(typeof(ParticipantTargetingPolicy), rawValue.ToString(), out object parsedResult))
            {
                return (ParticipantTargetingPolicy)parsedResult;
            }

            return ParticipantTargetingPolicy.Random;
        }

        public static CharacterModel LoadCharacterModel(string name)
        {
            string fullPath = CharacterModelResourcePath + "/" + name;
            var jsonData = CoreUtils.LoadResource<TextAsset>(fullPath).text;

            var cm = new CharacterModel();
            JsonConvert.PopulateObject(jsonData, cm, new JsonSerializerSettings
            {
                Converters = CCJsonConverters.Defaults.Converters,
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            });
            
            //TODO this will need to have something like "remapUIDs" since we can create multiple instances of the same character model now
            InventoryModel.AssignUIDs(cm.Inventory.EnumerateItems(), true);

            cm.UpdateStats();

            return cm;
        }

        //used for testing only, will remove or rework
        public static BattleDefinition GenerateDefaultBattleDefinition()
        {
            var bd = new BattleDefinition()
            {
                CommitCharacterModelsAtEnd = false,
                GameOverIfBattleLost = true,
                Stage = "TestStage",
                Participants = new Dictionary<string, BattleParticipant>()
                {
                    { "PlayerBattleParticipant", 
                        new BattleParticipant()
                        {
                            Battler = "TestBattler",
                            CharacterModelSource = BattleParticipant.CharacterModelSourceType.FromPlayer,
                            ControlledBy = BattleParticipant.ControlledByType.Player,
                            BattlerPosition = new Vector3(-3f, 0, 0),
                            BattlerRotation = new Vector3(0, 90, 0),
                            DisplayName = "Player"
                        } 
                    },
                    { "EnemyBattleParticipant",
                        new BattleParticipant()
                        {
                            Battler = "TestBattler",
                            CharacterModelSource = BattleParticipant.CharacterModelSourceType.InitializeNew,
                            CharacterModelName = "TestCharacterModel",
                            ControlledBy = BattleParticipant.ControlledByType.AI,
                            BattlerPosition = new Vector3(1f, 0, -2f),
                            BattlerRotation = new Vector3(0, -90, 0),
                            DisplayName = "Enemy1"
                        }
                    },
                    { "EnemyBattleParticipant2",
                        new BattleParticipant()
                        {
                            Battler = "TestBattler",
                            CharacterModelSource = BattleParticipant.CharacterModelSourceType.InitializeNew,
                            CharacterModelName = "TestCharacterModel",
                            ControlledBy = BattleParticipant.ControlledByType.AI,
                            BattlerPosition = new Vector3(4f, 0, 2f),
                            BattlerRotation = new Vector3(0, -90, 0),
                            DisplayName = "Enemy2"
                        }
                    }
                }
            };

            return bd;
        }

        public static Dictionary<string, MoveDefinition> GetMoveDefinitions()
        {
            var moves = new Dictionary<string, MoveDefinition>();

            //add default moves
            moves.Add("Attack", new MoveDefinition()
            {
                Name = "Attack",
                Animation = "Attack",
                SoundEffect = "GenericAttackSwing",
                HitEffect = "DefaultHit",
                Power = 1,
                Randomness = 0.2f,
                Speed = 0,
                DamageCalculation = MoveDamageCalculation.Normal,
                Target = MoveTarget.SingleEnemy,
                RepeatType = MoveRepeatType.Single,
                Flags = new List<MoveFlag>() { MoveFlag.ContinueAnimationFromMidpoint, MoveFlag.UseTargetHitPuff },
                MotionHint = MoveMotionHint.HitTarget,
                AlreadyDeadAction = MoveAlreadyDeadAction.Retarget                
            });

            moves.Add("Guard", new MoveDefinition()
            {
                Name = "Guard",
                Animation = "Guard",
                SoundEffect = "GenericGuardSwing",
                HitEffect = null,
                Power = 0,
                Speed = 0,
                Target = MoveTarget.Self,
                RepeatType = MoveRepeatType.Single,
                Flags = new List<MoveFlag>() {  MoveFlag.IsGuardMove }
            });

            //load external moves
            try
            {
                var moveJson = CoreUtils.LoadResource<TextAsset>(MoveModelResourcePath);
                var moveData = CoreUtils.LoadJson<Dictionary<string, MoveDefinition>>(moveJson.text);
                foreach(var kvp in moveData)
                {
                    kvp.Value.Name = kvp.Key;
                }
                moves.AddRangeReplaceExisting(moveData);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load move definitions from jason");
                Debug.LogException(ex);
            }
            

            return moves;
        }

        public static float CalculateDamage(MoveDefinition move, ParticipantData attacker, ParticipantData defender)
        {
            switch (move.DamageCalculation)
            {
                case MoveDamageCalculation.None:
                    return 0;
                case MoveDamageCalculation.IgnoreDR:
                case MoveDamageCalculation.IgnoreAllDefense:
                case MoveDamageCalculation.Normal:
                    {
                        float attack = attacker.Stats[move.HasFlag(MoveFlag.UseSpecialAttack) ? TBBSStatType.SpecialAttack : TBBSStatType.Attack];
                        float defence = 0;
                        if(move.DamageCalculation != MoveDamageCalculation.IgnoreAllDefense)
                            defence = defender.Stats[move.HasFlag(MoveFlag.UseSpecialDefense) ? TBBSStatType.SpecialDefence : TBBSStatType.Defence];

                        //Debug.Log($"attack: {attack:F2} | defence: {defence:F2} | power: {move.Power:F2} | randomness: {move.Randomness:F2}");

                        if(move.DamageCalculation != MoveDamageCalculation.IgnoreDR && move.DamageCalculation != MoveDamageCalculation.IgnoreAllDefense)
                        {
                            float defenceFromDR = defender.DamageResistance.GetOrDefault(move.DamageType, 0);
                            defence += defenceFromDR;
                        }

                        if (defender.Conditions.Any(c => c is TBBSGuardCondition))
                            defence *= 2f;

                        float power = move.Power * (1 + UnityEngine.Random.Range(-move.Randomness, move.Randomness));
                        float damage = (power * attack * 4) - (defence * 2);

                        return Mathf.Max(1, damage);
                    }
                case MoveDamageCalculation.ExactPower:
                    return move.Power;                   
            }

            throw new NotImplementedException();
        }

        public static TBBSConditionBase CreateConditionInstance(string condition)
        {
            TBBSConditionBase conditionInstance = null;
            try
            {
                Type type = Type.GetType(condition); //may need to extend this later
                if(type == null)
                {
                    type = Type.GetType("CommonCore.TurnBasedBattleSystem." + condition);
                }
                //roll through loaded types twice: once with exact match, once with endswith?
                //CCBase.AddonManager.EnumerateAddonAssemblies();
                /*
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
                {
                    var tt = assembly.GetType(name);
                    if (tt != null)
                    {
                        return tt;
                    }
                }
                */
                conditionInstance = (TBBSConditionBase)Activator.CreateInstance(type);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
            return conditionInstance;
        }

        public static void SpawnBattleParticipantWithEffect(TBBSSceneController sceneController, string participantName, BattleParticipant battleParticipant, string effect, Action callback)
        {
            sceneController.AddParticipant(participantName, battleParticipant);

            var battler = sceneController.Battlers[participantName];

            TBBSEffectScriptBase waitScript = null;
            if (!string.IsNullOrEmpty(effect))
            {
                var targetPos = battler.GetTargetPoint();
                var effectGO = WorldUtils.SpawnEffect(effect, targetPos, Quaternion.identity, null, true);
                if (effectGO != null)
                {
                    waitScript = effectGO.GetComponent<TBBSEffectScriptBase>();
                    if (waitScript != null)
                    {
                        waitScript.Init(battler, battler);
                    }
                }
            }

            if(waitScript != null && callback != null)
            {
                sceneController.StartCoroutine(waitForEffect());
            }
            else
            {
                callback?.Invoke();
            }

            IEnumerator waitForEffect()
            {
                while(waitScript != null && !waitScript.IsDone)
                {
                    yield return null;
                }

                callback();
            }
        }
    }
}