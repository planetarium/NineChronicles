using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class SkillView : IDisposable
    {
        public readonly ReactiveProperty<Sprite> iconSprite = new ReactiveProperty<Sprite>();
        public readonly ReactiveProperty<string> name = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> power = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> chance = new ReactiveProperty<string>();

        public SkillView(MaterialItemSheet.Row itemRow)
        {
            if (!Game.Game.instance.TableSheets.SkillSheet.TryGetValue(itemRow.SkillId, out var skillRow))
            {
                throw new KeyNotFoundException(nameof(itemRow.SkillId));
            }

            iconSprite.Value = skillRow.GetIcon();
            name.Value = skillRow.GetLocalizedName();
            power.Value =
                $"{LocalizationManager.Localize("UI_SKILL_POWER")}: {itemRow.SkillDamageMin} - {itemRow.SkillDamageMax}";
            chance.Value =
                $"{LocalizationManager.Localize("UI_SKILL_CHANCE")}: {itemRow.SkillChanceMin}% - {itemRow.SkillChanceMax}%";
        }

        public SkillView(Skill skill)
        {
            iconSprite.Value = skill.skillRow.GetIcon();
            name.Value = skill.skillRow.GetLocalizedName();
            power.Value = $"{LocalizationManager.Localize("UI_SKILL_POWER")}: {skill.power}";
            chance.Value = $"{LocalizationManager.Localize("UI_SKILL_CHANCE")}: {skill.chance}%";
        }
        
        public SkillView(BuffSkill skill)
        {
            var powerValue = string.Empty;
            var sheets = Game.Game.instance.TableSheets;
            var buffs = BuffFactory.GetBuffs(skill, sheets.SkillBuffSheet, sheets.BuffSheet);
            if (buffs.Count > 0)
            {
                var buff = buffs[0];
                powerValue = buff.RowData.StatModifier.ToString();
            }
            
            iconSprite.Value = skill.skillRow.GetIcon();
            name.Value = skill.skillRow.GetLocalizedName();
            power.Value = $"{LocalizationManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            chance.Value = $"{LocalizationManager.Localize("UI_SKILL_CHANCE")}: {skill.chance}%";
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
