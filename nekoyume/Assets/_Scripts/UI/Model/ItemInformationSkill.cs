using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
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

        public ItemInformationSkill(Data.Table.Item itemRow)
        {
            if (!Game.Game.instance.TableSheets.SkillSheet.TryGetValue(itemRow.skillId, out var skillRow))
            {
                throw new KeyNotFoundException(nameof(itemRow.skillId));
            }

            iconSprite.Value = skillRow.GetIcon();
            name.Value = skillRow.GetLocalizedName();
            power.Value =
                $"{LocalizationManager.Localize("UI_SKILL_POWER")}: {itemRow.minDamage} - {itemRow.maxDamage}";
            chance.Value =
                $"{LocalizationManager.Localize("UI_SKILL_CHANCE")}: {itemRow.minChance:0%} - {itemRow.maxChance:0%}";
        }

        public ItemInformationSkill(Skill skill)
        {
            iconSprite.Value = skill.skillRow.GetIcon();
            name.Value = skill.skillRow.GetLocalizedName();
            power.Value = $"{LocalizationManager.Localize("UI_SKILL_POWER")}: {skill.power}";
            chance.Value = $"{LocalizationManager.Localize("UI_SKILL_CHANCE")}: {skill.chance:0%}";
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
