using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Material : ItemBase
    {
        public new MaterialItemSheet.Row Data { get; }

        public Material(MaterialItemSheet.Row data) : base(data)
        {
            Data = data;
        }
        
        protected bool Equals(Material other)
        {
            return Data.Id == other.Data.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Material) obj);
        }
        
        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }

        // todo: 번역.
        public override string ToItemInfo()
        {
            var sb = new StringBuilder();
            if (Data.StatType.HasValue)
            {
                sb.AppendLine($"{Data.ElementalType} 속성. {Data.StatType} 을 최소 {Data.StatMin} ~ 최대 {Data.StatMax} 까지 상승시켜준다.");   
            }

            if (Data.SkillId == 0)
            {
                return sb.ToString();
            }
            
            if (!Tables.instance.SkillEffect.TryGetValue(Data.SkillId, out var skillEffect))
            {
                throw new KeyNotFoundException($"SkillEffect: {Data.SkillId}");
            }

            string targetString;
            switch (skillEffect.skillTargetType)
            {
                case SkillTargetType.Enemy:
                    targetString = "단일 적에게";
                    break;
                case SkillTargetType.Enemies:
                    targetString = "모든 적에게";
                    break;
                case SkillTargetType.Self:
                    targetString = "자신에게";
                    break;
                case SkillTargetType.Ally:
                    targetString = "아군에게";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (skillEffect.skillType)
            {
                case SkillType.Attack:
                    sb.AppendLine($"{Data.SkillChanceMin}% ~ {Data.SkillChanceMax}% 확률로 {targetString} {Data.SkillDamageMin} ~ {Data.SkillDamageMax}의 데미지를 입힌다.");
                    break;
                case SkillType.Buff:
                    sb.AppendLine($"{Data.SkillChanceMin}% ~ {Data.SkillChanceMax}% 확률로 {targetString} {Data.SkillDamageMin} ~ {Data.SkillDamageMax}의 버프를 사용한다.");
                    break;
                case SkillType.Debuff:
                    sb.AppendLine($"{Data.SkillChanceMin}% ~ {Data.SkillChanceMax}% 확률로 {targetString} {Data.SkillDamageMin} ~ {Data.SkillDamageMax}의 디버프를 사용한다.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return sb.ToString();
        }
    }
}
