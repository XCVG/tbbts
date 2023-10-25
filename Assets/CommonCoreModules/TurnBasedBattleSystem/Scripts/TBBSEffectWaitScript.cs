using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public abstract class TBBSEffectWaitScriptBase : MonoBehaviour
    {
        public abstract bool IsDone { get; }
    }

    public class TBBSEffectWaitScript : TBBSEffectWaitScriptBase
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