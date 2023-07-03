using CommonCore.RpgGame.Rpg;
using CommonCore.State;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public static class TBBSUtils
    {
        public static readonly string CharacterModelResourcePath = "Data/TurnBasedBattles/CharacterModels";
        public static readonly string CharacterModelMovesetKey = "TBBSMoveset";

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

        public static IReadOnlyList<string> GetMoveset(this CharacterModel characterModel)
        {
            if(characterModel.ExtraData.TryGetValue(CharacterModelMovesetKey, out object rawCollection))
            {
                if(rawCollection is IEnumerable<string> enumerableCollection)
                {
                    return enumerableCollection
                        .Distinct()
                        .OrderBy(k => k)
                        .ToList();
                }
            }

            return new List<string>() { "Attack", "Guard" };
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