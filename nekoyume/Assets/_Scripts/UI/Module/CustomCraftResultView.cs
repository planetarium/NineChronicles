using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CustomCraftResultView : MonoBehaviour
    {
        [SerializeField]
        private VanillaItemView itemView;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI baseStatText;

        [SerializeField]
        private TextMeshProUGUI optionCpText;

        [SerializeField]
        private TextMeshProUGUI cpTopPercentText;

        [SerializeField]
        private List<CoveredItemOptionView> optionViews;

        [SerializeField]
        private CoveredItemOptionView skillView;

        private int _statCount;

        public void Show(Equipment resultEquipment)
        {
            itemView.SetData(resultEquipment);
            itemNameText.SetText(resultEquipment.GetLocalizedName());
            baseStatText.SetText($"{resultEquipment.Stat.StatType} {resultEquipment.Stat.BaseValueAsLong}");
            optionViews.ForEach(view => view.Hide(true));
            skillView.Hide(true);

            long cpSum = 0;
            var additionalStats = resultEquipment.StatsMap.GetAdditionalStats().ToList();
            _statCount = additionalStats.Count;
            for (var i = 0; i < _statCount; i++)
            {
                var stat = additionalStats[i];
                var view = optionViews[i];
                var cp = (long)CPHelper.GetStatCP(stat.StatType, stat.AdditionalValue,
                    States.Instance.CurrentAvatarState.level);
                cpSum += cp;
                if(view is CustomCraftCpOptionView cpView)
                {
                    cpView.UpdateView(cp, stat);
                }
                else
                {
                    view.UpdateView($"{stat.StatType} {stat.AdditionalValueAsLong}", "");
                }
            }

            var skill = resultEquipment.Skills.First();
            var powerText = SkillExtensions.EffectToString(skill.SkillRow.Id, skill.SkillRow.SkillType, skill.Power, skill.StatPowerRatio, skill.ReferencedStatType);
            skillView.UpdateAsSkill(skill.SkillRow.GetLocalizedName(), powerText, skill.Chance);
            if (cpTopPercentText)
            {
                var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.OrderedList.Last(row => row.Relationship <= ReactiveAvatarState.Relationship);
                var cpRatingPercent = (int)Math.Max(100 - cpSum / (float) relationshipRow.CpGroups.Max(cp => cp.MaxCp) * 100, 1);
                cpTopPercentText.SetText(L10nManager.Localize("UI_TOP_N_PERCENT_FORMAT", cpRatingPercent));
                optionCpText.SetText(TextHelper.FormatNumber(cpSum));
            }
            else
            {
                optionCpText.SetText($"CP {TextHelper.FormatNumber(cpSum)}");
            }
        }

        public void ShowStatView(int index)
        {
            if (index < _statCount)
            {
                optionViews[index].Show();
                AudioController.instance.PlaySfx(AudioController.SfxCode.CustomCraftJudge2);
            }
        }

        public void ShowSkillView()
        {
            skillView.Show();
            AudioController.instance.PlaySfx(AudioController.SfxCode.CustomCraftJudge2);
        }
    }
}
