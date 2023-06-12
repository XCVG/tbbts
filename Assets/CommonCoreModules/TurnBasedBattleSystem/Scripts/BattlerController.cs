using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{

    /// <summary>
    /// This will eventually control a battler, providing interfaces to animation etc
    /// </summary>
    public abstract class BattlerController : MonoBehaviour
    {

        public abstract void PlayAnimation(string animation, Action completeCallback, BattlerAnimationArgs args);
        //public abstract void StopAnimation();

        public abstract void SetIdleAnimation(string animation, BattlerAnimationArgs args);

    }    
}