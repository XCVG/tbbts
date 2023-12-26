using CommonCore.Audio;
using CommonCore.World;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    /// <summary>
    /// Just for testing for now
    /// </summary>
    public class SimpleBattlerController : BattlerController
    {
        public BattlerAnimationDefinition[] AnimationDefinitions;
        public float IdleAnimationTimescale = 1;
        public string IdleAnimation;
        public string HitPuffEffect;

        [Header("References")]
        public Transform OverlayPoint;
        public Transform TargetPoint;
        public Transform AttackPoint;
        public Animator Animator;

        private AudioPlayer AudioPlayer;

        public override void Init(TBBSSceneController sceneController)
        {
            base.Init(sceneController);

            AudioPlayer = CCBase.GetModule<AudioModule>().AudioPlayer;
        }

        public override void PlayAnimation(string animation, Action completeCallback, BattlerAnimationArgs args)
        {
            StartCoroutine(CoDoAnimation(animation, completeCallback, args));
        }

        public override void SetIdleAnimation(string animation, BattlerAnimationArgs args)
        {
            //ignore any an all arguments and start/stop default idle animation
            if(string.IsNullOrEmpty(animation))
            {
                Animator.StopPlayback();
                //Animator.speed = 0; //hack
            }
            else
            {
                Animator.speed = IdleAnimationTimescale;
                Animator.Play(IdleAnimation);
                //Animator.StartPlayback();
            }            
        }

        public override Vector3 GetOverlayPoint()
        {
            return OverlayPoint.position;
        }

        public override Vector3 GetTargetPoint()
        {
            return TargetPoint.position;
        }

        public override Vector3 GetAttackOffsetVector()
        {
            if(AttackPoint != null)
                return transform.position - AttackPoint.position;

            return Vector3.zero;
        }

        public override string GetHitPuffEffect()
        {
            return HitPuffEffect;
        }


        private IEnumerator CoDoAnimation(string animation, Action completeCallback, BattlerAnimationArgs args)
        {
            BattlerAnimationDefinition animationDefinition = null;
            foreach(var a in  AnimationDefinitions)
            {
                if(a.Name.Equals(animation, StringComparison.OrdinalIgnoreCase))
                {
                    animationDefinition = a;
                    break;
                }
            }
            if(animationDefinition == null)
            {
                Debug.LogWarning($"BattlerController on {gameObject.name} can't play animation {animation} because no definition could be found!");
                yield return null;
                completeCallback();
                yield break;
            }

            float actualDuration = animationDefinition.Duration * (1f / args.AnimationTimescale);
            float actualTimescale = 1f / actualDuration;

            yield return null;
            //Debug.Log($"Duration: {actualDuration} | Timescale: {actualTimescale} | Animation: {animationDefinition.AnimationName}");

            // play additional sound effect if applicable
            if(!string.IsNullOrEmpty(args.SoundEffect))
            {
                AudioPlayer.PlaySound(args.SoundEffect, SoundType.Sound, false, false, false, args.PlaySoundPositional, 1f, transform.position);
            }
            // spawn effect if applicable
            if (!string.IsNullOrEmpty(args.InitialEffect))
            {
                SpawnEffect(args.InitialEffect, args.TargetBattler);
            }

            Animator.speed = actualTimescale;
            Animator.Play(animationDefinition.AnimationName);

            //do motion if applicable         
            //in this case maybe-spawn effect at midpoint and execute midpoint callback
            if(args.AnimateMotion)
            {
                Vector3 originalPosition = transform.position;

                float halfDuration = actualDuration / 2f;

                yield return null;
                for(float elapsed = 0; elapsed < halfDuration; elapsed += Time.deltaTime)
                {
                    float t = elapsed / halfDuration;
                    transform.position = Vector3.Lerp(originalPosition, args.TargetPosition, t);

                    yield return null;
                }

                transform.position = args.TargetPosition;

                args.MidpointCallback?.Invoke();
                if (!string.IsNullOrEmpty(args.LateEffect) && args.PlayEffectAtMidpoint)
                {
                    SpawnEffect(args.LateEffect, args.TargetBattler);
                }

                yield return null;

                for (float elapsed = 0; elapsed < halfDuration; elapsed += Time.deltaTime)
                {
                    float t = elapsed / halfDuration;
                    transform.position = Vector3.Lerp(args.TargetPosition, originalPosition, t);

                    yield return null;
                }

                transform.position = originalPosition;
            }
            else
            {
                yield return new WaitForSecondsEx(actualDuration, false, LockPause.PauseLockType.AllowCutscene);
            }            

            yield return null;

            if(!args.DoNotReturnToIdle)
            {
                Animator.speed = IdleAnimationTimescale;
                Animator.Play(IdleAnimation);
            }            

            if (!string.IsNullOrEmpty(args.LateEffect) && !args.PlayEffectAtMidpoint)
            {
                SpawnEffect(args.LateEffect, args.TargetBattler);
            }

            yield return new WaitForSecondsEx(animationDefinition.HoldTime * (1f / args.AnimationTimescale), false, LockPause.PauseLockType.AllowCutscene);

            yield return null;

            completeCallback();
        }

        private void SpawnEffect(string effect, BattlerController targetBattler)
        {
            var effectGO = WorldUtils.SpawnEffect(effect, GetTargetPoint(), Quaternion.identity, null, true);
            var effectScript = effectGO.GetComponent<TBBSEffectScriptBase>();
            effectScript.CurrentBattler = this;
            effectScript.TargetBattler = targetBattler;
        }
               

    }

    [Serializable]
    public class BattlerAnimationDefinition
    {
        public string Name;
        [Tooltip("if unset, will use Name as animation name")]
        public string AnimationName;
        public float Duration;
        public float HoldTime;
    }

}
