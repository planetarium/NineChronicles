using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    /// <summary>
    /// 일단 로딩창이 나옴
    /// 그 다음엔 외형이랑 스펙이 나옴
    /// 스펙엔 이름, 베이스 스탯, 옵션으로 붙은 스탯이 포함됨
    /// cp가 몇인지도 보여줘야하고
    /// </summary>
    public class CustomCraftResultPopup : PopupWidget
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
        private List<CustomCraftCpOptionView> optionViews;

        [SerializeField]
        private CoveredItemOptionView skillView;

        public void Show(Equipment resultEquipment)
        {
            itemView.SetData(resultEquipment);
            itemNameText.SetText(resultEquipment.GetLocalizedName());
            baseStatText.SetText($"{resultEquipment.Stat.StatType} {resultEquipment.Stat.BaseValueAsLong}");
            optionViews.ForEach(view => view.Hide(true));

            long cpSum = 0;
            var additionalStats = resultEquipment.StatsMap.GetAdditionalStats().ToList();
            var statCount = additionalStats.Count;
            for (var i = 0; i < statCount; i++)
            {
                var stat = additionalStats[i];
                var view = optionViews[i];
                var cp = (long)CPHelper.GetStatCP(stat.StatType, stat.AdditionalValue,
                    States.Instance.CurrentAvatarState.level);
                cpSum += cp;
                view.UpdateView(cp, stat);
                view.Show();
            }

            var skill = resultEquipment.Skills.First();
            var powerText = SkillExtensions.EffectToString(skill.SkillRow.Id, skill.SkillRow.SkillType, skill.Power, skill.StatPowerRatio, skill.ReferencedStatType);
            skillView.UpdateAsSkill(skill.SkillRow.GetLocalizedName(), powerText, skill.Chance);
            optionCpText.SetText($"{cpSum}");
            var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.OrderedList.First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            var cpRatingPercent = (int)Math.Max(100 - cpSum / (float) relationshipRow.MaxCp * 100, 1);
            cpTopPercentText.SetText(L10nManager.Localize("UI_TOP_N_PERCENT_FORMAT", cpRatingPercent));
            base.Show();
        }
    }
}
