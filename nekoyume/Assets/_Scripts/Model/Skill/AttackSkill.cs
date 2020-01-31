using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Elemental;
using Nekoyume.TableData;
using Unity.Mathematics;

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
        /// <returns></returns>
        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessDamage(CharacterBase caster, int simulatorWaveTurn)
        {
            var targets = skillRow.SkillTargetType.GetTarget(caster);
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = skillRow.ElementalType;
            var multiplier = GetMultiplier(skillRow.HitCount, 1);
            var skillDamage = caster.GetDamage(power);
            for (var i = 0; i < skillRow.HitCount; i++)
            {
                foreach (var target in targetList)
                {
                    // damage 0 = dodged.
                    var damage = 0;
                    var elementalResult = elemental.GetBattleResult(target.defElementType);
                    var critical = elementalResult == ElementalResult.Win;
                    if (target.IsHit(caster))
                    {
                        var multiply = multiplier[i];
                        if (!critical)
                        {
                            critical = caster.IsCritical(elementalResult);
                        }
                        damage = elemental.GetDamage(target.defElementType, skillDamage);
                        // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                        damage = (int)((long)damage * damage / (damage + target.DEF));
                        damage = (int)(damage * multiply);
                        damage = math.max(damage, 1);
                        if (critical)
                        {
                            damage = (int)(damage * CharacterBase.CriticalMultiplier);
                        }

                        target.CurrentHP -= damage;
                    }

                    // 연타공격은 항상 연출이 크리티컬로 보이도록 처리
                    if (skillRow.SkillCategory == SkillCategory.DoubleAttack)
                    {
                        critical = true;
                    }
                    infos.Add(new BattleStatus.Skill.SkillInfo((CharacterBase)target.Clone(), damage, critical,
                        skillRow.SkillCategory, simulatorWaveTurn, skillRow.ElementalType, skillRow.SkillTargetType));
                }
            }

            return infos;
        }

        private static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
        {
            if (hitCount == 1) return new[] { totalDamage };
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
