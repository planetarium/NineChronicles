using System;
using System.Collections;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AvatarInfo : XTweenWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private AvatarStats avatarStats = null;

        [SerializeField]
        private RectTransform avatarPosition = null;

        private EquipmentSlot _weaponSlot;
        private bool _isShownFromBattle;
        private Player _player;
        private Vector3 _previousAvatarPosition;
        private int _previousSortingLayerID;
        private int _previousSortingLayerOrder;
        private bool _previousActivated;
        private CharacterStats _tempStats;
        private Coroutine _constraintsPlayerToUI;

        #region Override

        public override void Initialize()
        {
            base.Initialize();

            if (!equipmentSlots.TryGetSlot(ItemSubType.Weapon, out _weaponSlot))
            {
                throw new Exception($"Not found {ItemSubType.Weapon} slot in {equipmentSlots}");
            }

            inventory.SharedModel.State
                .Subscribe(inventoryState =>
                {
                    switch (inventoryState)
                    {
                        case ItemType.Consumable:
                            break;
                        case ItemType.Costume:
                            costumeSlots.gameObject.SetActive(true);
                            equipmentSlots.gameObject.SetActive(false);
                            break;
                        case ItemType.Equipment:
                            costumeSlots.gameObject.SetActive(false);
                            equipmentSlots.gameObject.SetActive(true);
                            break;
                        case ItemType.Material:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(inventoryState),
                                inventoryState, null);
                    }
                })
                .AddTo(gameObject);
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
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _isShownFromBattle = Find<Battle>().gameObject.activeSelf;
            Show(currentAvatarState, ignoreShowAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            ReturnPlayer();
        }

        #endregion

        public void Show(AvatarState avatarState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            inventory.SharedModel.State.Value = ItemType.Equipment;

            ReplacePlayer(avatarState);
            UpdateSlotView(avatarState);
            UpdateStatViews();
        }

        private void ReplacePlayer(AvatarState avatarState)
        {
            if (_player is null)
            {
                if (_isShownFromBattle)
                {
                    _player = PlayerFactory
                        .Create(avatarState)
                        .GetComponent<Player>();
                }
                else
                {
                    var stage = Game.Game.instance.Stage;
                    _previousActivated = stage.selectedPlayer && stage.selectedPlayer.gameObject.activeSelf;
                    _player = stage.GetPlayer();
                    _player.Set(avatarState);
                    _previousAvatarPosition = _player.transform.position;
                    _previousSortingLayerID = _player.sortingGroup.sortingLayerID;
                    _previousSortingLayerOrder = _player.sortingGroup.sortingOrder;
                }
            }

            _player.transform.position = avatarPosition.position;
            var orderInLayer = MainCanvas.instance.GetLayer(WidgetType).root.sortingOrder + 3;
            _player.SetSortingLayer(SortingLayer.NameToID("UI"), orderInLayer);

            _tempStats = _player.Model.Stats.Clone() as CharacterStats;

            if (!(_constraintsPlayerToUI is null))
            {
                StopCoroutine(_constraintsPlayerToUI);
            }

            _constraintsPlayerToUI = StartCoroutine(CoConstraintsPlayerToUI(_player.transform));
        }

        private IEnumerator CoConstraintsPlayerToUI(Transform playerTransform)
        {
            while (enabled)
            {
                playerTransform.position = avatarPosition.position;
                yield return null;
            }
        }

        private void ReturnPlayer()
        {
            if (!(_constraintsPlayerToUI is null))
            {
                StopCoroutine(_constraintsPlayerToUI);
                _constraintsPlayerToUI = null;
            }

            if (_player is null)
            {
                return;
            }

            if (_isShownFromBattle)
            {
                Game.Game.instance.Stage.objectPool.Remove<Player>(_player.gameObject);
                _player = null;
                return;
            }

            // NOTE: 플레이어를 강제로 재생성해서 플레이어의 모델이 장비 변경 상태를 반영하도록 합니다.
            _player = Game.Game.instance.Stage.GetPlayer(_previousAvatarPosition, true);
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _player.Set(currentAvatarState);
            _player.SetSortingLayer(_previousSortingLayerID, _previousSortingLayerOrder);
            _player.gameObject.SetActive(_previousActivated);
            _player = null;
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = game.Stage.GetPlayer().Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            cpText.text = CPHelper.GetCP(avatarState, game.TableSheets.CharacterSheet)
                .ToString();

            costumeSlots.SetPlayerCostumes(playerModel, ShowTooltip, Unequip);
            equipmentSlots.SetPlayerEquipments(playerModel, ShowTooltip, Unequip);

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (avatarState.Equals(currentAvatarState))
            {
                // 인벤토리 아이템의 장착 여부를 `equipmentSlots`의 상태를 바탕으로 설정하기 때문에 `equipmentSlots.SetPlayer()`를 호출한 이후에 인벤토리 아이템의 장착 상태를 재설정한다.
                // 또한 인벤토리는 기본적으로 `OnEnable()` 단계에서 `OnResetItems` 이벤트를 일으키기 때문에 `equipmentSlots.SetPlayer()`와 호출 순서 커플링이 생기게 된다.
                // 따라서 강제로 상태를 설정한다.
                inventory.gameObject.SetActive(true);
                SubscribeInventoryResetItems(inventory);
            }
            else
            {
                inventory.gameObject.SetActive(false);
            }
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

            avatarStats.SetData(stats);
        }

        #region Subscribe

        private void SubscribeInventoryResetItems(Module.Inventory value)
        {
            inventory.SharedModel.EquippedEnabledFunc.SetValueAndForceNotify(inventoryItem =>
                TryToFindSlotAlreadyEquip(inventoryItem.ItemBase.Value, out _));
        }

        #endregion

        private void Equip(CountableItem countableItem)
        {
            if (_isShownFromBattle ||
                !(countableItem is InventoryItem inventoryItem))
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
                Unequip(slot, true);
            }

            slot.Set(itemBase, ShowTooltip, Unequip);
            LocalStateItemEquipModify(slot.Item, true);

            Player player = null;
            switch (itemBase)
            {
                case Costume costume:
                {
                    inventoryItem.EquippedEnabled.Value = true;
                    player = Game.Game.instance.Stage.GetPlayer();
                    player.EquipCostume(costume);

                    break;
                }
                case Equipment equipment:
                {
                    inventoryItem.EquippedEnabled.Value = true;
                    UpdateStatViews();
                    player = Game.Game.instance.Stage.GetPlayer();
                    switch (slot.ItemSubType)
                    {
                        case ItemSubType.Armor:
                        {
                            var armor = (Armor) equipment;
                            var weapon = (Weapon) _weaponSlot.Item;
                            player.EquipEquipmentsAndUpdateCustomize(armor, weapon);
                            break;
                        }
                        case ItemSubType.Weapon:
                            player.EquipWeapon((Weapon) slot.Item);
                            break;
                    }

                    break;
                }
            }

            if (player is null)
            {
                return;
            }

            Game.Event.OnUpdatePlayerEquip.OnNext(player);
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
        }

        private void Unequip(EquipmentSlot slot)
        {
            Unequip(slot, false);
        }

        private void Unequip(EquipmentSlot slot, bool onlyData)
        {
            if (_isShownFromBattle)
            {
                return;
            }

            Find<ItemInformationTooltip>().Close();

            if (slot.IsEmpty)
            {
                foreach (var item in inventory.SharedModel.Equipments)
                {
                    item.GlowEnabled.Value =
                        item.ItemBase.Value.ItemSubType == slot.ItemSubType;
                }

                return;
            }

            var slotItem = slot.Item;
            slot.Clear();
            LocalStateItemEquipModify(slotItem, false);

            Player player = null;
            switch (slotItem)
            {
                case Costume costume:
                {
                    if (!inventory.SharedModel.TryGetCostume(costume, out var inventoryItem))
                    {
                        return;
                    }

                    inventoryItem.EquippedEnabled.Value = false;

                    if (onlyData)
                    {
                        break;
                    }

                    player = Game.Game.instance.Stage.GetPlayer();
                    player.UnequipCostume(costume);

                    break;
                }
                case Equipment equipment:
                {
                    if (!inventory.SharedModel.TryGetEquipment(equipment, out var inventoryItem))
                    {
                        return;
                    }

                    inventoryItem.EquippedEnabled.Value = false;

                    if (onlyData)
                    {
                        break;
                    }

                    UpdateStatViews();
                    player = Game.Game.instance.Stage.GetPlayer();
                    switch (slot.ItemSubType)
                    {
                        case ItemSubType.Armor:
                        {
                            var weapon = (Weapon) _weaponSlot.Item;
                            player.EquipEquipmentsAndUpdateCustomize(null, weapon);
                            break;
                        }
                        case ItemSubType.Weapon:
                            player.EquipWeapon((Weapon) slotItem);
                            break;
                    }

                    break;
                }
            }

            if (player is null)
            {
                return;
            }

            Game.Event.OnUpdatePlayerEquip.OnNext(player);
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
        }

        private static void LocalStateItemEquipModify(ItemBase itemBase, bool equip)
        {
            switch (itemBase.ItemType)
            {
                case ItemType.Costume:
                    LocalStateModifier.SetCostumeEquip(
                        States.Instance.CurrentAvatarState.address,
                        itemBase.Id,
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
            switch (item.ItemType)
            {
                case ItemType.Costume:
                    return costumeSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Equipment:
                    return equipmentSlots.TryGetAlreadyEquip(item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private bool TryToFindSlotToEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Costume:
                    return costumeSlots.TryGetToEquip((Costume) item, out slot);
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
                view.Model is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model.Dimmed.Value && !_isShownFromBattle,
                view.Model.EquippedEnabled.Value
                    ? LocalizationManager.Localize("UI_UNEQUIP")
                    : LocalizationManager.Localize("UI_EQUIP"),
                _ => Equip(tooltip.itemInformation.Model.item.Value),
                _ => inventory.SharedModel.DeselectItemView());
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

            if (inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out var item) ||
                inventory.SharedModel.TryGetCostume(slot.Item as Costume, out item) ||
                inventory.SharedModel.TryGetEquipment(slot.Item as Equipment, out item))
            {
                tooltip.Show(
                    slot.RectTransform,
                    item,
                    _ => inventory.SharedModel.DeselectItemView());
            }
        }
    }
}
