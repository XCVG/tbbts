using CommonCore.State;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public static class TBBSUtils
    {
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
                            BattlerPosition = new Vector3(3f, 0, 0),
                            BattlerRotation = new Vector3(0, -90, 0),
                            DisplayName = "Enemy1"
                        }
                    }
                }
            };

            return bd;
        }
    }
}