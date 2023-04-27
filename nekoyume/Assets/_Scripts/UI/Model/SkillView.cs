using System;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class SkillView : IDisposable
    {
        public readonly ReactiveProperty<string> name = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> power = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> chance = new ReactiveProperty<string>();

        public SkillView(Skill skill)
        {
            name.Value = skill.SkillRow.GetLocalizedName();

            chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";
            if (skill is BuffSkill buffSkill)
            {
                var sheets = Game.Game.instance.TableSheets;
                var buffs = BuffFactory.GetBuffs(
                    default,
                    skill,
                    sheets.SkillBuffSheet,
                    sheets.StatBuffSheet,
                    sheets.SkillActionBuffSheet,
                    sheets.ActionBuffSheet).OfType<StatBuff>();
                if (buffs.Any())
                {
                    var buff = buffs.First();
                    var powerValue = buff.RowData.StatModifier.ToString();
                    power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
                }
            }
            else
            {
                power.Value = $"{L10nManager.Localize("UI_SKILL_POWER")}: {skill.Power}";
            }
        }

        public SkillView(BuffSkill skill)
        {
            var powerValue = string.Empty;
            var sheets = Game.Game.instance.TableSheets;
            var buffs = BuffFactory.GetBuffs(
                default,
                skill,
                sheets.SkillBuffSheet,
                sheets.StatBuffSheet,
                sheets.SkillActionBuffSheet,
                sheets.ActionBuffSheet).OfType<StatBuff>();
            if (buffs.Count() > 0)
            {
                var buff = buffs.First();
                powerValue = buff.RowData.StatModifier.ToString();
            }

            name.Value = skill.SkillRow.GetLocalizedName();
            power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";
        }

        public void Dispose()
        {
            name.Dispose();
            power.Dispose();
            chance.Dispose();
        }
    }
}
