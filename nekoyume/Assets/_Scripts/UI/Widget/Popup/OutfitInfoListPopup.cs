using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class OutfitInfoListPopup : PopupWidget
    {
        [SerializeField]
        private RandomOutfitScroll scroll;

        [SerializeField]
        private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
        }
        
        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            var rows = TableSheets.Instance.CustomEquipmentCraftIconSheet.Values.Where(row =>
                row.RequiredRelationship <= ReactiveAvatarState.Relationship &&
                row.ItemSubType == subType).ToList();
            var sumRatio = rows.Sum(row => (float)row.Ratio);
            scroll.UpdateData(rows.Select(
                row => new RandomOutfitCell.Model(row.IconId, $"{row.Ratio / sumRatio * 100f:F4}%")));
            base.Show(ignoreShowAnimation);
        }
    }
}
