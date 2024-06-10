using System;
using System.Collections;
using System.Linq;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game
{
    public class AuraIceShield : AuraPrefabBase
    {
        public static int FrostBiteId => 709000;
        
        // TODO: ID대신 GroupID사용 고려 혹은 ID와 GroupID사이의 정의 정리
        private static readonly int[] IceShieldIds = {
            708000, 708001, 708002
        };

        protected const string AppearAnimation    = "Appear";
        protected const string CastingAnimation   = "Casting";
        protected const string DisappearAnimation = "Disppear";
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

        protected override void OnDisable()
        {
            base.OnDisable();
            summonedSpine.gameObject.SetActive(false);
        }

        protected override void AddEventToOwner()
        {
            if (Owner == null)
            {
                return;
            }
            base.AddEventToOwner();

            foreach (var iceShieldId in IceShieldIds)
            {
                Owner.BuffCastCoroutine.Add(iceShieldId, OnBuffCast);
            }
        }

        protected override void RemoveEventFromOwner()
        {
            if (Owner == null)
            {
                return;
            }
            base.RemoveEventFromOwner();

            foreach (var iceShieldId in IceShieldIds)
            {
                Owner.BuffCastCoroutine.Remove(iceShieldId);
            }
        }

        protected override void ProcessCustomEvent(int customEventId)
        {
            if (FrostBiteId != customEventId)
            {
                return;
            }
            base.ProcessCustomEvent(customEventId);

            StartCoroutine(OnFrostBite());
        }

        protected override void ProcessBuffEnd(int buffId)
        {
            if (!IceShieldIds.Contains(buffId))
            {
                return;
            }
            base.ProcessBuffEnd(buffId);

            StartCoroutine(DisappearSummoner());
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

        private IEnumerator DisappearSummoner()
        {
            var disappearTrack = summonedSpine.AnimationState.SetAnimation(0, DisappearAnimation, false);
            while (!disappearTrack.IsComplete)
            {
                yield return null;
            }
            summonedSpine.gameObject.SetActive(false);
        }

        private IEnumerator OnBuffCast(BuffCastingVFX buffCastingVFX)
        {
            // 버프 연출과 동시에 수행하기 위해 대기하지 않음
            StartCoroutine(AppearSummoner());
            buffCastingVFX.Play();
            iceShieldParticle.Play();
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
