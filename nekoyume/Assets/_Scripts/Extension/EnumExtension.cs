using System;
using Nekoyume.Data.Table;

namespace Nekoyume
{
    public static class EnumExtension
    {
        public static string Translate(this Elemental.ElementalType elementalType, SkillEffect.Category category)
        {
            switch (elementalType)
            {
                case Elemental.ElementalType.Normal:
                    return "";
                case Elemental.ElementalType.Fire:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                            return "불";
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                            return "불꽃";
                        case SkillEffect.Category.Area:
                            return "용암";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Water:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                            return "물";
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                        case SkillEffect.Category.Area:
                            return "얼음";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Land:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                            return "대지";
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                        case SkillEffect.Category.Area:
                            return "모래";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Wind:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                            return "바람";
                        case SkillEffect.Category.Area:
                            return "거대";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
            }
        }
        
        public static string Translate(this SkillEffect.Category category, Elemental.ElementalType elementalType)
        {
            switch (category)
            {
                case SkillEffect.Category.Normal:
                    switch (elementalType)
                    {
                        case Elemental.ElementalType.Normal:
                        case Elemental.ElementalType.Fire:
                        case Elemental.ElementalType.Water:
                        case Elemental.ElementalType.Land:
                        case Elemental.ElementalType.Wind:
                            return "공격";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
                    }
                case SkillEffect.Category.Blow:
                    return "일격";
                case SkillEffect.Category.Double:
                    return "연사";
                case SkillEffect.Category.Area:
                    switch (elementalType)
                    {
                        case Elemental.ElementalType.Normal:
                            return "광역 난사";
                        case Elemental.ElementalType.Fire:
                        case Elemental.ElementalType.Water:
                            return "해일";
                        case Elemental.ElementalType.Land:
                            return "폭풍";
                        case Elemental.ElementalType.Wind:
                            return "태풍";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
