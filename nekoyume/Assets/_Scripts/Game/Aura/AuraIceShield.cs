using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game
{
    public class AuraIceShield : AuraPrefabBase
    {
        private const int IceShieldId = 708000;

        protected const string AppearAnimation    = "Appear";
        protected const string CastingAnimation   = "Casting";
        protected const string DisappearAnimation = "Disappear";
        protected const string IdleAnimation      = "Idle";

        [SerializeField]
        private SkeletonAnimation summonedSpine;

        protected void OnEnable()
        {
        }

        protected override void AddEventToOwner()
        {
            base.AddEventToOwner();
            Owner.BuffCastCoroutine.Add(IceShieldId, OnBuffCast);
        }

        protected override void RemoveEventFromOwner()
        {
            base.RemoveEventFromOwner();
            Owner.BuffCastCoroutine.Remove(IceShieldId);
        }

        protected override void ProcessBuff(int buffId)
        {
            if (IceShield.FrostBiteId != IceShieldId)
            {
                return;
            }

        }

        protected override void ProcessCustomEvent(int customEventId)
        {
            if (IceShield.FrostBiteId != customEventId)
            {
                return;
            }

        }

        private IEnumerator OnBuffCast(BuffCastingVFX buffCastingVFX)
        {
            var appearTrack = summonedSpine.AnimationState.SetAnimation(0, AppearAnimation, false);
            while (!appearTrack.IsComplete)
            {
                yield return null;
            }

            var castingTrack = summonedSpine.AnimationState.SetAnimation(0, CastingAnimation, false);
            while (!castingTrack.IsComplete)
            {
                yield return null;
            }
            buffCastingVFX.Play();
            // TODO: 소환수 파티클

            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }
    }
}
