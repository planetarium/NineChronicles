using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ItemInformationSkill
    {
        public readonly ReactiveProperty<Sprite> headerImage = new ReactiveProperty<Sprite>();
        public readonly ReactiveProperty<string> headerKey = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> headerValue = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> firstLineKey = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> firstLineValue = new ReactiveProperty<string>();
        public readonly ReactiveProperty<bool> secondLineEnabled = new ReactiveProperty<bool>();
        public readonly ReactiveProperty<string> secondLineKey = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> secondLineValue = new ReactiveProperty<string>();

        public ItemInformationSkill(Data.Table.Item itemRow)
        {
            if (!Tables.instance.SkillEffect.TryGetValue(itemRow.skillId, out var skillEffect))
            {
                throw new KeyNotFoundException(itemRow.skillId.ToString());
            }

            headerKey.Value = LocalizationManager.Localize("UI_SKILL");
            headerValue.Value = itemRow.elemental == Elemental.ElementalType.Normal
                ? $"{skillEffect.category.Translate(itemRow.elemental)}"
                : $"{itemRow.elemental.Translate(skillEffect.category)} {skillEffect.category.Translate(itemRow.elemental)}";
            firstLineKey.Value = $"  - {LocalizationManager.Localize("UI_POWER")}";
            firstLineValue.Value = $"{itemRow.minDamage} - {itemRow.maxDamage}";
            secondLineEnabled.Value = true;
            secondLineKey.Value = $"  - {LocalizationManager.Localize("UI_CHANCE")}";
            secondLineValue.Value = $"{itemRow.minChance:0%} - {itemRow.maxChance:0%}";
        }

        public ItemInformationSkill(SkillBase skillBase)
        {
            var skillName = skillBase.elementalType == Elemental.ElementalType.Normal
                ? $"{skillBase.effect.category.Translate(skillBase.elementalType)}"
                : $"{skillBase.elementalType.Translate(skillBase.effect.category)} {skillBase.effect.category.Translate(skillBase.elementalType)}";
            
            headerKey.Value = LocalizationManager.Localize("UI_SKILL");;
            headerValue.Value = "";
            firstLineKey.Value = string.Format(LocalizationManager.Localize("UI_SKILL_DESCRIPTION_FORMAT"), skillName, skillBase.chance, skillBase.power);
            firstLineValue.Value = "";
            secondLineEnabled.Value = false;
        }
    }
}
