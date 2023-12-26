using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public abstract class TBBSEffectScriptBase : MonoBehaviour
    {
        public abstract bool IsDone { get; }
        public BattlerController CurrentBattler { get; set; }
        public BattlerController TargetBattler { get; set; }
    }

    public class TBBSEffectWaitScript : TBBSEffectScriptBase
    {
        public float TimeToWait = 1f;

        public override bool IsDone => Elapsed >= TimeToWait;

        private float Elapsed = 0;

        private void Update()
        {
            if(Elapsed < TimeToWait)
            {
                Elapsed += Time.deltaTime;
            }
        }
    }
}