using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class MaterialTooltip : ItemTooltip
    {
        [SerializeField]
        protected GameObject acquisitionGroup;

        private const int MaxCountOfAcquisitionPlace = 4;

        public override void Show(
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0)
        {
            base.Show(
                item,
                submitText,
                interactable,
                onSubmit,
                onClose,
                onBlocked,
                itemCount);
            acquisitionGroup.SetActive(false);
            SetAcquisitionPlaceButtons(item);
        }

        public override void Show(
            ShopItem item,
            int apStoneCount,
            Action<ConditionalButton.State> onRegister,
            Action<ConditionalButton.State> onSellCancellation,
            System.Action onClose)
        {
            base.Show(item, apStoneCount, onRegister, onSellCancellation, onClose);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            base.Show(item, onBuy, onClose);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            System.Action onEnhancement = null)
        {
            Show(
                item.ItemBase,
                submitText,
                interactable,
                onSubmit,
                onClose,
                onBlocked,
                item.Count.Value);
        }

        private void SetAcquisitionPlaceButtons(ItemBase itemBase)
        {
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            var acquisitionPlaceList = ShortcutHelper.GetAcquisitionPlaceList(
                this, itemBase.Id, itemBase.ItemSubType, itemBase is ITradableItem);
            if (acquisitionPlaceList.Any())
            {
                acquisitionGroup.SetActive(true);
                var repeatCount = Math.Min(acquisitionPlaceList.Count, MaxCountOfAcquisitionPlace);
                for (var i = 0; i < repeatCount; i++)
                {
                    acquisitionPlaceButtons[i].gameObject.SetActive(true);
                    acquisitionPlaceButtons[i].Set(acquisitionPlaceList[i]);
                }
            }
        }
    }
}
