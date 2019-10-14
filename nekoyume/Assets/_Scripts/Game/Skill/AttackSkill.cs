using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.Game
{
    [Serializable]
    public abstract class AttackSkill : Skill
    {
        protected AttackSkill(SkillSheet.Row skillRow, int power, decimal chance) : base(skillRow, power, chance)
        {
        }

        /// <summary>
        /// todo: 캐릭터 스탯에 반영된 버프 효과가 스킬의 순수 데미지에는 영향을 주지 않는 로직.
        /// todo: 타겟의 회피 여부와 상관없이 버프의 확률로 발동되고 있음. 고민이 필요함.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        protected IEnumerable<Model.Skill.SkillInfo> ProcessDamage(CharacterBase caster)
        {
            var targets = effect.skillTargetType.GetTarget(caster);
            var infos = new List<Model.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = skillRow.ElementalType;
            var multiplier = GetMultiplier(effect.hitCount, 1);
            var skillDamage = caster.ATK + power;
            for (var i = 0; i < effect.hitCount; i++)
            {
                foreach (var target in targetList)
                {
                    if (target.Simulator.Random.Next(0, 100) < target.Stats.DOG)
                    {
                        Debug.LogWarning($"Dodged! caster: {caster.RowData.Id}, target: {target.RowData.Id}");
                        continue;
                    }

                    var multiply = multiplier[i];
                    var critical = caster.IsCritical();
                    var damage = elemental.GetDamage(target.defElementType, skillDamage);
                    // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                    damage = (int) ((long) damage * damage / (damage + target.DEF));
                    damage = (int) (damage * multiply);
                    damage = math.max(damage, 1);
                    if (critical)
                    {
                        damage = (int) (damage * CharacterBase.CriticalMultiplier);
                    }

                    target.CurrentHP -= damage;

                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), damage, critical,
                        effect.skillCategory, skillRow.ElementalType));
                }
            }

            return infos;
        }

        private static float[] GetMultiplier(int hitCount, float totalDamage)
        {
            if (hitCount == 1) return new[] {totalDamage};
            var multiplier = new List<float>();
            var avg = totalDamage / hitCount;
            var lastDamage = avg * 1.3f;
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
