using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Elemental;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public abstract class AttackSkill : Skill
    {
        protected AttackSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        /// <summary>
        /// todo: 캐릭터 스탯에 반영된 버프 효과가 스킬의 순수 데미지에는 영향을 주지 않는 로직.
        /// todo: 타겟의 회피 여부와 상관없이 버프의 확률로 발동되고 있음. 고민이 필요함.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="simulatorWaveTurn"></param>
        /// <param name="isNormalAttack"></param>
        /// <returns></returns>
        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessDamage(CharacterBase caster, int simulatorWaveTurn,
            bool isNormalAttack = false)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var targets = skillRow.SkillTargetType.GetTarget(caster).ToList();
            var elementalType = skillRow.ElementalType;
            var multipliers = GetMultiplier(skillRow.HitCount, 1m);
            for (var i = 0; i < skillRow.HitCount; i++)
            {
                var multiplier = multipliers[i];
                var damage = caster.ATK;

                foreach (var target in targets)
                {
                    var isCritical = false;
                    if (!isNormalAttack ||
                        target.IsHit(caster))
                    {
                        damage -= target.DEF;
                        if (damage < 1)
                        {
                            damage = 1;
                        }
                        else
                        {
                            damage = caster.GetDamage(damage, isNormalAttack);
                            damage = elementalType.GetDamage(target.defElementType, damage);
                            damage = (int) (damage * multiplier);
                            isCritical = caster.IsCritical(isNormalAttack);
                            if (isCritical)
                            {
                                damage = (int) (damage * CharacterBase.CriticalMultiplier);
                            }
                        }

                        target.CurrentHP -= damage;
                    }
                    else
                    {
                        damage = 0;
                    }

                    // 연타공격은 항상 연출이 크리티컬로 보이도록 처리.
                    if (skillRow.SkillCategory == SkillCategory.DoubleAttack)
                    {
                        isCritical = true;
                    }

                    infos.Add(new BattleStatus.Skill.SkillInfo((CharacterBase) target.Clone(), damage, isCritical,
                        skillRow.SkillCategory, simulatorWaveTurn, skillRow.ElementalType,
                        skillRow.SkillTargetType));
                }
            }

            return infos;
        }

        private static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
        {
            if (hitCount == 1) return new[] {totalDamage};
            var multiplier = new List<decimal>();
            var avg = totalDamage / hitCount;
            var lastDamage = avg * 1.3m;
            var lastHitIndex = hitCount - 1;
            var eachDamage = (totalDamage - lastDamage) / lastHitIndex;
            for (var i = 0; i < hitCount; i++)
            {
                var result = i == lastHitIndex ? lastDamage : eachDamage;
                multiplier.Add(result);
            }

            return multiplier.ToArray();
        }
    }
}
