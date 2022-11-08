using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Inventory = Nekoyume.UI.Module.Inventory;
using Material = Nekoyume.Model.Item.Material;
using Toggle = Nekoyume.UI.Module.Toggle;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using Scroller;
    using UniRx;

    public class AvatarInfoPopup : XTweenPopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField]
        private Transform titleSocket;

        [SerializeField]
        private EquipmentSlots costumeSlots;

        [SerializeField]
        private EquipmentSlots equipmentSlots;

        [SerializeField]
        private RuneSlots runeSlots;

        [SerializeField]
        private AvatarCP cp;

        [SerializeField]
        private AvatarStats stats;

        [SerializeField]
        private CategoryTabButton adventureButton;

        [SerializeField]
        private CategoryTabButton arenaButton;

        [SerializeField]
        private CategoryTabButton raidButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Toggle grindModeToggle;

        [SerializeField]
        private GrindModule grindModule;

        [SerializeField]
        private GameObject grindModePanel;

        [SerializeField]
        private GameObject statusObject;

        [SerializeField]
        private GameObject equipmentSlotObject;

        private GameObject _cachedCharacterTitle;
        private Coroutine _disableCpTween;
        private InventoryItem _pickedItem;
        private System.Action _onToggleEquipment;
        private System.Action _onToggleCostume;
        private System.Action _onToggleRune;

        private readonly ToggleGroup _toggleGroup = new();
        private readonly ReactiveProperty<bool> IsTweenEnd = new(true);
        private readonly Dictionary<BattleType, System.Action> _onToggleCallback = new()
        {
            { BattleType.Adventure , null},
            { BattleType.Arena , null},
            { BattleType.Raid , null},
        };

        private BattleType _battleType = BattleType.Adventure;
        private List<(ItemSubType, int)> _availableItemSlots = new();

        #region Override

        protected override void Awake()
        {
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(arenaButton);
            _toggleGroup.RegisterToggleable(raidButton);

            adventureButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);
                })
                .AddTo(gameObject);
            arenaButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Arena, _onToggleCallback[BattleType.Arena]);
                })
                .AddTo(gameObject);
            raidButton.OnClick
                .Subscribe(b =>
                {
                    OnClickPresetTab(b, BattleType.Raid, _onToggleCallback[BattleType.Raid]);
                })
                .AddTo(gameObject);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();

            foreach (var slot in equipmentSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            foreach (var slot in costumeSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            grindModeToggle.onValueChanged.AddListener(toggledOn =>
            {
                grindModePanel.SetActive(toggledOn);
                statusObject.SetActive(!toggledOn);
                equipmentSlotObject.SetActive(!toggledOn);
                if (toggledOn)
                {
                    grindModule.Show();
                }
                else
                {
                    grindModule.gameObject.SetActive(false);
                    UpdateInventory();
                }
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            grindModeToggle.isOn = false;
            IsTweenEnd.Value = false;
            Destroy(_cachedCharacterTitle);
            OnClickPresetTab(adventureButton, BattleType.Adventure, _onToggleCallback[BattleType.Adventure]);
            HelpTooltip.HelpMe(100013, true);
            UpdateInventory();
            base.Show(ignoreShowAnimation);
        }

        private void UpdateRuneView()
        {
            var states = States.Instance.RuneSlotStates[_battleType].GetRuneSlot();
            var equippedRuneState = GetEquippedRuneStates();

            inventory.UpdateRunes(equippedRuneState);
            runeSlots.Set(states, OnClickRuneSlot, OnDoubleClickRuneSlot);
        }

        private void UpdateItemView()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var level = avatarState.level;
            var (equipments, costumes) = GetEquippedItems();

            // update spine
            Game.Game.instance.Lobby.Character.Set(avatarState, costumes, equipments);

            // update slots
            costumeSlots.SetPlayerCostumes(level, costumes, OnClickSlot, OnDoubleClickSlot);
            equipmentSlots.SetPlayerEquipments(level, equipments, OnClickSlot, OnDoubleClickSlot);

            var itemSlotState = States.Instance.ItemSlotStates[_battleType];
            inventory.UpdateCostumes(itemSlotState.Costumes);
            inventory.UpdateEquipments(itemSlotState.Equipments);

            //todo: cp 수정해줘야함
            // var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
            // this.cp.UpdateCP(cp);
        }

        private (List<Equipment>, List<Costume>) GetEquippedItems()
        {
            var itemSlotState = States.Instance.ItemSlotStates[_battleType];
            var avatarState = States.Instance.CurrentAvatarState;
            var equipmentInventory = avatarState.inventory.Equipments;
            var equipments = itemSlotState.Equipments
                .Select(equipment => equipmentInventory.FirstOrDefault(x => x.ItemId == equipment))
                .Where(item => item != null).ToList();

            var costumeInventory = avatarState.inventory.Costumes;
            var costumes = itemSlotState.Costumes
                .Select(equipment => costumeInventory.FirstOrDefault(x => x.ItemId == equipment))
                .Where(item => item != null).ToList();
            return (equipments, costumes);
        }

        protected override void OnTweenComplete()
        {
            base.OnTweenComplete();
            IsTweenEnd.Value = true;
        }

        protected override void OnTweenReverseComplete()
        {
            IsTweenEnd.Value = true;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            IsTweenEnd.Value = false;
        }

        #endregion

        private void UpdateInventory()
        {
            var bp = Find<BattlePreparation>();
            var elementalTypes = bp.isActiveAndEnabled
                ? bp.GetElementalTypes()
                : ElementalTypeExtension.GetAllTypes();
            inventory.SetAvatarInfo(
                clickItem: ShowItemTooltip,
                doubleClickItem: EquipOrUnequip,
                clickEquipmentToggle: () => { },
                clickCostumeToggle: () => { },
                elementalTypes);
        }

        private void UpdateNickname(int level, string nameWithHash)
        {
            nicknameText.text = string.Format(NicknameTextFormat, level, nameWithHash);
        }

        private void UpdateTitle(AvatarState avatarState)
        {
            // var title = _player.Costumes
            //     .FirstOrDefault(x => x.ItemSubType == ItemSubType.Title && x.Equipped);
            // if (title is null)
            // {
            //     return;
            // }

            // Destroy(_cachedCharacterTitle);
            // var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
            //     title.GetLocalizedNonColoredName(false));
            // _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private void UpdateStat(int previousCp)
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
            var (equipments, costumes) = GetEquippedItems();
            characterStats.SetAll(
                avatarState.level,
                equipments,
                costumes,
                null,
            equipmentSetEffectSheet,
                costumeSheet);

            var equippedRuneState = GetEquippedRuneStates();
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

            stats.SetData(characterStats);
            cp.PlayAnimation(previousCp, GetCp());
        }

        private int GetCp()
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeSlotState = States.Instance.RuneSlotStates[_battleType];
            var (equipments, costumes) = GetEquippedItems();
            var runeOptionInfos = runeSlotState.GetEquippedRuneStatInfos(runeOptionSheet);
            return CPHelper.TotalCP(equipments, costumes, runeOptionInfos, level, row, costumeSheet);
        }

        private void OnClickRuneSlot(RuneSlotView slot)
        {
            if (slot.RuneSlot.IsEquipped(out var runeState))
            {
                if (!inventory.TryGetModel(runeState.RuneId, out var item))
                {
                    return;
                }

                if (_pickedItem != null)
                {
                    UnequipRune(item);
                    EquipRune(_pickedItem);
                    _pickedItem = null;
                }
                else
                {
                    ShowRuneTooltip(item, slot.RectTransform, new float2(-50, 0));
                }
            }
            else
            {
                //todo : 장착 가능한룬 처리해줘야함
            }
        }

        private void OnDoubleClickRuneSlot(RuneSlotView slot)
        {
            if (!slot.RuneSlot.IsEquipped(out var runeState))
            {
                return;
            }

            if (!inventory.TryGetModel(runeState.RuneId, out var item))
            {
                return;
            }

            UnequipRune(item);
        }

        private void OnClickSlot(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                var bp = Find<BattlePreparation>();
                var elementalTypes = bp.isActiveAndEnabled
                    ? bp.GetElementalTypes() : ElementalTypeExtension.GetAllTypes();
                inventory.Focus(slot.ItemType, slot.ItemSubType, elementalTypes);
            }
            else
            {
                if (!inventory.TryGetModel(slot.Item, out var model))
                {
                    return;
                }

                inventory.ClearFocus();
                ShowItemTooltip(model, slot.RectTransform);
            }
        }

        private void OnDoubleClickSlot(EquipmentSlot slot)
        {
            if (!inventory.TryGetModel(slot.Item, out var item))
            {
                return;
            }

            UnequipItem(item);
        }

        private void EquipOrUnequip(InventoryItem inventoryItem)
        {
            var prevCp = GetCp();
            if (inventoryItem.RuneState != null)
            {
                if (inventoryItem.Equipped.Value)
                {
                    UnequipRune(inventoryItem);
                }
                else
                {
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

            UpdateStat(prevCp);
        }

        private void UnequipItem(InventoryItem inventoryItem)
        {
            var states = States.Instance.ItemSlotStates[_battleType];
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Costume:
                    if (inventoryItem.ItemBase is Costume costume)
                    {
                        states.Costumes.Remove(costume.ItemId);
                    }
                    inventory.UpdateEquipments(states.Costumes);
                    break;
                case ItemType.Equipment:
                    if (inventoryItem.ItemBase is Equipment equipment)
                    {
                        states.Equipments.Remove(equipment.ItemId);
                    }
                    inventory.UpdateEquipments(states.Equipments);
                    break;
            }

            UpdateItemView();
        }

        private void EquipItem(InventoryItem inventoryItem)
        {
            if (inventoryItem.LevelLimited.Value)
            {
                return;
            }


            var avatarState = States.Instance.CurrentAvatarState;
            if (!IsValid(inventoryItem, avatarState.level))
            {
                return;
            }

            var states = States.Instance.ItemSlotStates[_battleType];
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Costume:
                    var costumes = avatarState.inventory.Costumes;
                    if (costumes is null)
                    {
                        return;
                    }

                    var costumeSlots = states.Costumes;
                    var costumeGuids = new Dictionary<Guid, ItemSubType>();
                    foreach (var guid in costumeSlots)
                    {
                        var item = costumes.FirstOrDefault(x => x.ItemId == guid);
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
                        costumeSlots.Remove(guid);
                    }

                    if (inventoryItem.ItemBase is Costume costume)
                    {
                        costumeSlots.Add(costume.ItemId);
                    }

                    inventory.UpdateCostumes(costumeSlots);
                    break;

                case ItemType.Equipment:
                    var items = avatarState.inventory.Equipments;
                    if (items is null)
                    {
                        return;
                    }

                    var equipmentSlots = states.Equipments;
                    var equipmentGuids = new Dictionary<Guid, ItemSubType>();
                    foreach (var guid in states.Equipments)
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
                                        equipmentSlots.Remove(guid);
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
                                equipmentSlots.Remove(selectedItem);
                                break;
                        }
                    }
                    else
                    {
                        foreach (var guid in equipmentRemovalList)
                        {
                            equipmentSlots.Remove(guid);
                        }
                    }

                    if (inventoryItem.ItemBase is Equipment equip)
                    {
                        equipmentSlots.Add(equip.ItemId);
                    }

                    inventory.UpdateEquipments(equipmentSlots);
                    break;
            }

            UpdateItemView();
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(GetCp());
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
            Find<HeaderMenuStatic>().UpdateInventoryNotification(inventory.HasNotification);
        }

        private bool IsInteractableMaterial()
        {
            if (Find<HeaderMenuStatic>().ChargingAP) // is charging?
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
            var confirm = Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK", "UI_CANCEL",
                true, IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = () => Game.Game.instance.ActionManager.ChargeActionPoint(material).Subscribe();
            confirm.CancelCallback = () => confirm.Close();
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            if (model.RuneState != null)
            {
                ShowRuneTooltip(model, target, new float2(0, 0));
                _pickedItem = null;
                if (!model.Equipped.Value)
                {
                    var states = States.Instance.RuneSlotStates[_battleType].GetRuneSlot();;
                    var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                    if (!sheet.TryGetValue(model.RuneState.RuneId, out var row))
                    {
                        return;
                    }

                    var slots = states
                        .Where(x => !x.IsLock)
                        .Where(x => x.RuneType == (RuneType)row.RuneType)
                        .ToDictionary(x => x.Index, x => x);
                    if (slots.Values.All(x => x.IsEquipped(out _)) &&
                        slots.Values.Count(x => x.IsEquipped(out _)) > 1)
                    {
                        var indexes = slots.Where(x => x.Value.IsEquipped(out _))
                            .Select(kv => kv.Key)
                            .ToList();
                        runeSlots.ActiveWearable(indexes);
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_SELECT_RUNE_SLOT"),
                            NotificationCell.NotificationType.Alert);
                        _pickedItem = model;
                    }
                }
            }
            else
            {
                var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
                var (submitText, interactable, submit, blocked) = GetToolTipParams(model);
                tooltip.Show(
                    model,
                    submitText,
                    interactable,
                    submit,
                    () => inventory.ClearSelectedItem(),
                    blocked,
                    target);
            }
        }

        private Coroutine _coroutine;
        private void ShowRuneTooltip(InventoryItem model, RectTransform target, float2 offset)
        {
            Find<RuneTooltip>().Show(
                model,
                L10nManager.Localize(model.Equipped.Value ? "UI_UNEQUIP" : "UI_EQUIP"),
                true,
                () => EquipOrUnequip(model),
                () => { },
                () =>
                {
                    inventory.ClearSelectedItem();
                    UpdateRuneView();
                },
                target,
                offset);
        }

        private (string, bool, System.Action, System.Action) GetToolTipParams(InventoryItem model)
        {
            var item = model.ItemBase;
            var submitText = string.Empty;
            var interactable = false;
            System.Action submit = null;
            System.Action blocked = null;

            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    break;
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

                    submit = () => EquipOrUnequip(model);

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

            return (submitText, interactable, submit, blocked);
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
                    return costumeSlots.TryGetToEquip((Costume)item, out slot);
                case ItemType.Equipment:
                    return equipmentSlots.TryGetToEquip((Equipment)item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private void EquipRune(InventoryItem inventoryItem)
        {
            var states = States.Instance.RuneSlotStates[_battleType].GetRuneSlot();
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (!sheet.TryGetValue(inventoryItem.RuneState.RuneId, out var row))
            {
                return;
            }

            var slots = states
                .Where(x => !x.IsLock)
                .Where(x => x.RuneType == (RuneType)row.RuneType)
                .ToDictionary(x => x.Index, x => x);

            var selectedSlot = slots.Values.FirstOrDefault(x => !x.IsEquipped(out _));
            if (selectedSlot != null)
            {
                selectedSlot.Equip(inventoryItem.RuneState);
            }
            else
            {
                var count = slots.Count(x => x.Value.IsEquipped(out _));
                if (count == 1)
                {
                    slots.First().Value.Equip(inventoryItem.RuneState);
                }
                else
                {
                    // Do nothing
                }
            }

            UpdateRuneView();
        }

        private List<RuneState> GetEquippedRuneStates()
        {
            var states = States.Instance.RuneSlotStates[_battleType].GetRuneSlot();
            var runeStates = new List<RuneState>();
            foreach (var slot in states)
            {
                if (slot.IsEquipped(out var runeState))
                {
                    runeStates.Add(runeState);
                }
            }

            return runeStates;
        }

        private void UnequipRune(InventoryItem item)
        {
            var states = States.Instance.RuneSlotStates[_battleType].GetRuneSlot();
            foreach (var slot in states)
            {
                if (slot.IsEquipped(out var runeState))
                {
                    if (runeState.RuneId == item.RuneState.RuneId)
                    {
                        slot.Unequip();
                    }
                }
            }

            UpdateRuneView();
        }

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType,
            System.Action onSetToggle = null)
        {
            var prevCp = GetCp();
            _battleType = battleType;
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            onSetToggle?.Invoke();
            AudioController.PlayClick();

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;

            _availableItemSlots = UnlockHelper.GetAvailableEquipmentSlots(currentAvatarState.level);
            UpdateNickname(currentAvatarState.level, currentAvatarState.NameWithHash);
            UpdateTitle(currentAvatarState);
            UpdateRuneView();
            UpdateItemView();
            UpdateStat(prevCp);
        }

        private bool IsValid(InventoryItem inventoryItem, int level)
        {
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Costume:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            return level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        case ItemSubType.Title:
                            return level >= GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                            break;
                    }
                    break;
                case ItemType.Equipment:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                        case ItemSubType.Armor:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                        case ItemSubType.Belt:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                        case ItemSubType.Necklace:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                        case ItemSubType.Ring:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1;
                    }
                    break;
            }

            return false;
        }

        #region For tutorial

        public void TutorialActionClickAvatarInfoFirstInventoryCellView()
        {
            if (inventory.TryGetFirstCell(out var item))
            {
                item.Selected.Value = true;
            }
            else
            {
                Debug.LogError(
                    $"TutorialActionClickAvatarInfoFirstInventoryCellView() throw error.");
            }
        }

        public void TutorialActionCloseAvatarInfoWidget() => Close();

        #endregion

        // private void EquipEquipment(InventoryItem inventoryItem)
        // {
        //     if (Game.Game.instance.IsInWorld)
        //     {
        //         return;
        //     }
        //
        //     if (inventoryItem.LevelLimited.Value && !inventoryItem.Equipped.Value)
        //     {
        //         return;
        //     }
        //
        //     var itemBase = inventoryItem.ItemBase;
        //     if (TryToFindSlotAlreadyEquip(itemBase, out var slot))
        //     {
        //         Unequip(slot, false);
        //         return;
        //     }
        //
        //     if (!TryToFindSlotToEquip(itemBase, out slot))
        //     {
        //         return;
        //     }
        //
        //     var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
        //     var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
        //     var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
        //     var prevCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
        //     if (!slot.IsEmpty)
        //     {
        //         Unequip(slot, true);
        //     }
        //
        //     slot.Set(itemBase, OnClickSlot, OnDoubleClickSlot);
        //     LocalLayerModifier.SetItemEquip(currentAvatarState.address, slot.Item, true);
        //
        //     var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
        //     cp.PlayAnimation(prevCp, currentCp);
        //
        //     var player = Game.Game.instance.Stage.GetPlayer();
        //     switch (itemBase)
        //     {
        //         default:
        //             return;
        //         case Costume costume:
        //         {
        //             player.EquipCostume(costume);
        //             if (costume.ItemSubType == ItemSubType.Title)
        //             {
        //                 Destroy(_cachedCharacterTitle);
        //                 var clone = ResourcesHelper.GetCharacterTitle(costume.Grade,
        //                     costume.GetLocalizedNonColoredName(false));
        //                 _cachedCharacterTitle = Instantiate(clone, titleSocket);
        //             }
        //
        //             break;
        //         }
        //         case Equipment _:
        //         {
        //             switch (slot.ItemSubType)
        //             {
        //                 case ItemSubType.Armor:
        //                 {
        //                     var armor = (Armor)_armorSlot.Item;
        //                     var weapon = (Weapon)_weaponSlot.Item;
        //                     player.EquipEquipmentsAndUpdateCustomize(armor, weapon);
        //                     break;
        //                 }
        //                 case ItemSubType.Weapon:
        //                     player.EquipWeapon((Weapon)slot.Item);
        //                     break;
        //             }
        //
        //             break;
        //         }
        //     }
        //
        //     Game.Event.OnUpdatePlayerEquip.OnNext(player);
        //     PostEquipOrUnequip(slot);
        //
        //
        // }

                // private void Unequip(EquipmentSlot slot, bool considerInventoryOnly)
        // {
        //     if (Game.Game.instance.IsInWorld || slot.IsEmpty)
        //     {
        //         return;
        //     }
        //
        //     var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
        //     var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
        //     var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
        //     var prevCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
        //     var slotItem = slot.Item;
        //     slot.Clear();
        //     LocalLayerModifier.SetItemEquip(currentAvatarState.address, slotItem, false);
        //
        //     var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
        //     cp.PlayAnimation(prevCp, currentCp);
        //
        //     if (!considerInventoryOnly)
        //     {
        //         var selectedPlayer = Game.Game.instance.Stage.GetPlayer();
        //         switch (slotItem)
        //         {
        //             default:
        //                 return;
        //             case Costume costume:
        //                 selectedPlayer.UnequipCostume(costume, true);
        //                 selectedPlayer.EquipEquipmentsAndUpdateCustomize((Armor)_armorSlot.Item,
        //                     (Weapon)_weaponSlot.Item);
        //                 Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer);
        //
        //                 if (costume.ItemSubType == ItemSubType.Title)
        //                 {
        //                     Destroy(_cachedCharacterTitle);
        //                 }
        //
        //                 break;
        //             case Equipment _:
        //                 switch (slot.ItemSubType)
        //                 {
        //                     case ItemSubType.Armor:
        //                     {
        //                         selectedPlayer.EquipEquipmentsAndUpdateCustomize(
        //                             (Armor)_armorSlot.Item,
        //                             (Weapon)_weaponSlot.Item);
        //                         break;
        //                     }
        //                     case ItemSubType.Weapon:
        //                         selectedPlayer.EquipWeapon((Weapon)_weaponSlot.Item);
        //                         break;
        //                 }
        //
        //                 Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer); // 사실상 UI_Status 변경 용도
        //                 break;
        //         }
        //     }
        //
        //     PostEquipOrUnequip(slot);
        // }
    }
}
