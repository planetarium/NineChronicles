using System;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Inventory = Nekoyume.UI.Module.Inventory;
using Material = Nekoyume.Model.Item.Material;
using Toggle = Nekoyume.UI.Module.Toggle;

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
        private AvatarCP cp;

        [SerializeField]
        private AvatarStats stats;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Toggle grindModeToggle;

        [SerializeField]
        private GrindModule grindModule;

        [SerializeField]
        private GameObject statusObject;

        [SerializeField]
        private GameObject equipmentSlotObject;

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
        private GameObject _cachedCharacterTitle;
        private Coroutine _disableCpTween;
        private readonly ReactiveProperty<bool> IsTweenEnd = new ReactiveProperty<bool>(true);

        #region Override

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

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            grindModeToggle.onValueChanged.AddListener(toggledOn =>
            {
                if (toggledOn)
                {
                    statusObject.SetActive(false);
                    equipmentSlotObject.SetActive(false);
                    grindModule.Show();
                }
                else
                {
                    statusObject.SetActive(true);
                    equipmentSlotObject.SetActive(true);
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

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _player = Util.CreatePlayer(currentAvatarState, PlayerPosition);

            UpdateInventory();
            UpdateNickname(currentAvatarState.level, currentAvatarState.NameWithHash);
            UpdateTitle(currentAvatarState);
            UpdateStat(currentAvatarState);
            UpdateSlot(currentAvatarState);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);
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
                ? bp.GetElementalTypes() : ElementalTypeExtension.GetAllTypes();
            inventory.SetAvatarInfo(
                clickItem: ShowItemTooltip,
                doubleClickItem: Equip,
                clickEquipmentToggle: () =>
                {
                    costumeSlots.gameObject.SetActive(false);
                    equipmentSlots.gameObject.SetActive(true);
                },
                clickCostumeToggle: () =>
                {
                    costumeSlots.gameObject.SetActive(true);
                    equipmentSlots.gameObject.SetActive(false);
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
            if (Game.Game.instance.Stage.IsInStage)
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

            if (!slot.IsEmpty)
            {
                Unequip(slot, true);
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var prevCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
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
            if (Game.Game.instance.Stage.IsInStage || slot.IsEmpty)
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

                        Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer);
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

            return !Game.Game.instance.Stage.IsInStage;
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
                    if (!Game.Game.instance.Stage.IsInStage)
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

                    if (Game.Game.instance.Stage.IsInStage)
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

                        if (Game.Game.instance.Stage.IsInStage)
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
