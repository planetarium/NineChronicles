using Nekoyume.Model.BattleStatus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class RaidBoss : RaidCharacter
    {
        [Serializable]
        public class SpecialAttackAnimationInfo
        {
            public int SkillId;
            public CharacterAnimation.Type AnimationType;
            public float AnimationTime;
        }

        [SerializeField]
        private SpineController controller;

        [SerializeField]
        private List<SpecialAttackAnimationInfo> skillAnimationInfos;

        protected override void Awake()
        {
            Animator = new EnemyAnimator(this)
            {
                TimeScale = AnimatorTimeScale
            };

            base.Awake();
        }

        public override void Init(RaidCharacter target)
        {
            Animator.ResetTarget(gameObject);
            _attackTime = SpineAnimationHelper.GetAnimationDuration(controller, "Attack");
            _criticalAttackTime = SpineAnimationHelper.GetAnimationDuration(controller, "CriticalAttack");

            base.Init(target);
        }

        public override void UpdateStatusUI()
        {
            base.UpdateStatusUI();
            _worldBossBattle.UpdateStatus(_currentHp, _characterModel.HP, _characterModel.Buffs);
        }

        protected override IEnumerator CoProcessDamage(Skill.SkillInfo info, bool isConsiderElementalType)
        {
            var dmg = info.Effect;
            if (_currentHp - dmg < 0)
            {
                var exceeded = dmg - _currentHp;
                dmg -= exceeded;
            }
            Game.instance.RaidStage.AddScore(dmg);
            yield return base.CoProcessDamage(info, isConsiderElementalType);
        }

        public override IEnumerator CoSpecialAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            ActionPoint = () => ApplyDamage(skillInfos);
            var animationInfo = skillAnimationInfos.FirstOrDefault(x => x.SkillId == NextSpecialSkillId);
            NextSpecialSkillId = 0;

            if (animationInfo is null)
            {
                yield return StartCoroutine(CoAnimationAttack(skillInfos.Any(x => x.Critical)));
            }
            else
            {
                Animator.Play(animationInfo.AnimationType);
                yield return new WaitForSeconds(animationInfo.AnimationTime);
            }

            foreach (var info in skillInfos)
            {
                if (info.Buff is null)
                {
                    continue;
                }

                var target = info.Target.Id == Id ? this : _target;
                target.ProcessBuff(target, info);
            }
        }
    }
}
