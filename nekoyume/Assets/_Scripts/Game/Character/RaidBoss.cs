using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Skill = Nekoyume.Model.BattleStatus.Skill;

namespace Nekoyume.Game.Character
{
    public class RaidBoss : RaidCharacter
    {
        [SerializeField]
        private float buffVFXInterval = 0.5f;

        [SerializeField]
        private SpineController controller;

        private IEnumerator<Skill.SkillInfo> _skillEnumerator;
        private SkillSheet.Row _skillRow;

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
            Skill.SkillInfo info,
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

        public void SetSkillInfos(SkillSheet.Row skillRow, IEnumerable<Skill.SkillInfo> infos)
        {
            _skillRow = skillRow;
            _skillEnumerator = infos.GetEnumerator();
        }

        public void ProceedSkill(bool playAll)
        {
            StartCoroutine(CoProcessSkill(_skillRow, playAll));
        }

        public IEnumerator CoProcessSkill(SkillSheet.Row skillRow, bool playAll)
        {
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
                    if (playAll)
                    {
                        while (_skillEnumerator.MoveNext())
                        {
                            var info = _skillEnumerator.Current;
                            var buffTarget = info.Target.Id == Id ? this : _target;
                            ProcessBuff(buffTarget, info);
                            yield return new WaitForSeconds(buffVFXInterval);
                        }
                    }
                    else
                    {
                        _skillEnumerator.MoveNext();
                        var info = _skillEnumerator.Current;
                        var buffTarget = info.Target.Id == Id ? this : _target;
                        ProcessBuff(buffTarget, info);
                        yield return new WaitForSeconds(buffVFXInterval);
                    }
                    break;
                case SkillCategory.Heal:
                    if (playAll)
                    {
                        while (_skillEnumerator.MoveNext())
                        {
                            var info = _skillEnumerator.Current;
                            ProcessHeal(info);
                        }
                    }
                    else
                    {
                        _skillEnumerator.MoveNext();
                        var info = _skillEnumerator.Current;
                        ProcessHeal(info);
                    }
                    break;
                default:
                    if (playAll)
                    {
                        while (_skillEnumerator.MoveNext())
                        {
                            var info = _skillEnumerator.Current;
                            if (info.Buff == null)
                            {
                                var attackTarget = info.Target.Id == Id ? this : _target;
                                ProcessAttack(attackTarget, info, true);
                            }
                            else
                            {
                                var buffTarget = info.Target.Id == Id ? this : _target;
                                ProcessBuff(buffTarget, info);
                                yield return new WaitForSeconds(buffVFXInterval);
                            }
                        }
                    }
                    else
                    {
                        _skillEnumerator.MoveNext();
                        var info = _skillEnumerator.Current;

                        if (info.Buff == null)
                        {
                            var attackTarget = info.Target.Id == Id ? this : _target;
                            ProcessAttack(attackTarget, info, true);
                        }
                        else
                        {
                            var buffTarget = info.Target.Id == Id ? this : _target;
                            ProcessBuff(buffTarget, info);
                            yield return new WaitForSeconds(buffVFXInterval);
                        }
                    }
                    break;
            }
        }
    }
}
