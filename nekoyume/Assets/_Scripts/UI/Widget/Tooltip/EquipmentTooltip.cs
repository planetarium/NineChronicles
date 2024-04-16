using System;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class EquipmentTooltip : ItemTooltip
    {
        [SerializeField] private TMP_Text expText;

        private void SetExpText(long exp)
        {
            expText.text = $"EXP : {exp.ToCurrencyNotation()}";
        }

        public override void Show(
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0)
        {
            base.Show(item, submitText, interactable, onSubmit, onClose, onBlocked, itemCount);
            SetExpText(item is Equipment equipment ? equipment.Exp : 0);
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
            base.Show(item, submitText, interactable, onSubmit, onClose, onBlocked, onEnhancement);
            SetExpText(item.ItemBase is Equipment equipment ? equipment.Exp : 0);
        }

        public override void Show(
            ShopItem item,
            int apStoneCount,
            Action<ConditionalButton.State> onRegister,
            Action<ConditionalButton.State> onSellCancellation,
            System.Action onClose)
        {
            base.Show(item, apStoneCount, onRegister, onSellCancellation, onClose);
            SetExpText(item.ItemBase is Equipment equipment ? equipment.Exp : 0);
        }

        public override void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            base.Show(item, onBuy, onClose);
            SetExpText(item.ItemBase is Equipment equipment ? equipment.Exp : 0);
        }

        public override void Show(
            EnhancementInventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            base.Show(item, submitText, interactable, onSubmit, onClose, onBlocked);
            SetExpText(item.ItemBase is Equipment equipment ? equipment.Exp : 0);
        }
    }
}
