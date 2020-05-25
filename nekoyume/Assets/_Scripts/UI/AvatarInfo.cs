using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AvatarInfo : Widget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        // TODO: 코스튬 대응하기.
        // [SerializeField]
        // private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private DetailedStatView[] statViews = null;

        [SerializeField]
        private RectTransform avatarPosition = null;

        private EquipmentSlot _weaponSlot;
        private Vector3 _previousAvatarPosition;
        private int _previousSortingLayerID;
        private int _previousSortingLayerOrder;
        private CharacterStats _tempStats;

        #region Override

        public override void Initialize()
        {
            base.Initialize();

            _weaponSlot = equipmentSlots.First(es => es.ItemSubType == ItemSubType.Weapon);

            inventory.SharedModel.SelectedItemView
                .Subscribe(ShowTooltip)
                .AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView
                .Subscribe(itemView =>
                {
                    if (itemView is null ||
                        itemView.Model is null ||
                        itemView.Model.Dimmed.Value)
                    {
                        return;
                    }

                    Equip(itemView.Model);
                })
                .AddTo(gameObject);
            inventory.OnResetItems.Subscribe(SubscribeInventoryResetItems).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            inventory.SharedModel.State.Value = ItemType.Equipment;

            ReplacePlayer();
            UpdateSlotView();
            UpdateStatViews();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            ReturnPlayer();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void ReplacePlayer()
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            var playerTransform = player.transform;
            _previousAvatarPosition = playerTransform.position;
            _previousSortingLayerID = player.sortingGroup.sortingLayerID;
            _previousSortingLayerOrder = player.sortingGroup.sortingOrder;

            playerTransform.position = avatarPosition.position;
            player.SetSortingLayer(SortingLayer.NameToID("UI"), 11);

            _tempStats = player.Model.Stats.Clone() as CharacterStats;
        }

        private void ReturnPlayer()
        {
            var player = Game.Game.instance.Stage.GetPlayer(_previousAvatarPosition, true);
            player.SetSortingLayer(_previousSortingLayerID, _previousSortingLayerOrder);
        }

        private void UpdateSlotView()
        {
            var game = Game.Game.instance;
            var currentAvatar = game.States.CurrentAvatarState;
            var playerModel = game.Stage.GetPlayer().Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                currentAvatar.level,
                currentAvatar.NameWithHash);

            cpText.text = CPHelper.GetCP(currentAvatar, game.TableSheets.CharacterSheet)
                .ToString();

            // TODO: 코스튬 대응하기.
            // costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, Unequip);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, Unequip);

            // 인벤토리 아이템의 장착 여부를 `equipmentSlots`의 상태를 바탕으로 설정하기 때문에 `equipmentSlots.SetPlayer()`를 호출한 이후에 인벤토리 아이템의 장착 상태를 재설정한다.
            // 또한 인벤토리는 기본적으로 `OnEnable()` 단계에서 `OnResetItems` 이벤트를 일으키기 때문에 `equipmentSlots.SetPlayer()`와 호출 순서 커플링이 생기게 된다.
            // 따라서 강제로 상태를 설정한다.
            SubscribeInventoryResetItems(inventory);
        }

        private void UpdateStatViews()
        {
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                null,
                Game.Game.instance.TableSheets
            );
            using (var enumerator = stats.GetBaseAndAdditionalStats().GetEnumerator())
            {
                foreach (var statView in statViews)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    var (statType, baseValue, additionalValue) = enumerator.Current;
                    statView.Show(statType, baseValue, additionalValue);
                }
            }
        }

        #region Subscribe

        private void SubscribeInventoryResetItems(Module.Inventory value)
        {
            foreach (var inventoryItem in value.SharedModel.Equipments)
            {
                switch (inventoryItem.ItemBase.Value.Data.ItemType)
                {
                    case ItemType.Consumable:
                    case ItemType.Equipment:
                        inventoryItem.EquippedEnabled.Value =
                            TryToFindSlotAlreadyEquip(
                                (ItemUsable) inventoryItem.ItemBase.Value,
                                out _);
                        break;
                    case ItemType.Material:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        private void Equip(CountableItem countableItem)
        {
            if (!(countableItem is InventoryItem inventoryItem))
            {
                return;
            }

            var itemBase = inventoryItem.ItemBase.Value;
            // 이미 장착중인 아이템이라면 해제한다.
            if (TryToFindSlotAlreadyEquip(itemBase, out var slot))
            {
                Unequip(slot);
                return;
            }

            // 아이템을 장착할 슬롯을 찾는다.
            if (!TryToFindSlotToEquip(itemBase, out slot))
            {
                return;
            }

            // 이미 슬롯에 아이템이 있다면 해제한다.
            if (!slot.IsEmpty)
            {
                if (inventory.SharedModel.TryGetEquipment(
                        slot.Item as Equipment,
                        out var inventoryItemToUnequip) ||
                    inventory.SharedModel.TryGetConsumable(
                        slot.Item as Consumable,
                        out inventoryItemToUnequip))
                {
                    inventoryItemToUnequip.EquippedEnabled.Value = false;
                    LocalStateItemEquipModify(slot.Item, false);
                }
            }

            inventoryItem.EquippedEnabled.Value = true;
            slot.Set(itemBase, ShowTooltip, Unequip);
            LocalStateItemEquipModify(slot.Item, true);
            PostEquipOrUnequip(slot);
        }

        private void Unequip(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                foreach (var item in inventory.SharedModel.Equipments)
                {
                    item.GlowEnabled.Value =
                        item.ItemBase.Value.Data.ItemSubType == slot.ItemSubType;
                }

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(
                    slot.Item as Equipment,
                    out var inventoryItem) ||
                inventory.SharedModel.TryGetConsumable(
                    slot.Item as Consumable,
                    out inventoryItem))
            {
                inventoryItem.EquippedEnabled.Value = false;
                LocalStateItemEquipModify(slot.Item, false);
            }

            slot.Clear();
            PostEquipOrUnequip(slot);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStatViews();
            Find<ItemInformationTooltip>().Close();

            var player = Game.Game.instance.Stage.GetPlayer();
            if (slot.ItemSubType == ItemSubType.Armor)
            {
                var armor = (Armor) slot.Item;
                var weapon = (Weapon) _weaponSlot.Item;
                player.EquipEquipmentsAndUpdateCustomize(armor, weapon);
            }
            else if (slot.ItemSubType == ItemSubType.Weapon)
            {
                player.EquipWeapon((Weapon) slot.Item);
            }

            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
        }

        private static void LocalStateItemEquipModify(ItemBase itemBase, bool equip)
        {
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Costume:
                    LocalStateModifier.SetCostumeEquip(
                        States.Instance.CurrentAvatarState.address,
                        itemBase.Data.Id,
                        equip,
                        false);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment) itemBase;
                    LocalStateModifier.SetEquipmentEquip(
                        States.Instance.CurrentAvatarState.address,
                        equipment.ItemId,
                        equip,
                        false);
                    break;
            }
        }

        private bool TryToFindSlotAlreadyEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.Data.ItemType)
            {
                // TODO: 코스튬 대응하기.
                // case ItemType.Costume:
                //     return costumeSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Equipment:
                    return equipmentSlots.TryGetAlreadyEquip(item, out slot);
            }

            slot = null;
            return false;
        }

        private bool TryToFindSlotToEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.Data.ItemType)
            {
                // TODO: 코스튬 대응하기.
                // case ItemType.Costume:
                //     return costumeSlots.TryGetToEquip((Costume) item, out slot);
                case ItemType.Equipment:
                    return equipmentSlots.TryGetToEquip((Equipment) item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private void ShowTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model.Dimmed.Value,
                view.Model.EquippedEnabled.Value
                    ? LocalizationManager.Localize("UI_UNEQUIP")
                    : LocalizationManager.Localize("UI_EQUIP"),
                _ => Equip(tooltip.itemInformation.Model.item.Value),
                _ => { inventory.SharedModel.DeselectItemView(); });
        }

        private void ShowTooltip(EquipmentSlot slot)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (slot is null ||
                slot.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(slot.Item as Equipment, out var item) ||
                inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out item))
            {
                tooltip.Show(
                    slot.RectTransform,
                    item,
                    _ => inventory.SharedModel.DeselectItemView());
            }
        }
    }
}
