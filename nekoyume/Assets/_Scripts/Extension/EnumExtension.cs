using System;
using Assets.SimpleLocalization;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game.Skill;

namespace Nekoyume
{
    public static class EnumExtension
    {
        public static string Translate(this Elemental.ElementalType elementalType, SkillCategory skillCategory)
        {
            switch (elementalType)
            {
                case Elemental.ElementalType.Normal:
                    return "";
                case Elemental.ElementalType.Fire:
                    switch (skillCategory)
                    {
                        case SkillCategory.Normal:
                            return LocalizationManager.Localize("UI_SKILL_FIRE_NORMAL");
                        case SkillCategory.Blow:
                        case SkillCategory.Double:
                            return LocalizationManager.Localize("UI_SKILL_FIRE_BLOW");
                        case SkillCategory.Area:
                            return LocalizationManager.Localize("UI_SKILL_FIRE_AREA");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(skillCategory), skillCategory, null);
                    }
                case Elemental.ElementalType.Water:
                    switch (skillCategory)
                    {
                        case SkillCategory.Normal:
                            return LocalizationManager.Localize("UI_SKILL_WATER_NORMAL");
                        case SkillCategory.Blow:
                        case SkillCategory.Double:
                        case SkillCategory.Area:
                            return LocalizationManager.Localize("UI_SKILL_WATER_BLOW");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(skillCategory), skillCategory, null);
                    }
                case Elemental.ElementalType.Land:
                    switch (skillCategory)
                    {
                        case SkillCategory.Normal:
                            return LocalizationManager.Localize("UI_SKILL_LAND_NORMAL");
                        case SkillCategory.Blow:
                        case SkillCategory.Double:
                        case SkillCategory.Area:
                            return LocalizationManager.Localize("UI_SKILL_LAND_BLOW");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(skillCategory), skillCategory, null);
                    }
                case Elemental.ElementalType.Wind:
                    switch (skillCategory)
                    {
                        case SkillCategory.Normal:
                        case SkillCategory.Blow:
                        case SkillCategory.Double:
                            return LocalizationManager.Localize("UI_SKILL_WIND_NORMAL");
                        case SkillCategory.Area:
                            return LocalizationManager.Localize("UI_SKILL_WIND_AREA");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(skillCategory), skillCategory, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
            }
        }
        
        public static string Translate(this SkillCategory skillCategory, Elemental.ElementalType elementalType)
        {
            switch (skillCategory)
            {
                case SkillCategory.Normal:
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
                case SkillCategory.Blow:
                    return LocalizationManager.Localize("UI_SKILL_BLOW");
                case SkillCategory.Double:
                    return LocalizationManager.Localize("UI_SKILL_DOUBLE");
                case SkillCategory.Area:
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
                    throw new ArgumentOutOfRangeException(nameof(skillCategory), skillCategory, null);
            }
        }
    }
}
