using CommonCore.State;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public static class TBBSConsoleCommands
    {
        [Command(useClassName = false)]
        public static void StartBattle(string battle)
        {
            string battleData = CoreUtils.LoadResource<TextAsset>("Data/TurnBasedBattles/BattleDefinitions/" + battle).text;
            var battleDefinition = CoreUtils.LoadJson<BattleDefinition>(battleData);
            MetaState.Instance.GameData[BattleDefinition.DefaultBattleDefinitionKey] = battleDefinition;
            SharedUtils.ChangeScene("TBBSBattleScene");
        }
    }
}


