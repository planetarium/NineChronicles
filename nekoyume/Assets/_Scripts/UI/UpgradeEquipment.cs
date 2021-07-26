using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using System.Numerics;
using Nekoyume.UI.Model;
using UnityEngine.UI;
using EquipmentInventory = Nekoyume.UI.Module.EquipmentInventory;

namespace Nekoyume.UI
{
    using UniRx;
    public class UpgradeEquipment : Widget
    {
        [SerializeField] private EquipmentInventory inventory;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private UpgradeEquipmentSlot baseEquipmentSlot;
        [SerializeField] private UpgradeEquipmentSlot materialEquipmentSlot;

        private Equipment _baseItem;
        private Equipment _materialItem;
        private BigInteger CostNCG = 0;
        private int CostAP = 0;

        protected override void Awake()
        {
            base.Awake();
            upgradeButton.onClick.AddListener(ActionUpgradeItem);
            closeButton.onClick.AddListener(() => Close(true));
        }

        public override void Initialize()
        {
            base.Initialize();

            baseEquipmentSlot.RemoveMaterial();
            materialEquipmentSlot.RemoveMaterial();
            inventory.SharedModel.SelectedItemView.Subscribe(ShowItemInformationTooltip).AddTo(gameObject);
            inventory.SharedModel.DeselectItemView();
            inventory.SharedModel.State.Value = ItemSubType.Weapon;
            inventory.SharedModel.DimmedFunc.Value = DimFunc;
            inventory.SharedModel.EffectEnabledFunc.Value = Contains;
        }

        private bool DimFunc(InventoryItem inventoryItem)
        {
            if (_baseItem is null && _materialItem is null)
                return false;

            var selectedItem = (Equipment)inventoryItem.ItemBase.Value;

            if (!(_baseItem is null))
            {
                if (CheckDim(selectedItem, _baseItem))
                {
                    return true;
                }
            }

            if (!(_materialItem is null))
            {
                if (CheckDim(selectedItem, _materialItem))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckDim(Equipment selectedItem, Equipment slotItem)
        {
            if (selectedItem.ItemId.Equals(slotItem.ItemId))
            {
                return true;
            }

            if (selectedItem.ItemSubType != slotItem.ItemSubType)
            {
                return true;
            }

            if (selectedItem.Grade != slotItem.Grade)
            {
                return true;
            }

            if (selectedItem.level != slotItem.level)
            {
                return true;
            }

            return false;
        }

        private bool Contains(InventoryItem inventoryItem)
        {
            var selectedItem = (Equipment)inventoryItem.ItemBase.Value;

            if (!(_baseItem is null))
            {
                if (selectedItem.ItemId.Equals(_baseItem.ItemId))
                {
                    return true;
                }
            }

            if (!(_materialItem is null))
            {
                if (selectedItem.ItemId.Equals(_materialItem.ItemId))
                {
                    return true;
                }
            }

            return false;
        }

        private void ActionUpgradeItem()
        {
            var baseGuid = _baseItem.ItemId;
            var materialGuid = _materialItem.ItemId;
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var slotIndex = Find<Combination>().selectedIndex;

            LocalLayerModifier.ModifyAgentGold(agentAddress, CostNCG * -1);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -CostAP);
            LocalLayerModifier.RemoveItem(avatarAddress, _baseItem.TradableId, _baseItem.RequiredBlockIndex, 1);
            LocalLayerModifier.RemoveItem(avatarAddress, _materialItem.TradableId, _materialItem.RequiredBlockIndex, 1);
            LocalLayerModifier.ModifyCombinationSlotItemEnhancement(baseGuid, materialGuid, slotIndex);
            Notification.Push(MailType.Workshop, L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START"));
            Game.Game.instance.ActionManager
                .ItemEnhancement(baseGuid, materialGuid, slotIndex)
                .Subscribe(_ => { }, e => ActionRenderHandler.BackToMain(false, e));

            baseEquipmentSlot.RemoveMaterial();
            materialEquipmentSlot.RemoveMaterial();
        }

        private void ShowItemInformationTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null || view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                _ => StageMaterial(view),
                _ => inventory.SharedModel.DeselectItemView());
        }

        private void StageMaterial(InventoryItemView viewModel)
        {
            if (_baseItem is null)
            {
                _baseItem = (Equipment) viewModel.Model.ItemBase.Value;
                baseEquipmentSlot.AddMaterial(viewModel.Model.ItemBase.Value,
                    () =>
                    {
                        inventory.ClearItemState(_baseItem);
                        _baseItem = null;
                        inventory.SharedModel.UpdateDimAndEffectAll();
                    });
            }
            else
            {
                _materialItem = (Equipment) viewModel.Model.ItemBase.Value;
                materialEquipmentSlot.AddMaterial(viewModel.Model.ItemBase.Value,
                    () =>
                    {
                        inventory.ClearItemState(_materialItem);
                        _materialItem = null;
                        inventory.SharedModel.UpdateDimAndEffectAll();
                    });
            }
            inventory.SharedModel.UpdateDimAndEffectAll();
        }
    }
}
