using System;
using System.Collections;
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

        [SerializeField]
        private ParticleSystem iceShieldParticle;

        [SerializeField]
        private ParticleSystem frostBiteParticle;

        protected void Awake()
        {
            summonedSpine.gameObject.SetActive(false);
        }

        protected override void AddEventToOwner()
        {
            StartCoroutine(AppearSummoner());
            if (Owner == null)
            {
                return;
            }
            base.AddEventToOwner();
            Owner.BuffCastCoroutine.Add(IceShieldId, OnBuffCast);
        }

        protected override void RemoveEventFromOwner()
        {
            if (Owner == null)
            {
                return;
            }
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

            StartCoroutine(OnFrostBite());
        }

        private IEnumerator AppearSummoner()
        {
            summonedSpine.gameObject.SetActive(true);
            var appearTrack = summonedSpine.AnimationState.SetAnimation(0, AppearAnimation, false);
            while (!appearTrack.IsComplete)
            {
                yield return null;
            }
            summonedSpine.AnimationState.SetAnimation(0, IdleAnimation, true);
        }

        private IEnumerator OnBuffCast(BuffCastingVFX buffCastingVFX)
        {
            var castingTrack = summonedSpine.AnimationState.SetAnimation(0, CastingAnimation, false);
            buffCastingVFX.Play();
            iceShieldParticle.Play();
            while (!castingTrack.IsComplete)
            {
                yield return null;
            }
            summonedSpine.AnimationState.SetAnimation(0, IdleAnimation, true);
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        private IEnumerator OnFrostBite()
        {
            var castingTrack = summonedSpine.AnimationState.SetAnimation(0, CastingAnimation, false);
            frostBiteParticle.Play();
            while (!castingTrack.IsComplete)
            {
                yield return null;
            }
            summonedSpine.AnimationState.SetAnimation(0, IdleAnimation, true);
        }
    }
}
