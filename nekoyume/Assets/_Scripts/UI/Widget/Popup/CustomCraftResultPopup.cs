using System.Linq;
using Lib9c.Renderers;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.Battle;
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

        public void Show(Equipment resultEquipment, ActionEvaluation<CustomEquipmentCraft> eval)
        {
            itemView.SetData(resultEquipment);
            itemNameText.SetText(resultEquipment.GetLocalizedName());
            baseStatText.SetText(resultEquipment.Stat.DecimalStatToString());
            var cp = resultEquipment.StatsMap.GetAdditionalStats().Sum(stat =>
                CPHelper.GetStatCP(stat.StatType, stat.AdditionalValue,
                    States.Instance.CurrentAvatarState.level));
            optionCpText.SetText($"{(long)cp}");
            base.Show();
        }
    }
}
