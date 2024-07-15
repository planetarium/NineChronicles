using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class OutfitSelectPopup : PopupWidget
    {
        [SerializeField]
        private SimpleItemScroll outfitScroll;

        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            outfitScroll.UpdateData(TableSheets.Instance.EquipmentItemRecipeSheet
                .Select(pair => pair.Value)
                .Where(r => r.ItemSubType == subType)
                .Select(r => new Item(ItemFactory.CreateItem(r.GetResultEquipmentItemRow(),
                    new ActionRenderHandler.LocalRandom(0)))));
        }
    }
}
