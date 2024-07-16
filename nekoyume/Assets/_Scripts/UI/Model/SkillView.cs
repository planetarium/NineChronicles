using System;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Common;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class SkillView : IDisposable
    {
        public readonly Skill Skill;
        public readonly ReactiveProperty<string> Name = new();
        public readonly ReactiveProperty<string> Power = new();
        public readonly ReactiveProperty<string> Chance = new();

        public SkillView(Skill skill)
        {
            Skill = skill;
            Name.Value = skill.SkillRow.GetLocalizedName();
            Chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";

            if (skill is BuffSkill buffSkill)
            {
                var powerValue = buffSkill.EffectToString();
                Power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            }
            else
            {
                var powerValue = skill.EffectToString();
                Power.Value = $"{L10nManager.Localize("UI_SKILL_POWER")}: {powerValue}";
            }
        }

        public SkillView(BuffSkill skill)
        {
            Skill = skill;
            var powerValue = skill.EffectToString();
            Name.Value = skill.SkillRow.GetLocalizedName();
            Power.Value = $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}";
            Chance.Value = $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%";
        }

        public void Dispose()
        {
            Name.Dispose();
            Power.Dispose();
            Chance.Dispose();
        }
    }
}
