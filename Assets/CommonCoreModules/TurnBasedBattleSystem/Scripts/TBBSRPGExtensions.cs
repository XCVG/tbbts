using CommonCore.RpgGame.Rpg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PseudoExtensibleEnum;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.State
{
    public partial class GameState
    {
        /// <summary>
        /// [TBBS] Character models for the rest of the party
        /// </summary>
        public Dictionary<string, CharacterModel> Party { get; private set; } = new Dictionary<string, CharacterModel>();

        [Init(-1)]
        private void TBBSInit()
        {
            try
            {
                //load party
                var rawResource = CoreUtils.LoadResource<TextAsset>("Data/RPGDefs/init_party");
                if(rawResource != null)
                {
                    var jData = CoreUtils.ReadJson(rawResource.text) as JObject;
                    foreach(var item in jData)
                    {
                        var cm = new CharacterModel();
                        using (var sr = item.Value.CreateReader())
                        {
                            JsonSerializer.Create(new JsonSerializerSettings
                            {
                                Converters = CCJsonConverters.Defaults.Converters,
                                TypeNameHandling = TypeNameHandling.Auto,
                                NullValueHandling = NullValueHandling.Ignore
                            }).Populate(sr, cm);
                        }
                        InventoryModel.AssignUIDs(cm.Inventory.EnumerateItems(), true);
                        cm.UpdateStats();
                        Party.Add(item.Key, cm);
                    }
                }                
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load initial party");
                Debug.LogException(e);
            }
        }
    }
}

namespace CommonCore.TurnBasedBattleSystem
{
    [PseudoExtend(typeof(StatType))]
    public enum TBBSStatType
    {
        Attack = 11,
        Defence = 12,
        SpecialAttack = 13,
        SpecialDefence = 14,
        Agility = 15,
        Luck = 16
    }
}