using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Spine;
using Spine.Unity;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
	[RequireComponent(typeof(SkeletonAnimation))]
    public class SkeletonAnimationController: MonoBehaviour
    {
	    [Serializable]
	    public class StateNameToAnimationReference {
		    public string stateName;
		    public AnimationReferenceAsset animation;
	    }
	    
		public List<StateNameToAnimationReference> statesAndAnimations = new List<StateNameToAnimationReference>();

        private SkeletonAnimation _skeletonAnimation;

		private Spine.Animation targetAnimation { get; set; }

		#region Mono

		private void Awake ()
		{
			foreach (var entry in statesAndAnimations) {
				entry.animation.Initialize();
			}

			_skeletonAnimation = GetComponent<SkeletonAnimation>();
		}

		#endregion
		
		/// <summary>Sets the horizontal flip state of the skeleton based on a nonzero float. If negative, the skeleton is flipped. If positive, the skeleton is not flipped.</summary>
		public void SetFlip (float horizontal) {
			if (Math.Abs(horizontal) > 0f) {
				_skeletonAnimation.Skeleton.ScaleX = horizontal > 0 ? 1f : -1f;
			}
		}

		/// <summary>Plays an  animation based on the hash of the state name.</summary>
		public void PlayAnimationForState (int shortNameHash, int layerIndex) {
			var foundAnimation = GetAnimationForState(shortNameHash);
			if (foundAnimation == null)
				return;

			PlayNewAnimation(foundAnimation, layerIndex);
		}

		/// <summary>Gets a Spine Animation based on the hash of the state name.</summary>
		public Spine.Animation GetAnimationForState (int shortNameHash) {
			var foundState = statesAndAnimations.Find(entry => StringToHash(entry.stateName) == shortNameHash);
			return foundState?.animation;
		}

		/// <summary>Play an animation. If a transition animation is defined, the transition is played before the target animation being passed.</summary>
		public void PlayNewAnimation (Spine.Animation target, int layerIndex)
		{
			var loop = target.Name == CharacterAnimation.IdleLower
			           || target.Name == CharacterAnimation.RunLower
			           || target.Name == CharacterAnimation.CastingLower;

			_skeletonAnimation.AnimationState.SetAnimation(layerIndex, target, loop);
			targetAnimation = target;
		}

		/// <summary>Play a non-looping animation once then continue playing the state animation.</summary>
		public void PlayOneShot (Spine.Animation oneShot, int layerIndex) {
			var state = _skeletonAnimation.AnimationState;
			state.SetAnimation(0, oneShot, false);
			state.AddAnimation(0, targetAnimation, true, 0f);
		}

		private int StringToHash (string s) {
			return Animator.StringToHash(s);
		}
    }
}
