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

        [Header("References")]
        public Transform OverlayPoint;
        public Transform TargetPoint;
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
                AudioPlayer.PlaySound(args.SoundEffect, SoundType.Sound, false);
            }
            // spawn effect if applicable
            if (!string.IsNullOrEmpty(args.InitialEffect))
            {
                WorldUtils.SpawnEffect(args.InitialEffect, GetTargetPoint(), Quaternion.identity, null, true);
            }

            Animator.speed = actualTimescale;
            Animator.Play(animationDefinition.AnimationName);

            //TODO do motion if applicable
            //TODO spawn late effect if applicable (play at midpoint)
            //TODO midpoint callback if applicable 
            args.MidpointCallback?.Invoke();

            yield return new WaitForSecondsEx(actualDuration, false, LockPause.PauseLockType.AllowCutscene);

            yield return null;

            Animator.speed = IdleAnimationTimescale;
            Animator.Play(IdleAnimation);

            if (!string.IsNullOrEmpty(args.LateEffect) && !args.PlayEffectAtMidpoint)
            {
                WorldUtils.SpawnEffect(args.LateEffect, GetTargetPoint(), Quaternion.identity, null, true);
            }

            yield return new WaitForSecondsEx(animationDefinition.HoldTime * (1f / args.AnimationTimescale), false, LockPause.PauseLockType.AllowCutscene);

            yield return null;

            completeCallback();
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
