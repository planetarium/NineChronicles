using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game.Battle;
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
using UnityEngine.UI;

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

        [SerializeField]
        private Button collectionEffectButton;

        private GameObject _cachedCharacterTitle;
        private BattleType _battleType = BattleType.Adventure;
        private System.Action _onUpdate;
        private int? _compareCp;
        private int _previousCp;
        private int _currentCp;
        private bool _isAvatarInfo;

        private readonly Dictionary<Inventory.InventoryTabType, GameObject> _slots = new();
        private readonly List<int> _consumableIds = new();
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

            collectionEffectButton.onClick.AddListener(() =>
            {
                Widget.Find<StatsBonusPopup>().Show();
                AudioController.PlayClick();
            });

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

        public bool TryGetCellByIndex(int index, out InventoryCell item)
        {
            return inventory.TryGetCellByIndex(index, out item);
        }

        public void UpdateInventory(BattleType battleType, int? compareCp = null)
        {
            _compareCp = compareCp;
            _consumableIds.Clear();
            var elementalTypes = GetElementalTypes();
            inventory.SetAvatarInformation(
                OnClickInventoryItem,
                model => EquipOrUnequip(model),
                OnClickTab,
                elementalTypes,
                _battleType);

            StartCoroutine(CoUpdateView(battleType, Inventory.InventoryTabType.Equipment));
        }

        public List<Guid> GetBestEquipments()
        {
            return inventory.GetBestEquipments();
        }

        public InventoryItem GetBestEquipmentInventoryItems()
        {
            return inventory.GetBestEquipmentInventoryItem();
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
            var equippedRuneStates = States.Instance.GetEquippedRuneStates(_battleType);
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            inventory.UpdateRunes(equippedRuneStates, _battleType, sheet);
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
            inventory.UpdateConsumables(_consumableIds);
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
            var consumablesInventory =
                States.Instance.CurrentAvatarState.inventory.Consumables.ToArray();

            var equippedConsumables = new List<Consumable>();
            foreach (var id in _consumableIds)
            {
                var item = consumablesInventory.FirstOrDefault(consumable =>
                    consumable.Id == id && !equippedConsumables.Contains(consumable));
                if (item != null)
                {
                    equippedConsumables.Add(item);
                }
            }

            return equippedConsumables;
        }

        private void OnClickRuneSlot(RuneSlotView slot)
        {
            if (slot.RuneSlot.IsLock)
            {
                if (BattleRenderer.Instance.IsOnBattle)
                {
                    return;
                }

                FungibleAssetValue balance;
                int cost;
                CostType costType;
                string notEnoughContent;
                string attractMessage;
                System.Action onAttractWhenNotEnough;
                var isStatSlot = slot.RuneType == RuneType.Stat;
                var enoughContent = isStatSlot
                    ? L10nManager.Localize("UI_RUNE_SLOT_OPEN_STAT")
                    : L10nManager.Localize("UI_RUNE_SLOT_OPEN_SKILL");
                switch (slot.RuneSlot.RuneSlotType)
                {
                    case RuneSlotType.Ncg:
                        costType = CostType.NCG;
                        balance = States.Instance.GoldBalanceState.Gold;
                        cost = isStatSlot
                            ? States.Instance.GameConfigState.RuneStatSlotUnlockCost
                            : States.Instance.GameConfigState.RuneSkillSlotUnlockCost;
                        notEnoughContent =
                            L10nManager.Localize("UI_NOT_ENOUGH_NCG_WITH_SUPPLIER_INFO");
                        attractMessage = L10nManager.Localize("UI_SHOP");
                        onAttractWhenNotEnough = GoToMarket;
                        break;
                    case RuneSlotType.Crystal:
                        costType = CostType.Crystal;
                        balance = States.Instance.CrystalBalance;
                        cost = isStatSlot
                            ? States.Instance.GameConfigState.RuneStatSlotCrystalUnlockCost
                            : States.Instance.GameConfigState.RuneSkillSlotCrystalUnlockCost;
                        notEnoughContent = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                        attractMessage = L10nManager.Localize("UI_GO_GRINDING");
                        onAttractWhenNotEnough = GoToGrinding;
                        break;
                    case RuneSlotType.Default:
                    case RuneSlotType.Stake:
                    default:
                        return;
                }

                var enough = balance >= balance.Currency * cost;
                Widget.Find<PaymentPopup>().ShowAttract(
                    costType,
                    cost,
                    enough ? enoughContent : notEnoughContent,
                    enough ? L10nManager.Localize("UI_YES") : attractMessage,
                    enough
                        ? () =>
                        {
                            ActionManager.Instance.UnlockRuneSlot(slot.RuneSlot.Index);

                        }
                        : onAttractWhenNotEnough);
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

        private static void GoToMarket()
        {
            Widget.Find<AvatarInfoPopup>().CloseWithOtherWidgets();
            Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            Widget.Find<ShopSell>().Show(true);
        }

        private static void GoToGrinding()
        {
            Widget.Find<AvatarInfoPopup>().CloseWithOtherWidgets();
            Widget.Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            Widget.Find<Menu>().Close();
            Widget.Find<WorldMap>().Close();
            Widget.Find<StageInformation>().Close();
            Widget.Find<BattlePreparation>().Close();
            Widget.Find<Grind>().Show();
        }

        private void OnDoubleClickRuneSlot(RuneSlotView slot)
        {
            if (BattleRenderer.Instance.IsOnBattle)
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
                ShowTooltip(model, true);
            }
        }

        private void OnDoubleClickSlot(EquipmentSlot slot)
        {
            if (BattleRenderer.Instance.IsOnBattle)
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
            if (BattleRenderer.Instance.IsOnBattle)
            {
                return;
            }

            if (inventoryItem.LevelLimited.Value)
            {
                return;
            }

            var avatarState = States.Instance.CurrentAvatarState;
            var gameConfig = States.Instance.GameConfigState;
            if (!inventoryItem.IsValid(avatarState.level, gameConfig))
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
                                if (avatarState.level < gameConfig.RequireCharacterLevel_EquipmentSlotRing2)
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

                    if (inventoryItem.Count.Value <= 0)
                    {
                        return;
                    }

                    var slotCount = 0;
                    if (gameConfig.RequireCharacterLevel_ConsumableSlot1 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (gameConfig.RequireCharacterLevel_ConsumableSlot2 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (gameConfig.RequireCharacterLevel_ConsumableSlot3 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (gameConfig.RequireCharacterLevel_ConsumableSlot4 <= avatarState.level)
                    {
                        slotCount++;
                    }
                    if (gameConfig.RequireCharacterLevel_ConsumableSlot5 <= avatarState.level)
                    {
                        slotCount++;
                    }

                    if (_consumableIds.Any() && _consumableIds.Count == slotCount)
                    {
                        var lastConsumableId = _consumableIds.Last();
                        if (inventory.TryGetConsumable(lastConsumableId, out var lastItem))
                        {
                            _consumableIds.Remove(lastConsumableId);
                            lastItem.Count.Value++;
                        }
                    }

                    if (inventoryItem.ItemBase is Consumable consumable)
                    {
                        _consumableIds.Add(consumable.Id);
                        inventoryItem.Count.Value--;
                    }

                    inventory.UpdateConsumables(_consumableIds);
                    break;
            }

            UpdateItemView();
        }

        private void UnequipItem(InventoryItem inventoryItem)
        {
            if (BattleRenderer.Instance.IsOnBattle)
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
                        _consumableIds.Remove(consumable.Id);
                        inventoryItem.Count.Value++;
                    }
                    inventory.UpdateConsumables(_consumableIds);
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
                    if(!States.Instance.AllRuneState.TryGetRuneState(firstRuneId, out var firstState))
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
                        if(!States.Instance.AllRuneState.TryGetRuneState(runeId, out var state))
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
            if (BattleRenderer.Instance.IsOnBattle)
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
            ShowTooltip(model);
        }

        private void ShowTooltip(InventoryItem model, bool inSlot = false)
        {
            if (model.ItemBase is not null)
            {
                ShowItemTooltip(model, inSlot);
            }
            else if (model.RuneState is not null)
            {
                ShowRuneTooltip(model);
            }
            else
            {
                ShowFavTooltip(model);
            }
        }

        private void ShowItemTooltip(InventoryItem model, bool inSlot)
        {
            var item = model.ItemBase;
            var submitText = string.Empty;
            var interactable = false;
            System.Action submit = null;
            var blockedMessage = string.Empty;

            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    submitText = inSlot
                        ? L10nManager.Localize("UI_UNEQUIP")
                        : L10nManager.Localize("UI_EQUIP");
                    interactable = consumeSlots is not null && (inSlot || model.Count.Value > 0);
                    submit = () => EquipOrUnequip(model, inSlot);
                    blockedMessage = L10nManager.Localize("UI_EQUIP_FAILED");
                    break;
                case ItemType.Costume or ItemType.Equipment:
                    submitText = model.Equipped.Value
                        ? L10nManager.Localize("UI_UNEQUIP")
                        : L10nManager.Localize("UI_EQUIP");

                    if (!BattleRenderer.Instance.IsOnBattle)
                    {
                        if (model.DimObjectEnabled.Value)
                        {
                            interactable = model.Equipped.Value;
                        }
                        else
                        {
                            interactable = !model.LevelLimited.Value ||
                                           model.LevelLimited.Value && model.Equipped.Value;
                        }
                    }

                    submit = () => EquipOrUnequip(model);
                    blockedMessage = BattleRenderer.Instance.IsOnBattle
                        ? L10nManager.Localize("UI_BLOCK_EQUIP")
                        : L10nManager.Localize("UI_EQUIP_FAILED");
                    break;
                case ItemType.Material when item.ItemSubType == ItemSubType.ApStone:
                    submitText = L10nManager.Localize("UI_CHARGE_AP");
                    interactable = ActionPoint.IsInteractableMaterial();
                    submit = ReactiveAvatarState.ActionPoint > 0
                        ? () => ActionPoint.ShowRefillConfirmPopup(ActionPoint.ChargeAP)
                        : ActionPoint.ChargeAP;
                    blockedMessage = BattleRenderer.Instance.IsOnBattle
                        ? L10nManager.Localize("UI_BLOCK_CHARGE_AP")
                        : L10nManager.Localize("UI_AP_IS_FULL");
                    break;
            }

            System.Action blocked = () => NotificationSystem.Push(
                MailType.System,
                blockedMessage,
                NotificationCell.NotificationType.Alert);
            System.Action enhancement = item.ItemType == ItemType.Equipment ? () =>
            {
                if (BattleRenderer.Instance.IsOnBattle)
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
            } : null;

            ItemTooltip.Find(model.ItemBase.ItemType).Show(
                model,
                submitText,
                interactable,
                submit,
                () => inventory.ClearSelectedItem(),
                blocked,
                enhancement);
        }

        private void ShowRuneTooltip(InventoryItem model)
        {
            Widget.Find<RuneTooltip>().
                Show(
                model,
                L10nManager.Localize(model.Equipped.Value ? "UI_UNEQUIP" : "UI_EQUIP"),
                !BattleRenderer.Instance.IsOnBattle && !model.DimObjectEnabled.Value,
                () => EquipOrUnequip(model),
                () =>
                {
                    if (BattleRenderer.Instance.IsOnBattle)
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

        private void ShowFavTooltip(InventoryItem model)
        {
            Widget.Find<FungibleAssetTooltip>().
                Show(model,
                    () =>
                    {
                        inventory.ClearSelectedItem();
                        UpdateRuneView();
                    });
        }

        private void EquipOrUnequip(InventoryItem inventoryItem, bool inSlot = false)
        {
            if (inventoryItem.RuneState != null)
            {
                if (inventoryItem.Equipped.Value)
                {
                    UnequipRune(inventoryItem);
                }
                else
                {
                    if (BattleRenderer.Instance.IsOnBattle)
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
            else if (inventoryItem.ItemBase.ItemType == ItemType.Consumable)
            {
                if (inSlot)
                {
                    UnequipItem(inventoryItem);
                }
                else
                {
                    EquipItem(inventoryItem);
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

            var equippedRuneStates = States.Instance.GetEquippedRuneStates(_battleType);

            var allRuneState = States.Instance.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(allRuneState,
                runeListSheet, runeLevelBonusSheet);

            var runeStatModifiers = new List<StatModifier>();
            foreach (var runeState in equippedRuneStates)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var statRow) ||
                    !statRow.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                runeStatModifiers.AddRange(
                    statInfo.Stats.Select(x =>
                        new StatModifier(
                            x.stat.StatType,
                            x.operationType,
                            (long)(x.stat.BaseValue * (100000 + runeLevelBonus) / 100000m))));
            }

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionStatModifiers = collectionState.GetEffects(collectionSheet);

            characterStats.SetAll(
                avatarState.level,
                equipments,
                costumes,
                consumables,
                runeStatModifiers,
                equipmentSetEffectSheet,
                costumeSheet,
                collectionStatModifiers);

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
