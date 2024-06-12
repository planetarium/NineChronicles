using System;
using System.Collections;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Skill;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game
{
    public class AuraIceShield : AuraPrefabBase
    {
        public static int FrostBiteId => 709000;

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
        
        private bool _isPlaying;

        protected void Awake()
        {
            summonedSpine.gameObject.SetActive(false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            summonedSpine.gameObject.SetActive(false);
            _isPlaying = false;
        }

        protected override void AddEventToOwner()
        {
            if (Owner == null)
            {
                return;
            }
            base.AddEventToOwner();
            
            ForeachAllIceShieldBuff(iceShieldId =>
            {
                Owner.BuffCastCoroutine.Add(iceShieldId, OnBuffCast);
            });
        }

        protected override void RemoveEventFromOwner()
        {
            if (Owner == null)
            {
                return;
            }
            base.RemoveEventFromOwner();

            ForeachAllIceShieldBuff(iceShieldId =>
            {
                Owner.BuffCastCoroutine.Remove(iceShieldId);
            });
        }
        
        private void ForeachAllIceShieldBuff(Action<int> action, bool invokeOnce = false)
        {
            var actionBuffSheet = TableSheets.Instance.ActionBuffSheet;
            foreach (var row in actionBuffSheet)
            {
                if (row.ActionBuffType != ActionBuffType.IceShield)
                {
                    continue;
                }
                
                action(row.Id);
                
                if (invokeOnce)
                {
                    break;
                }
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
            ForeachAllIceShieldBuff(iceShieldId =>
            {
                if (iceShieldId != buffId)
                {
                    return;
                }
                
                base.ProcessBuffEnd(buffId);
                StartCoroutine(OnBuffEnd());
            }, invokeOnce: true);
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
            // 버프 연출과 동시에 수행하기 위해 대기하지 않음
            StartCoroutine(AppearSummoner());
            buffCastingVFX.Play();
            iceShieldParticle.Play();
            summonedSpine.AnimationState.SetAnimation(0, IdleAnimation, true);
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
            
            _isPlaying = true;
        }

        private IEnumerator OnBuffEnd()
        {
            _isPlaying = false;
            var disappearTrack = summonedSpine.AnimationState.SetAnimation(0, DisappearAnimation, false);
            while (!disappearTrack.IsComplete)
            {
                yield return null;
            }
            summonedSpine.gameObject.SetActive(false);
        }

        private IEnumerator OnFrostBite()
        {
            var castingTrack = summonedSpine.AnimationState.SetAnimation(0, CastingAnimation, false);
            frostBiteParticle.Play();
            while (!castingTrack.IsComplete)
            {
                yield return null;
            }

            if (_isPlaying)
            {
                summonedSpine.AnimationState.SetAnimation(0, IdleAnimation, true);
            }
        }
    }
}
