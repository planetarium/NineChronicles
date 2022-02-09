using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class AvatarInfoPopup : XTweenPopupWidget
    {
        private const string NicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";
        private static readonly Vector3 PlayerPosition = new Vector3(3000f, 2999.2f, 2.15f);

        [SerializeField]
        private InventoryView inventoryView;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private Transform titleSocket = null;

        [SerializeField]
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private DigitTextTweener cpTextValueTweener = null;

        [SerializeField]
        private GameObject additionalCpArea = null;

        [SerializeField]
        private TextMeshProUGUI additionalCpText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private AvatarStats avatarStats = null;

        [SerializeField]
        private Button closeButton = null;

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
        private Coroutine _disableCpTween;
        private GameObject _cachedCharacterTitle;

        private readonly ReactiveProperty<bool> IsTweenEnd = new ReactiveProperty<bool>(true);

        public bool HasNotification => inventoryView.HasNotification;

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

            inventoryView.SetAction(ShowItemTooltip, Equip,
                () =>
                {
                    costumeSlots.gameObject.SetActive(false);
                    equipmentSlots.gameObject.SetActive(true);
                },
                () =>
                {
                    costumeSlots.gameObject.SetActive(true);
                    equipmentSlots.gameObject.SetActive(false);
                });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Destroy(_cachedCharacterTitle);
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            IsTweenEnd.Value = false;
            CreatePlayer(currentAvatarState);
            _player.gameObject.SetActive(true);
            UpdateSlotView(currentAvatarState);
            UpdateStatViews();
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
            _player?.gameObject.SetActive(false);
            _player = null;
        }

        #endregion

        private void CreatePlayer(AvatarState avatarState)
        {
            _player = PlayerFactory.Create(avatarState).GetComponent<Player>();
            var t = _player.transform;
            t.localScale = Vector3.one;
            t.position = PlayerPosition;
        }

        private void UpdateUIPlayer()
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _player.Set(currentAvatarState);
        }

        private void UpdateSlotView(AvatarState avatarState)
        {
            var game = Game.Game.instance;
            var playerModel = _player.Model;

            nicknameText.text = string.Format(
                NicknameTextFormat,
                avatarState.level,
                avatarState.NameWithHash);

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);

            if (!(title is null))
            {
                Destroy(_cachedCharacterTitle);
                var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                    title.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            costumeSlots.SetPlayerCostumes(playerModel, OnClickSlot, OnDoubleClickSlot);
            equipmentSlots.SetPlayerEquipments(playerModel, OnClickSlot, OnDoubleClickSlot);

            var currentAvatarState = game.States.CurrentAvatarState;
            if (avatarState.Equals(currentAvatarState))
            {
                var currentPlayer = game.Stage.SelectedPlayer;
                cpText.text = CPHelper.GetCP(currentPlayer.Model, game.TableSheets.CostumeStatSheet)
                    .ToString();
            }

            UpdateUIPlayer();
        }

        private void OnClickSlot(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                inventoryView.Focus(slot.ItemType, slot.ItemSubType);
            }
            else
            {
                if (!inventoryView.TryGetItemViewModel(slot.Item, out var model))
                {
                    return;
                }

                inventoryView.DisableFocus();
                ShowItemTooltip(model, slot.RectTransform);
            }
        }

        private void OnDoubleClickSlot(EquipmentSlot slot)
        {
            Unequip(slot, false);
        }

        private void UpdateStatViews()
        {
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();

            var costumeIds = costumeSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item.Id)
                .ToList();

            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var statModifiers = new List<StatModifier>();
            foreach (var itemId in costumeIds)
            {
                var stat = costumeStatSheet.OrderedList
                    .Where(r => r.CostumeId == itemId)
                    .Select(row => new StatModifier(row.StatType, StatModifier.OperationType.Add, (int)row.Stat));
                statModifiers.AddRange(stat);
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            _player.Set(currentAvatarState);
            var stats = _player.Model.Stats.SetAll(_player.Model.Stats.Level, equipments,
                null, Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet);
            stats.SetOption(statModifiers);
            avatarStats.SetData(stats);
            UpdateUIPlayer();
        }

        private IEnumerator CoDisableIncreasedCp()
        {
            yield return new WaitForSeconds(1.5f);
            additionalCpArea.gameObject.SetActive(false);
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
            LocalStateItemEquipModify(slotItem, false);

            var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            cpTextValueTweener.Play(prevCp, currentCp);

            var player = considerInventoryOnly
                ? null
                : Game.Game.instance.Stage.GetPlayer();

            if (!considerInventoryOnly)
            {
                switch (slotItem)
                {
                    default:
                        return;
                    case Costume costume:
                        UpdateStatViews();
                        player.UnequipCostume(costume, true);
                        player.EquipEquipmentsAndUpdateCustomize((Armor)_armorSlot.Item,
                            (Weapon)_weaponSlot.Item);
                        Game.Event.OnUpdatePlayerEquip.OnNext(player);

                        if (costume.ItemSubType == ItemSubType.Title)
                        {
                            Destroy(_cachedCharacterTitle);
                        }

                        break;
                    case Equipment _:
                        UpdateStatViews();
                        switch (slot.ItemSubType)
                        {
                            case ItemSubType.Armor:
                            {
                                player.EquipEquipmentsAndUpdateCustomize((Armor)_armorSlot.Item,
                                    (Weapon)_weaponSlot.Item);
                                break;
                            }
                            case ItemSubType.Weapon:
                                player.EquipWeapon((Weapon)_weaponSlot.Item);
                                break;
                        }

                        Game.Event.OnUpdatePlayerEquip.OnNext(player);
                        break;
                }
            }

            PostEquipOrUnequip(slot);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
            Find<HeaderMenuStatic>().UpdateInventoryNotification(HasNotification);
        }

        private void LocalStateItemEquipModify(ItemBase itemBase, bool equip)
        {
            if (!(itemBase is INonFungibleItem nonFungibleItem))
            {
                return;
            }

            LocalLayerModifier.SetItemEquip(States.Instance.CurrentAvatarState.address,
                nonFungibleItem.NonFungibleId, equip);
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

        private (string, bool, System.Action, System.Action) GetToolTipParams(
            InventoryItemViewModel model)
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
                        interactable = !model.Disabled.Value;
                    }

                    submit = () => Equip(model);

                    if (Game.Game.instance.Stage.IsInStage)
                    {
                        blocked = () => ShowNotification("UI_BLOCK_EQUIP");
                    }
                    else
                    {
                        blocked = () => ShowNotification("UI_EQUIP_FAILED");
                    }
                    break;
                case ItemType.Material:
                    if (item.ItemSubType == ItemSubType.ApStone)
                    {
                        submitText = L10nManager.Localize("UI_CHARGE_AP");
                        interactable = IsInteractableMaterial();

                        if (States.Instance.CurrentAvatarState.actionPoint > 0)
                        {
                            submit = () => ShowRefillConfirmPopup(item);
                        }
                        else
                        {
                            submit = () => ChargeActionPoint(item);
                        }

                        if (Game.Game.instance.Stage.IsInStage)
                        {
                            blocked = () => ShowNotification("UI_BLOCK_CHARGE_AP");
                        }
                        else
                        {
                            blocked = () => ShowNotification("UI_AP_IS_FULL");
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (submitText, interactable, submit, blocked);
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

        private void ShowNotification(string message,
            NotificationCell.NotificationType notificationType =
                NotificationCell.NotificationType.Alert)
        {
            NotificationSystem.Push(MailType.System, L10nManager.Localize(message),
                notificationType);
        }

        private void Equip(InventoryItemViewModel inventoryItem)
        {
            if (Game.Game.instance.Stage.IsInStage)
            {
                return;
            }

            if (inventoryItem.Disabled.Value)
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
            LocalStateItemEquipModify(slot.Item, true);

            if (!(_disableCpTween is null))
                StopCoroutine(_disableCpTween);
            additionalCpArea.gameObject.SetActive(false);

            var currentCp = CPHelper.GetCPV2(currentAvatarState, characterSheet, costumeStatSheet);
            cpTextValueTweener.Play(prevCp, currentCp);
            if (prevCp < currentCp)
            {
                additionalCpArea.gameObject.SetActive(true);
                additionalCpText.text = (currentCp - prevCp).ToString();
                _disableCpTween = StartCoroutine(CoDisableIncreasedCp());
            }

            var player = Game.Game.instance.Stage.GetPlayer();
            switch (itemBase)
            {
                default:
                    return;
                case Costume costume:
                {
                    player.EquipCostume(costume);
                    UpdateStatViews();
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
                    UpdateStatViews();
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

        private void ShowRefillConfirmPopup(ItemBase itemBase)
        {
            var confirm = Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK", "UI_CANCEL",
                true, IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = () => ChargeActionPoint(itemBase);
            confirm.CancelCallback = () => confirm.Close();
        }

        private void ChargeActionPoint(ItemBase itemBase)
        {
            if (!(itemBase is Nekoyume.Model.Item.Material material))
            {
                return;
            }

            ShowNotification("UI_CHARGE_AP", NotificationCell.NotificationType.Information);
            Game.Game.instance.ActionManager.ChargeActionPoint(material).Subscribe();
            var address = States.Instance.CurrentAvatarState.address;
            if (GameConfigStateSubject.ActionPointState.ContainsKey(address))
            {
                GameConfigStateSubject.ActionPointState.Remove(address);
            }

            GameConfigStateSubject.ActionPointState.Add(address, true);
        }

        private void ShowItemTooltip(InventoryItemViewModel model, RectTransform target)
        {
            var tooltip = Find<ItemTooltip>();
            var (submitText, interactable, submit, blocked) = GetToolTipParams(model);
            tooltip.Show(target, model, submitText, interactable,
                submit, () => inventoryView.ClearSelectedItem(), blocked);
        }

        public void TutorialActionClickAvatarInfoFirstInventoryCellView()
        {
            if (inventoryView.TryGetFirstCell(out var item))
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
    }
}
