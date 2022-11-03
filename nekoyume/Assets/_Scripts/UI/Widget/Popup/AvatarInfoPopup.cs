using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
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
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class AvatarInfoPopup : XTweenPopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";
        private static readonly Vector3 PlayerPosition = new Vector3(3000f, 2999.2f, 2.15f);

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

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
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

            if (!equipmentSlots.TryGetSlot(ItemSubType.Weapon, out _weaponSlot))
            {
                throw new Exception($"Not found {ItemSubType.Weapon} slot in {equipmentSlots}");
            }

            if (!equipmentSlots.TryGetSlot(ItemSubType.Armor, out _armorSlot))
            {
                throw new Exception($"Not found {ItemSubType.Armor} slot in {equipmentSlots}");
            }

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
            base.Show(ignoreShowAnimation);
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

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            if (_player != null)
            {
                _player.gameObject.SetActive(false);
                _player = null;
            }
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
                doubleClickItem: Equip,
                clickEquipmentToggle: () =>
                {
                },
                clickCostumeToggle: () =>
                {
                },
                elementalTypes);
        }

        private void UpdateNickname(int level, string nameWithHash)
        {
            nicknameText.text = string.Format(NicknameTextFormat, level, nameWithHash);
        }

        private void UpdateTitle(AvatarState avatarState)
        {
            var title = _player.Costumes
                .FirstOrDefault(x => x.ItemSubType == ItemSubType.Title && x.Equipped);
            if (title is null)
            {
                return;
            }

            Destroy(_cachedCharacterTitle);
            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private void UpdateSlot(AvatarState avatarState)
        {
            costumeSlots.SetPlayerCostumes(_player.Model, OnClickSlot, OnDoubleClickSlot);
            equipmentSlots.SetPlayerEquipments(_player.Model, OnClickSlot, OnDoubleClickSlot);
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
            this.cp.UpdateCP(cp);
        }

        private void UpdateStat(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var equipmentSetEffectSheet =
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var s = _player.Model.Stats.SetAll(_player.Model.Stats.Level,
                equipments, costumes, null,
                equipmentSetEffectSheet, costumeSheet);
            stats.SetData(s);
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
            Unequip(slot, false);
        }

        private void Equip(InventoryItem inventoryItem)
        {
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
                EquipEquipment(inventoryItem);
            }
        }

        private void EquipEquipment(InventoryItem inventoryItem)
        {
            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            if (inventoryItem.LevelLimited.Value && !inventoryItem.Equipped.Value)
            {
                return;
            }

            var itemBase = inventoryItem.ItemBase;
            if (TryToFindSlotAlreadyEquip(itemBase, out var slot))
            {
                Unequip(slot, false);
                return;
            }

            if (!TryToFindSlotToEquip(itemBase, out slot))
            {
                return;
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var prevCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            if (!slot.IsEmpty)
            {
                Unequip(slot, true);
            }

            slot.Set(itemBase, OnClickSlot, OnDoubleClickSlot);
            LocalLayerModifier.SetItemEquip(currentAvatarState.address, slot.Item, true);

            var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            cp.PlayAnimation(prevCp, currentCp);

            var player = Game.Game.instance.Stage.GetPlayer();
            switch (itemBase)
            {
                default:
                    return;
                case Costume costume:
                {
                    player.EquipCostume(costume);
                    if (costume.ItemSubType == ItemSubType.Title)
                    {
                        Destroy(_cachedCharacterTitle);
                        var clone = ResourcesHelper.GetCharacterTitle(costume.Grade,
                            costume.GetLocalizedNonColoredName(false));
                        _cachedCharacterTitle = Instantiate(clone, titleSocket);
                    }

                    break;
                }
                case Equipment _:
                {
                    switch (slot.ItemSubType)
                    {
                        case ItemSubType.Armor:
                        {
                            var armor = (Armor)_armorSlot.Item;
                            var weapon = (Weapon)_weaponSlot.Item;
                            player.EquipEquipmentsAndUpdateCustomize(armor, weapon);
                            break;
                        }
                        case ItemSubType.Weapon:
                            player.EquipWeapon((Weapon)slot.Item);
                            break;
                    }

                    break;
                }
            }

            Game.Event.OnUpdatePlayerEquip.OnNext(player);
            PostEquipOrUnequip(slot);
        }

        private void Unequip(EquipmentSlot slot, bool considerInventoryOnly)
        {
            if (Game.Game.instance.IsInWorld || slot.IsEmpty)
            {
                return;
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var prevCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            var slotItem = slot.Item;
            slot.Clear();
            LocalLayerModifier.SetItemEquip(currentAvatarState.address, slotItem, false);

            var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            cp.PlayAnimation(prevCp, currentCp);

            if (!considerInventoryOnly)
            {
                var selectedPlayer = Game.Game.instance.Stage.GetPlayer();
                switch (slotItem)
                {
                    default:
                        return;
                    case Costume costume:
                        selectedPlayer.UnequipCostume(costume, true);
                        selectedPlayer.EquipEquipmentsAndUpdateCustomize((Armor)_armorSlot.Item,
                            (Weapon)_weaponSlot.Item);
                        Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer);

                        if (costume.ItemSubType == ItemSubType.Title)
                        {
                            Destroy(_cachedCharacterTitle);
                        }

                        break;
                    case Equipment _:
                        switch (slot.ItemSubType)
                        {
                            case ItemSubType.Armor:
                            {
                                selectedPlayer.EquipEquipmentsAndUpdateCustomize(
                                    (Armor)_armorSlot.Item,
                                    (Weapon)_weaponSlot.Item);
                                break;
                            }
                            case ItemSubType.Weapon:
                                selectedPlayer.EquipWeapon((Weapon)_weaponSlot.Item);
                                break;
                        }

                        Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer); // 사실상 UI_Status 변경 용도
                        break;
                }
            }

            PostEquipOrUnequip(slot);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(Game.Game.instance.States.CurrentAvatarState);
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
                    var states = States.Instance.RuneSlotStates[_battleType];
                    var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                    if (!sheet.TryGetValue(model.RuneState.RuneId, out var row))
                    {
                        return;
                    }

                    var slots = states
                        .Where(x => !x.Value.IsLock)
                        .Where(x => x.Value.RuneType == (RuneType)row.RuneType)
                        .ToDictionary(x => x.Key, x => x.Value);
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
                () => Equip(model),
                () => { },
                () =>
                {
                    inventory.ClearSelectedItem();
                    UpdateRuneSlots();
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

                    submit = () => Equip(model);

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
            var states = States.Instance.RuneSlotStates[_battleType];
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (!sheet.TryGetValue(inventoryItem.RuneState.RuneId, out var row))
            {
                return;
            }

            var slots = states
                .Where(x => !x.Value.IsLock)
                .Where(x => x.Value.RuneType == (RuneType)row.RuneType)
                .ToDictionary(x => x.Key, x => x.Value);

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

            UpdateRuneSlots();
        }

        private void UpdateRuneSlots()
        {
            var states = States.Instance.RuneSlotStates[_battleType];
            inventory.UpdateRunes(states);
            runeSlots.Set(states, OnClickRuneSlot, OnDoubleClickRuneSlot);
        }

        private void UnequipRune(InventoryItem item)
        {
            var states = States.Instance.RuneSlotStates[_battleType];
            foreach (var slot in states.Values)
            {
                if (slot.IsEquipped(out var runeState))
                {
                    if (runeState.RuneId == item.RuneState.RuneId)
                    {
                        slot.Unequip();
                    }
                }
            }

            UpdateRuneSlots();
        }

        private void OnClickPresetTab(
            IToggleable toggle,
            BattleType battleType,
            System.Action onSetToggle = null)
        {
            _battleType = battleType;
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            onSetToggle?.Invoke();
            AudioController.PlayClick();
            UpdateRuneSlots();

            AvatarState currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            switch (battleType)
            {
                case BattleType.Adventure:
                    currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
                    break;
                case BattleType.Arena:
                    currentAvatarState = RxProps.PlayersArenaParticipant.Value.AvatarState;
                    break;
                case BattleType.Raid:
                    currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
                    break;
            }

            _player = Util.CreatePlayer(currentAvatarState, PlayerPosition);
            UpdateNickname(currentAvatarState.level, currentAvatarState.NameWithHash);

            // for has
            UpdateInventory();
            UpdateTitle(currentAvatarState);
            UpdateStat(currentAvatarState);
            UpdateSlot(currentAvatarState);

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
    }
}
