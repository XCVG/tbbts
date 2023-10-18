using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    /// <summary>
    /// Just for testing for now
    /// </summary>
    public class TestBattlerController : BattlerController
    {
        public Transform OverlayPoint;
        public Transform TargetPoint;

        public override void PlayAnimation(string animation, Action completeCallback, BattlerAnimationArgs args)
        {

        }

        public override void SetIdleAnimation(string animation, BattlerAnimationArgs args)
        {

        }

        public override Vector3 GetOverlayPoint()
        {
            return OverlayPoint.position;
        }

        public override Vector3 GetTargetPoint()
        {
            return TargetPoint.position;
        }
    }

}
