using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;

namespace Nekoyume.Game.Character
{
    public class RaidBoss : RaidCharacter
    {
        [SerializeField]
        private SpineController controller;

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

        public override void ProcessDamage(
            Model.BattleStatus.Skill.SkillInfo info,
            bool isConsiderElementalType)
        {
            var dmg = info.Effect;
            if (_currentHp - dmg < 0)
            {
                var exceeded = dmg - _currentHp;
                dmg -= exceeded;
            }
            Game.instance.RaidStage.AddScore(dmg);
            base.ProcessDamage(info, isConsiderElementalType);
        }

        public void ProcessSkill(
            int skillId,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> infos)
        {
            if (!Game.instance.TableSheets.SkillSheet.TryGetValue(skillId, out var skillRow))
            {
                return;
            }

            switch (skillRow.SkillCategory)
            {
                case SkillCategory.Buff:
                case SkillCategory.Debuff:
                case SkillCategory.AttackBuff:
                case SkillCategory.HPBuff:
                case SkillCategory.DefenseBuff:
                case SkillCategory.CriticalBuff:
                case SkillCategory.SpeedBuff:
                case SkillCategory.HitBuff:
                case SkillCategory.DamageReductionBuff:
                    foreach (var info in infos)
                    {
                        var buffTarget = info.Target.Id == Id ? this : _target;
                        ProcessBuff(buffTarget, info);
                    }
                    break;
                case SkillCategory.Heal:
                    foreach (var info in infos)
                    {
                        ProcessHeal(info);
                    }
                    break;
                default:
                    foreach (var info in infos)
                    {
                        if (info.Buff == null)
                        {
                            var attackTarget = info.Target.Id == Id ? this : _target;
                            ProcessAttack(attackTarget, info, true);
                        }
                        else
                        {
                            var buffTarget = info.Target.Id == Id ? this : _target;
                            ProcessBuff(buffTarget, info);
                        }
                    }
                    break;
            }
        }
    }
}
