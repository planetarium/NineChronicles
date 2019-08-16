using System;
using System.Collections.Generic;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ItemInformationSkill : IDisposable
    {
        public readonly ReactiveProperty<Sprite> iconSprite = new ReactiveProperty<Sprite>();
        public readonly ReactiveProperty<string> name = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> power = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> chance = new ReactiveProperty<string>();

        public ItemInformationSkill(SkillSheet.Row skillRow)
        {
//            if (!Tables.instance.SkillEffect.TryGetValue(itemRow.skillId, out var skillEffect))
//            {
//                throw new KeyNotFoundException(itemRow.skillId.ToString());
//            }

            iconSprite.Value = skillRow.GetIcon();
            name.Value = skillRow.GetLocalizedName();

//            headerKey.Value = LocalizationManager.Localize("UI_SKILL");
//            headerValue.Value = itemRow.elemental == Elemental.ElementalType.Normal
//                ? $"{skillEffect.category.Translate(itemRow.elemental)}"
//                : $"{itemRow.elemental.Translate(skillEffect.category)} {skillEffect.category.Translate(itemRow.elemental)}";
//            firstLineKey.Value = $"  - {LocalizationManager.Localize("UI_POWER")}";
//            firstLineValue.Value = $"{itemRow.minDamage} - {itemRow.maxDamage}";
//            secondLineEnabled.Value = true;
//            secondLineKey.Value = $"  - {LocalizationManager.Localize("UI_CHANCE")}";
//            secondLineValue.Value = $"{itemRow.minChance:0%} - {itemRow.maxChance:0%}";
        }

        public ItemInformationSkill(SkillBase skillBase)
        {
            var skillName = skillBase.elementalType == Elemental.ElementalType.Normal
                ? $"{skillBase.effect.category.Translate(skillBase.elementalType)}"
                : $"{skillBase.elementalType.Translate(skillBase.effect.category)} {skillBase.effect.category.Translate(skillBase.elementalType)}";

//            headerKey.Value = LocalizationManager.Localize("UI_SKILL");;
//            headerValue.Value = "";
//            firstLineKey.Value = string.Format(LocalizationManager.Localize("UI_SKILL_DESCRIPTION_FORMAT"), skillName, skillBase.chance, skillBase.power);
//            firstLineValue.Value = "";
//            secondLineEnabled.Value = false;
        }

        public void Dispose()
        {
            iconSprite.Dispose();
            name.Dispose();
            power.Dispose();
            chance.Dispose();
        }
    }
}
