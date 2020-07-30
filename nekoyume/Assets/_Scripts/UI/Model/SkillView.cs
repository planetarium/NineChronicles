using System;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
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

        public SkillView(Skill skill)
        {
            iconSprite.Value = skill.SkillRow.GetIcon();
            name.Value = skill.SkillRow.GetLocalizedName();
            power.Value = $"{L10nManager.Localize("UI_SKILL_POWER")}: {skill.Power}";
            chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";
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

            iconSprite.Value = skill.SkillRow.GetIcon();
            name.Value = skill.SkillRow.GetLocalizedName();
            power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";
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
