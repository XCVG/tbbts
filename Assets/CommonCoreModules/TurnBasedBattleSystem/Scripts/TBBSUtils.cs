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
    }
}