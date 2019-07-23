using System;
using Assets.SimpleLocalization;
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
                            return LocalizationManager.Localize("UI_SKILL_FIRE_NORMAL");
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                            return LocalizationManager.Localize("UI_SKILL_FIRE_BLOW");
                        case SkillEffect.Category.Area:
                            return LocalizationManager.Localize("UI_SKILL_FIRE_AREA");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Water:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                            return LocalizationManager.Localize("UI_SKILL_WATER_NORMAL");
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                        case SkillEffect.Category.Area:
                            return LocalizationManager.Localize("UI_SKILL_WATER_BLOW");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Land:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                            return LocalizationManager.Localize("UI_SKILL_LAND_NORMAL");
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                        case SkillEffect.Category.Area:
                            return LocalizationManager.Localize("UI_SKILL_LAND_BLOW");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(category), category, null);
                    }
                case Elemental.ElementalType.Wind:
                    switch (category)
                    {
                        case SkillEffect.Category.Normal:
                        case SkillEffect.Category.Blow:
                        case SkillEffect.Category.Double:
                            return LocalizationManager.Localize("UI_SKILL_WIND_NORMAL");
                        case SkillEffect.Category.Area:
                            return LocalizationManager.Localize("UI_SKILL_WIND_AREA");
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
                            return LocalizationManager.Localize("UI_SKILL_NORMAL");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
                    }
                case SkillEffect.Category.Blow:
                    return LocalizationManager.Localize("UI_SKILL_BLOW");
                case SkillEffect.Category.Double:
                    return LocalizationManager.Localize("UI_SKILL_DOUBLE");
                case SkillEffect.Category.Area:
                    switch (elementalType)
                    {
                        case Elemental.ElementalType.Normal:
                            return LocalizationManager.Localize("UI_SKILL_AREA_NORMAL");
                        case Elemental.ElementalType.Fire:
                        case Elemental.ElementalType.Water:
                            return LocalizationManager.Localize("UI_SKILL_AREA_FIRE_OR_WATER");
                        case Elemental.ElementalType.Land:
                            return LocalizationManager.Localize("UI_SKILL_AREA_LAND");
                        case Elemental.ElementalType.Wind:
                            return LocalizationManager.Localize("UI_SKILL_AREA_WIND");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
