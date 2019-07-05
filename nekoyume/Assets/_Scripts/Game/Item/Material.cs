using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Data;
using Nekoyume.Data.Table;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Material : ItemBase
    {
        public Material(Data.Table.Item data) : base(data)
        {
        }

        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Data.elemental} 속성. {Data.stat} 을 최소 {Data.minStat} ~ 최대 {Data.maxStat} 까지 상승시켜준다.");

            if (Data.skillId == 0)
            {
                return sb.ToString();
            }
            
            if (!Tables.instance.SkillEffect.TryGetValue(Data.skillId, out var skillEffect))
            {
                throw new KeyNotFoundException($"SkillEffect: {Data.skillId}");
            }

            string targetString;
            switch (skillEffect.target)
            {
                case SkillEffect.Target.Enemy:
                    targetString = "단일 적에게";
                    break;
                case SkillEffect.Target.Enemies:
                    targetString = "모든 적에게";
                    break;
                case SkillEffect.Target.Self:
                    targetString = "자신에게";
                    break;
                case SkillEffect.Target.Ally:
                    targetString = "아군에게";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (skillEffect.type)
            {
                case SkillEffect.SkillType.Attack:
                    sb.AppendLine($"{Data.minChance}% ~ {Data.maxChance}% 확률로 {targetString} {Data.minDamage} ~ {Data.maxDamage}의 데미지를 입힌다.");
                    break;
                case SkillEffect.SkillType.Buff:
                    sb.AppendLine($"{Data.minChance}% ~ {Data.maxChance}% 확률로 {targetString} {Data.minDamage} ~ {Data.maxDamage}의 버프를 사용한다.");
                    break;
                case SkillEffect.SkillType.Debuff:
                    sb.AppendLine($"{Data.minChance}% ~ {Data.maxChance}% 확률로 {targetString} {Data.minDamage} ~ {Data.maxDamage}의 디버프를 사용한다.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return sb.ToString();
        }
    }
}
