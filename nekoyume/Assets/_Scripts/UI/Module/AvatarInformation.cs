using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using Scroller;
    using UniRx;

    public class AvatarInformation : MonoBehaviour
    {
        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private Transform titleSocket;

        [SerializeField]
        private EquipmentSlots equipmentSlots;

        [SerializeField]
        private EquipmentSlots costumeSlots;

        [SerializeField]
        private EquipmentSlots consumeSlots;

        [SerializeField]
        private RuneSlots runeSlots;

        [SerializeField]
        private TextMeshProUGUI cp;

        [SerializeField]
        private AvatarStats stats;

        private GameObject _cachedCharacterTitle;
        private BattleType _battleType = BattleType.Adventure;
        private System.Action _onUpdate;
        private int? _compareCp;
        private int _previousCp;
        private int _currentCp;
        private bool _isAvatarInfo;

        private readonly Dictionary<Inventory.InventoryTabType, GameObject> _slots = new();
        private readonly List<Guid> _consumables = new();
        private readonly List<IDisposable> _disposables = new();

        private void Start()
        {
            Game.Event.OnUpdateRuneState.AddListener(() =>
            {
                if (gameObject.activeSelf)
                {
                    UpdateInventory(_battleType);
                }
            });
        }

        public void Initialize(bool isAvatarInfo = false, System.Action onUpdate = null)
        {
            _isAvatarInfo = isAvatarInfo;
            _onUpdate = onUpdate;
            _slots.Add(Inventory.InventoryTabType.Equipment, equipmentSlots.gameObject);
            _slots.Add(Inventory.InventoryTabType.Costume, costumeSlots.gameObject);
            _slots.Add(Inventory.InventoryTabType.Rune, runeSlots.gameObject);
            if (consumeSlots != null)
            {
                _slots.Add(Inventory.InventoryTabType.Consumable, consumeSlots.gameObject);
                foreach (var slot in consumeSlots)
                {
                    slot.ShowUnlockTooltip = true;
                }
            }

            foreach (var slot in equipmentSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            foreach (var slot in costumeSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            _disposables.DisposeAllAndClear();
            LoadingHelper.UnlockRuneSlot.ObserveCountChanged().Subscribe(x => {
                UpdateRuneView();
            }).AddTo(_disposables);
        }

        public bool TryGetFirstCell(out InventoryItem item)
        {
            var result = inventory.TryGetFirstCell(out var inventoryItem);
            item = inventoryItem;
            return result;
        }

        public void UpdateInventory(BattleType battleType, int? compareCp = null)
        {
            _compareCp = compareCp;
            _consumables.Clear();
            var elementalTypes = GetElementalTypes();
            inventory.SetAvatarInformation(
                OnClickInventoryItem,
                EquipOrUnequip,
                OnClickTab,
                elementalTypes,
                _battleType);

            StartCoroutine(CoUpdateView(battleType, Inventory.InventoryTabType.Equipment));
        }

        public List<Guid> GetBestEquipments()
        {
            return inventory.GetBestEquipments();
        }

        public List<InventoryItem> GetBestRunes(BattleType battleType)
        {
            return inventory.GetBestRunes(battleType);
        }

        private IEnumerator CoUpdateView(BattleType battleType, Inventory.InventoryTabType tabType)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            OnClickTab(tabType);
            UpdateView(battleType);
        }

        private void OnClickTab(Inventory.InventoryTabType tabType)
        {
            if (_isAvatarInfo)
            {
                return;
            }

            foreach (var (k, v) in _slots)
            {
                v.SetActive(k == tabType);
            }
        }

        public void UpdateView(BattleType battleType)
        {
            _battleType = battleType;
            UpdateRuneView();
            UpdateItemView();
            UpdateStat();
            _onUpdate?.Invoke();
        }

        private void UpdateRuneView()
        {
            var states = States.Instance.CurrentRuneSlotStates[_battleType].GetRuneSlot();
            var equippedRuneState = States.Instance.GetEquippedRuneStates(_battleType);
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            inventory.UpdateRunes(equippedRuneState, _battleType, sheet);
            runeSlots.Set(states, OnClickRuneSlot, OnDoubleClickRuneSlot);
        }

        private void UpdateItemView()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var level = avatarState.level;
            var (equipments, costumes) = States.Instance.GetEquippedItems(_battleType);
            Game.Game.instance.Lobby.Character.Set(avatarState, equipments, costumes);

            costumeSlots.SetPlayerCostumes(level, costumes, OnClickSlot, OnDoubleClickSlot);
            equipmentSlots.SetPlayerEquipments(level, equipments, OnClickSlot, OnDoubleClickSlot);
            if (consumeSlots != null)
            {
                var consumables = GetEquippedConsumables();
                consumeSlots.SetPlayerConsumables(level, consumables,OnClickSlot, OnDoubleClickSlot);
            }

            var itemSlotState = States.Instance.CurrentItemSlotStates[_battleType];
            inventory.UpdateCostumes(itemSlotState.Costumes);
            inventory.UpdateEquipments(itemSlotState.Equipments);
            inventory.UpdateConsumables(_consumables);
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Destroy(_cachedCharacterTitle);

            var (_, costumes) = States.Instance.GetEquippedItems(_battleType);
            var title = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.Title);
            if (title is null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        public List<Consumable> GetEquippedConsumables()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var consumablesInventory = avatarState.inventory.Consumables;
            var consumables = _consumables
                .Select(guid => consumablesInventory.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            return consumables;
        }

        private void OnClickRuneSlot(RuneSlotView slot)
        {
            if (slot.RuneSlot.IsLock)
            {
                if (Game.Game.instance.IsInWorld)
                {
                    return;
                }

                switch (slot.RuneSlot.RuneSlotType)
                {
                    case RuneSlotType.Ncg:
                        var cost = slot.RuneType == RuneType.Stat
                            ? States.Instance.GameConfigState.RuneStatSlotUnlockCost
                            : States.Instance.GameConfigState.RuneSkillSlotUnlockCost;
                        var ncgHas = States.Instance.GoldBalanceState.Gold;
                        var enough = ncgHas.MajorUnit >= cost;
                        var content = slot.RuneType == RuneType.Stat
                                ? L10nManager.Localize("UI_RUNE_SLOT_OPEN_STAT")
                                : L10nManager.Localize("UI_RUNE_SLOT_OPEN_SKILL");
                        if (!enough)
                        {
                            content = L10nManager.Localize("UI_NOT_ENOUGH_NCG_WITH_SUPPLIER_INFO");
                        }

                        var attractMessage = enough
                            ? L10nManager.Localize("UI_YES")
                            : L10nManager.Localize("UI_GO_TO_MARKET");
                        Widget.Find<PaymentPopup>().ShowAttract(
                            CostType.NCG,
                            cost,
                            content,
                            attractMessage,
                            () =>
                            {
                                if (enough)
                                {
                                    ActionManager.Instance.UnlockRuneSlot(slot.RuneSlot.Index);
                                }
                                else
                                {
                                    GoToMarket();
                                }
                            });
                        break;
                    case RuneSlotType.Stake:
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_MESSAGE_CAN_NOT_OPEN"),
                            NotificationCell.NotificationType.Alert);
                        break;
                }
            }
            else
            {
                if (slot.RuneSlot.RuneId.HasValue)
                {
                    if (!inventory.TryGetModel(slot.RuneSlot.RuneId.Value, out var item))
                    {
                        return;
                    }

                    ShowRuneTooltip(item);
                }
                else
                {
                    inventory.Focus(slot.RuneType, Game.Game.instance.TableSheets.RuneListSheet);
                }
            }
        }

        private void GoToMarket()
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            Widget.Find<AvatarInfoPopup>().CloseWithOtherWidgets();
            Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            Widget.Find<ShopSell>().Show(true);
        }

        private void OnDoubleClickRuneSlot(RuneSlotView slot)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            if (!slot.RuneSlot.RuneId.HasValue)
            {
                return;
            }

            if (!inventory.TryGetModel(slot.RuneSlot.RuneId.Value, out var item))
            {
                return;
            }

            UnequipRune(item);
            UpdateStat();
            ShowCpScreen(item);
        }

        private void OnClickSlot(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                inventory.Focus(slot.ItemType, slot.ItemSubType, GetElementalTypes());
            }
            else
            {
                if (!inventory.TryGetModel(slot.Item, out var model))
                {
                    return;
                }

                inventory.ClearFocus();
                ShowItemTooltip(model);
            }
        }

        private void OnDoubleClickSlot(EquipmentSlot slot)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            if (!inventory.TryGetModel(slot.Item, out var item))
            {
                return;
            }

            UnequipItem(item);
            _onUpdate?.Invoke();
            UpdateStat();
            ShowCpScreen(item);
        }

        private void EquipItem(InventoryItem inventoryItem)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            if (inventoryItem.LevelLimited.Value)
            {
                return;
            }

            var avatarState = States.Instance.CurrentAvatarState;
            if (!inventoryItem.IsValid(avatarState.level))
            {
                return;
            }

            var states = States.Instance.CurrentItemSlotStates[_battleType];
            switch (inventoryItem.ItemBase.ItemType)
            {
              case ItemType.Equipment:
                    var items = avatarState.inventory.Equipments;
                    if (items is null)
                    {
                        return;
                    }

                    var equipments = states.Equipments;
                    var equipmentGuids = new Dictionary<Guid, ItemSubType>();
                    foreach (var guid in equipments)
                    {
                        var item = items.FirstOrDefault(x => x.ItemId == guid);
                        if (item != null)
                        {
                            equipmentGuids.Add(guid, item.ItemSubType);
                        }
                    }

                    var equipmentRemovalList = equipmentGuids
                        .Where(x => x.Value == inventoryItem.ItemBase.ItemSubType)
                        .Select(x => x.Key);
                    if (inventoryItem.ItemBase.ItemSubType == ItemSubType.Ring)
                    {
                        switch (equipmentRemovalList.Count())
                        {
                            case 1:
                                if (avatarState.level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2)
                                {
                                    foreach (var guid in equipmentRemovalList)
                                    {
                                        equipments.Remove(guid);
                                    }
                                }
                                break;
                            case 2:
                                var cp = new Dictionary<Guid, int>();
                                foreach (var guid in equipmentRemovalList)
                                {
                                    var item = items.FirstOrDefault(x => x.ItemId == guid);
                                    cp.Add(guid, CPHelper.GetCP(item));
                                }

                                var selectedItem = cp.OrderBy(x => x.Value).First().Key;
                                equipments.Remove(selectedItem);
                                break;
                        }
                    }
                    else
                    {
                        foreach (var guid in equipmentRemovalList)
                        {
                            equipments.Remove(guid);
                        }
                    }

                    if (inventoryItem.ItemBase is Equipment equip)
                    {
                        equipments.Add(equip.ItemId);
                    }

                    inventory.UpdateEquipments(equipments);
                    break;

                case ItemType.Costume:
                    var costumeItems = avatarState.inventory.Costumes;
                    if (costumeItems is null)
                    {
                        return;
                    }

                    var costumes = states.Costumes;
                    var costumeGuids = new Dictionary<Guid, ItemSubType>();
                    foreach (var guid in costumes)
                    {
                        var item = costumeItems.FirstOrDefault(x => x.ItemId == guid);
                        if (item != null)
                        {
                            costumeGuids.Add(guid, item.ItemSubType);
                        }
                    }

                    var costumeRemovalList = costumeGuids
                        .Where(x => x.Value == inventoryItem.ItemBase.ItemSubType)
                        .Select(x => x.Key);
                    foreach (var guid in costumeRemovalList)
                    {
                        costumes.Remove(guid);
                    }

                    if (inventoryItem.ItemBase is Costume costume)
                    {
                        costumes.Add(costume.ItemId);
                    }

                    inventory.UpdateCostumes(costumes);
                    break;

                case ItemType.Consumable:
                    if (_isAvatarInfo)
                    {
                        return;
                    }

                    var slotCount = 0;
                    if (GameConfig.RequireCharacterLevel.CharacterConsumableSlot1 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (GameConfig.RequireCharacterLevel.CharacterConsumableSlot2 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (GameConfig.RequireCharacterLevel.CharacterConsumableSlot3 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (GameConfig.RequireCharacterLevel.CharacterConsumableSlot4 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (GameConfig.RequireCharacterLevel.CharacterConsumableSlot5 <= avatarState.level)
                    {
                        slotCount++;
                    }

                    if (_consumables.Any() && _consumables.Count == slotCount)
                    {
                        _consumables.Remove(_consumables.Last());
                    }

                    if (inventoryItem.ItemBase is Consumable consumable)
                    {
                        _consumables.Add(consumable.ItemId);
                    }

                    inventory.UpdateConsumables(_consumables);
                    break;
            }

            UpdateItemView();
        }

        private void UnequipItem(InventoryItem inventoryItem)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            var states = States.Instance.CurrentItemSlotStates[_battleType];
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Equipment:
                    if (inventoryItem.ItemBase is Equipment equipment)
                    {
                        states.Equipments.Remove(equipment.ItemId);
                    }
                    inventory.UpdateEquipments(states.Equipments);
                    break;

                case ItemType.Costume:
                    if (inventoryItem.ItemBase is Costume costume)
                    {
                        states.Costumes.Remove(costume.ItemId);
                    }
                    inventory.UpdateCostumes(states.Costumes);
                    break;

                case ItemType.Consumable:
                    if (inventoryItem.ItemBase is Consumable consumable)
                    {
                        _consumables.Remove(consumable.ItemId);
                    }
                    inventory.UpdateConsumables(_consumables);
                    break;
            }

            UpdateItemView();
        }

        private void EquipRune(InventoryItem inventoryItem)
        {
            var states = States.Instance.CurrentRuneSlotStates[_battleType].GetRuneSlot();
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (!sheet.TryGetValue(inventoryItem.RuneState.RuneId, out var row))
            {
                return;
            }

            var slots = states
                .Where(x => !x.IsLock)
                .Where(x => x.RuneType == (RuneType)row.RuneType)
                .ToDictionary(x => x.Index, x => x);

            var selectedSlot = slots.Values.FirstOrDefault(x => !x.RuneId.HasValue);
            if (selectedSlot != null)// 비어있으면
            {
                selectedSlot.Equip(inventoryItem.RuneState.RuneId);
            }
            else
            {
                var firstSlot = slots.First();
                var count = slots.Count(x => x.Value.RuneId.HasValue);
                if (count == 1)
                {
                    firstSlot.Value.Equip(inventoryItem.RuneState.RuneId);
                }
                else
                {
                    if (!firstSlot.Value.RuneId.HasValue)
                    {
                        return;
                    }

                    var firstRuneId = firstSlot.Value.RuneId.Value;
                    if(!States.Instance.TryGetRuneState(firstRuneId, out var firstState))
                    {
                        return;
                    }

                    var cp = Util.GetRuneCp(firstState);
                    var slotIndex = firstSlot.Key;
                    foreach (var (index, runeSlot) in slots)
                    {
                        if (!runeSlot.RuneId.HasValue)
                        {
                            continue;
                        }

                        var runeId = runeSlot.RuneId.Value;
                        if(!States.Instance.TryGetRuneState(runeId, out var state))
                        {
                            return;
                        }

                        var curCp = Util.GetRuneCp(state);
                        if (curCp < cp)
                        {
                            slotIndex = index;
                            cp = curCp;
                        }
                    }

                    slots[slotIndex].Equip(inventoryItem.RuneState.RuneId);
                }
            }

            UpdateRuneView();
        }

        private void UnequipRune(InventoryItem item)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            var states = States.Instance.CurrentRuneSlotStates[_battleType].GetRuneSlot();
            foreach (var slot in states)
            {
                if (slot.RuneId.HasValue)
                {
                    if (slot.RuneId.Value == item.RuneState.RuneId)
                    {
                        slot.Unequip();
                    }
                }
            }

            UpdateRuneView();
        }

        private void OnClickInventoryItem(InventoryItem model)
        {
            ShowItemTooltip(model);
        }

        private void ShowItemTooltip(InventoryItem model)
        {
            if (model.RuneState != null)
            {
                ShowRuneTooltip(model);
            }
            else
            {
                var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
                var (submitText,
                    interactable,
                    submit,
                    blocked,
                    enhancement) = GetToolTipParams(model);
                tooltip.Show(
                    model,
                    submitText,
                    interactable,
                    submit,
                    () => inventory.ClearSelectedItem(),
                    blocked,
                    enhancement);
            }
        }

        private void ShowRuneTooltip(InventoryItem model)
        {
            Widget.Find<RuneTooltip>().
                Show(
                model,
                L10nManager.Localize(model.Equipped.Value ? "UI_UNEQUIP" : "UI_EQUIP"),
                !Game.Game.instance.IsInWorld && !model.DimObjectEnabled.Value,
                () => EquipOrUnequip(model),
                () =>
                {
                    if (Game.Game.instance.IsInWorld)
                    {
                        return;
                    }

                    if (LoadingHelper.RuneEnhancement.Value)
                    {
                        NotificationSystem.Push(MailType.System,
                            L10nManager.Localize("UI_CAN_NOT_ENTER_RUNE_MENU"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    var rune = Widget.Find<Rune>();
                    rune.CloseWithOtherWidgets();
                    rune.Show(model.RuneState.RuneId, true);
                    AudioController.PlayClick();
                },
                () =>
                {
                    inventory.ClearSelectedItem();
                    UpdateRuneView();
                });
        }

        private (string, bool, System.Action, System.Action, System.Action)
            GetToolTipParams(InventoryItem model)
        {
            var item = model.ItemBase;
            var submitText = string.Empty;
            var interactable = false;
            System.Action submit = null;
            System.Action blocked = null;
            System.Action enhancement = null;

            switch (item.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Costume:
                case ItemType.Equipment:
                    submitText = model.Equipped.Value
                        ? L10nManager.Localize("UI_UNEQUIP")
                        : L10nManager.Localize("UI_EQUIP");
                    if (!Game.Game.instance.IsInWorld)
                    {
                        if (model.DimObjectEnabled.Value)
                        {
                            interactable = model.Equipped.Value;
                        }
                        else
                        {
                            interactable = !model.LevelLimited.Value || model.LevelLimited.Value && model.Equipped.Value;
                        }
                    }

                    if (item.ItemType == ItemType.Consumable && consumeSlots is null)
                    {
                        interactable = false;
                    }

                    submit = () => EquipOrUnequip(model);

                    if (item.ItemType == ItemType.Equipment)
                    {
                        enhancement = () =>
                        {
                            if (Game.Game.instance.IsInWorld)
                            {
                                return;
                            }

                            if (item is not Equipment equipment)
                            {
                                return;
                            }

                            var e = Widget.Find<Enhancement>();
                            e.CloseWithOtherWidgets();
                            e.Show(item.ItemSubType, equipment.ItemId, true);
                            AudioController.PlayClick();
                        };
                    }

                    if (Game.Game.instance.IsInWorld)
                    {
                        blocked = () => NotificationSystem.Push(MailType.System,
                            L10nManager.Localize("UI_BLOCK_EQUIP"),
                            NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        blocked = () => NotificationSystem.Push(MailType.System,
                            L10nManager.Localize("UI_EQUIP_FAILED"),
                            NotificationCell.NotificationType.Alert);
                    }

                    break;
                case ItemType.Material:
                    if (item.ItemSubType == ItemSubType.ApStone)
                    {
                        submitText = L10nManager.Localize("UI_CHARGE_AP");
                        interactable = IsInteractableMaterial();

                        if (States.Instance.CurrentAvatarState.actionPoint > 0)
                        {
                            submit = () => ShowRefillConfirmPopup(item as Material);
                        }
                        else
                        {
                            submit = () => Game.Game.instance.ActionManager.ChargeActionPoint(item as Material).Subscribe();
                        }

                        if (Game.Game.instance.IsInWorld)
                        {
                            blocked = () => NotificationSystem.Push(MailType.System,
                                L10nManager.Localize("UI_BLOCK_CHARGE_AP"),
                                NotificationCell.NotificationType.Alert);
                        }
                        else
                        {
                            blocked = () => NotificationSystem.Push(MailType.System,
                                L10nManager.Localize("UI_AP_IS_FULL"),
                                NotificationCell.NotificationType.Alert);
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (submitText, interactable, submit, blocked, enhancement);
        }

        private bool IsInteractableMaterial()
        {
            if (Widget.Find<HeaderMenuStatic>().ChargingAP) // is charging?
            {
                return false;
            }

            if (States.Instance.CurrentAvatarState.actionPoint ==
                States.Instance.GameConfigState.ActionPointMax) // full?
            {
                return false;
            }

            return !Game.Game.instance.IsInWorld;
        }

        private void ShowRefillConfirmPopup(Material material)
        {
            var confirm = Widget.Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK", "UI_CANCEL",
                true, IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = () => Game.Game.instance.ActionManager.ChargeActionPoint(material).Subscribe();
            confirm.CancelCallback = () => confirm.Close();
        }

        private void EquipOrUnequip(InventoryItem inventoryItem)
        {
            if (inventoryItem.RuneState != null)
            {
                if (inventoryItem.Equipped.Value)
                {
                    UnequipRune(inventoryItem);
                }
                else
                {
                    if (Game.Game.instance.IsInWorld)
                    {
                        return;
                    }

                    if (inventoryItem.DimObjectEnabled.Value)
                    {
                        NotificationSystem.Push(MailType.System,
                            L10nManager.Localize("UI_MESSAGE_CAN_NOT_EQUIPPED"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    EquipRune(inventoryItem);
                }
            }
            else
            {
                if (inventoryItem.Equipped.Value)
                {
                    UnequipItem(inventoryItem);
                }
                else
                {
                    EquipItem(inventoryItem);
                }
            }

            UpdateStat();
            ShowCpScreen(inventoryItem);
            _onUpdate?.Invoke();
        }

        private void UpdateStat()
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var equipmentSetEffectSheet = Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                return;
            }

            var characterStats = new CharacterStats(row, avatarState.level);
            var consumables = GetEquippedConsumables();
            var (equipments, costumes) = States.Instance.GetEquippedItems(_battleType);
            characterStats.SetAll(
                avatarState.level,
                equipments,
                costumes,
                consumables,
                equipmentSetEffectSheet,
                costumeSheet);

            var equippedRuneState = States.Instance.GetEquippedRuneStates(_battleType);
            foreach (var runeState in equippedRuneState)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var statRow) ||
                    !statRow.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    statInfo.Stats.Select(x =>
                        new StatModifier(
                            x.statMap.StatType,
                            x.operationType,
                            x.statMap.ValueAsInt)));

                characterStats.AddOption(statModifiers);
                characterStats.EqualizeCurrentHPWithHP();
            }

            UpdateCp();
            stats.SetData(characterStats);
            Widget.Find<HeaderMenuStatic>().UpdateInventoryNotification(inventory.HasNotification());
        }

        private void UpdateCp()
        {
            _previousCp = _currentCp;
            var consumables = GetEquippedConsumables();
            _currentCp = Util.TotalCP(_battleType) + consumables.Sum(CPHelper.GetCP);;
            cp.text = _currentCp.ToString();
            if (_compareCp.HasValue)
            {
                cp.color = _currentCp < _compareCp.Value
                    ? Palette.GetColor(ColorType.TextDenial)
                    : Palette.GetColor(ColorType.TextPositive);
            }
            else
            {
                cp.color = Color.white;
            }
        }

        private void ShowCpScreen(InventoryItem inventoryItem)
        {
            if (inventoryItem.ItemBase?.ItemType is ItemType.Material)
            {
                return;
            }

            if (_previousCp == _currentCp)
            {
                return;
            }
            var cpScreen = Widget.Find<CPScreen>();
            cpScreen.Show(_previousCp, _currentCp);
        }

        private static List<ElementalType> GetElementalTypes()
        {
            var bp = Widget.Find<BattlePreparation>();
            var elementalTypes = bp.isActiveAndEnabled
                ? bp.GetElementalTypes()
                : ElementalTypeExtension.GetAllTypes();
            return elementalTypes;
        }
    }
}
