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

        public virtual void Init(TBBSSceneController sceneController)
        {

        }

        public abstract void PlayAnimation(string animation, Action completeCallback, BattlerAnimationArgs args);
        //public abstract void StopAnimation();

        public abstract void SetIdleAnimation(string animation, BattlerAnimationArgs args);

        public abstract Vector3 GetOverlayPoint();
        public abstract Vector3 GetTargetPoint();
    }    
}