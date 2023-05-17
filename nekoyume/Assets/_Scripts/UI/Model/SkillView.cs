using System;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.State;
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
                var powerValue = buffSkill.EffectToString();
                power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            }
            else
            {
                var powerValue = skill.EffectToString();
                power.Value = $"{L10nManager.Localize("UI_SKILL_POWER")}: {powerValue}";
            }
        }

        public SkillView(BuffSkill skill)
        {
            var powerValue = skill.EffectToString();
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
