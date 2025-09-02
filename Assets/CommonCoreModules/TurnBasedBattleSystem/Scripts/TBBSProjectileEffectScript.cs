using CommonCore.TurnBasedBattleSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSProjectileEffectScript : TBBSEffectScriptBase
    {
        public GameObject LaunchEffect = null;
        public float LaunchEffectTime = 5f;
        public GameObject FlyEffect = null;
        public float FlyEffectSpeed = 0;
        public float FlyEffectTime = 1f;
        public GameObject ExplodeEffect = null;
        public float ExplodeEffectTime = 5f;

        public bool AlignToFlyVector = true;

        public override bool IsDone => (State == TBBSProjectileEffectState.Finished);

        private TBBSProjectileEffectState State;

        public override void Init(BattlerController currentBattler, BattlerController targetBattler)
        {
            base.Init(currentBattler, targetBattler);

            if(FlyEffectSpeed > 0 && FlyEffectTime > 0)
            {
                Debug.LogWarning($"Projectile {name} has both FlyEffectSpeed and FlyEffectTime set, only FlyEffectTime will be used!");
            }

            StartCoroutine(CoRunEffect());
        }

        private IEnumerator CoRunEffect()
        {
            Vector3 targetPosition = TargetBattler.GetTargetPoint();
            Vector3 vecToTarget = (targetPosition - transform.position);
            Vector3 dirToTarget = vecToTarget.normalized;
            if (AlignToFlyVector)
            {
                transform.forward = dirToTarget;
            }

            if (LaunchEffect != null)
            {
                LaunchEffect.SetActive(true);
                State = TBBSProjectileEffectState.Launching;
                yield return new WaitForSeconds(LaunchEffectTime);
                //LaunchEffect.SetActive(false);
            }

            if(FlyEffect != null)
            {
                State = TBBSProjectileEffectState.Flying;
                
                float distToTarget = vecToTarget.magnitude;
                

                float effectiveEffectSpeed = FlyEffectSpeed;
                if(FlyEffectTime > 0)
                {
                    effectiveEffectSpeed = distToTarget / FlyEffectTime;
                }

                //this seems off but I can't actually find anything wrong with the calculation
                //Debug.Log($"calculated speed: {effectiveEffectSpeed:F2}");

                float displacement = 0;
                //yield return null;
                FlyEffect.SetActive(true);
                while (displacement < distToTarget)
                {
                    float distToMove = Time.deltaTime * effectiveEffectSpeed;
                    transform.Translate(dirToTarget * distToMove, Space.World);
                    displacement += distToMove;
                    yield return null;
                }

                transform.position = targetPosition;
                FlyEffect.SetActive(false);
            }

            if (LaunchEffect != null)
            {
                ExplodeEffect.SetActive(true);
                State = TBBSProjectileEffectState.Exploding;
                yield return new WaitForSeconds(ExplodeEffectTime);
                //ExplodeEffect.SetActive(false);
            }

            yield return null;

            State = TBBSProjectileEffectState.Finished;
        }

        public enum TBBSProjectileEffectState
        {
            NotStarted = 0,
            Launching = 1,
            Flying = 2,
            Exploding = 3,
            Finished = 4
        }
    }

    
}


