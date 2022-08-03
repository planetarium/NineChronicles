using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Model.Elemental;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.WorldBoss;
using Inventory = Nekoyume.UI.Module.Inventory;
using Toggle = UnityEngine.UI.Toggle;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class RaidPreparation : Widget
    {
        private static readonly Vector3 PlayerPosition = new(1999.8f, 1999.3f, 3f);

        [SerializeField]
        private Toggle toggle;

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private EquipmentSlots equipmentSlots;

        [SerializeField]
        private EquipmentSlots costumeSlots;

        [SerializeField]
        private EquipmentSlots consumableSlots;

        [SerializeField]
        private Transform titleSocket;

        [SerializeField]
        private AvatarStats stats;

        [SerializeField]
        private ParticleSystem[] particles;

        [SerializeField]
        private ConditionalCostButton startButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Transform buttonStarImageTransform;

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick = null;

        [SerializeField]
        private GameObject blockStartingTextObject;

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
        private GameObject _cachedCharacterTitle;

        private int _requiredCost;

        private readonly List<IDisposable> _disposables = new();
        private HeaderMenuStatic _headerMenu;
        private RaiderState _cachedRaiderState;

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent && startButton.Interactable;

        public bool IsSkipRender => toggle.isOn;

        #region override

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

            foreach (var slot in consumableSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                AudioController.PlayClick();
            });

            CloseWidget = () => Close(true);

            startButton.OnSubmitSubject
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickStartButton())
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
            toggle.gameObject.SetActive(GameConfig.IsEditor);
        }

        public void Show(RaiderState cachedRaiderState, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _headerMenu = Find<HeaderMenuStatic>();
            _cachedRaiderState = cachedRaiderState;

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;

            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            if (_cachedRaiderState is null)
            {
                var cost = GetEntranceFee(currentAvatarState);
                startButton.SetCost(CostType.Crystal, cost);
            }
            else
            {
                startButton.SetCost(CostType.WorldBossTicket, 1);
                startButton.SetCostColor(_headerMenu.WorldBossTickets.RemainTicket > 0);
            }

            if (_player == null)
            {
                _player = PlayerFactory.Create(currentAvatarState).GetComponent<Player>();
            }

            _player.transform.position = PlayerPosition;
            _player.SpineController.Appear();
            _player.Set(currentAvatarState);
            _player.gameObject.SetActive(true);

            UpdateInventory();
            UpdateTitle();
            UpdateStat(currentAvatarState);
            UpdateSlot(currentAvatarState, true);

            foreach (var particle in particles)
            {
                if (startButton.IsSubmittable)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }
            UpdateStartButton(currentAvatarState);


            coverToBlockClick.SetActive(false);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);

            ReactiveAvatarState.Inventory.Subscribe(_ =>
            {
                UpdateSlot(Game.Game.instance.States.CurrentAvatarState);
                UpdateStartButton(Game.Game.instance.States.CurrentAvatarState);
            }).AddTo(_disposables);

            AgentStateSubject.Crystal
                .Subscribe(_ => startButton.UpdateObjects())
                .AddTo(_disposables);
        }

        private static int GetEntranceFee(AvatarState currentAvatarState)
        {
            var worldBossListSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var row = worldBossListSheet.FindRowByBlockIndex(currentBlockIndex);
            var fee = CrystalCalculator.CalculateEntranceFee(
                currentAvatarState.level, row.EntranceFee);
            var cost = Convert.ToInt32(fee.GetQuantityString());
            return cost;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            consumableSlots.Clear();
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateInventory()
        {
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
                ElementalTypeExtension.GetAllTypes());
        }

        private void UpdateTitle()
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

        private void UpdateSlot(AvatarState avatarState, bool isResetConsumableSlot = false)
        {
            _player.Set(avatarState);
            equipmentSlots.SetPlayerEquipments(_player.Model,
                OnClickSlot, OnDoubleClickSlot,
                ElementalTypeExtension.GetAllTypes());
            costumeSlots.SetPlayerCostumes(_player.Model, OnClickSlot, OnDoubleClickSlot);
            if (isResetConsumableSlot)
            {
                consumableSlots.SetPlayerConsumables(_player.Level,OnClickSlot, OnDoubleClickSlot);
            }
        }

        private void UpdateStat(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item).ToList();
            var equipmentSetEffectSheet =
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var s = _player.Model.Stats.SetAll(_player.Model.Stats.Level,
                equipments, costumes, consumables,
                equipmentSetEffectSheet, costumeSheet);
            stats.SetData(s);
        }

        private void OnClickSlot(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                inventory.Focus(slot.ItemType, slot.ItemSubType, ElementalTypeExtension.GetAllTypes());
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
            slot.Set(itemBase, OnClickSlot, OnDoubleClickSlot);
            LocalLayerModifier.SetItemEquip(currentAvatarState.address, slot.Item, true);

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
                case Consumable consumable:
                    break;
            }

            Game.Event.OnUpdatePlayerEquip.OnNext(player);
            PostEquipOrUnequip(slot);
        }

        private void Unequip(EquipmentSlot slot, bool considerInventoryOnly)
        {
            if (slot.IsEmpty)
            {
                return;
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var slotItem = slot.Item;
            slot.Clear();
            LocalLayerModifier.SetItemEquip(currentAvatarState.address, slotItem, false);

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

        private (string, bool, System.Action, System.Action) GetToolTipParams(
            InventoryItem model)
        {
            var item = model.ItemBase;
            var submitText = string.Empty;
            var interactable = false;
            System.Action submit = null;
            System.Action blocked = null;

            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    submitText = model.Equipped.Value
                        ? L10nManager.Localize("UI_UNEQUIP")
                        : L10nManager.Localize("UI_EQUIP");
                    interactable = !model.LevelLimited.Value;
                    submit = () => Equip(model);
                    blocked = () => NotificationSystem.Push(MailType.System,
                        L10nManager.Localize("UI_EQUIP_FAILED"),
                        NotificationCell.NotificationType.Alert);

                    break;
                case ItemType.Costume:
                case ItemType.Equipment:
                    submitText = model.Equipped.Value
                        ? L10nManager.Localize("UI_UNEQUIP")
                        : L10nManager.Localize("UI_EQUIP");
                    if (model.DimObjectEnabled.Value)
                    {
                        interactable = model.Equipped.Value;
                    }
                    else
                    {
                        interactable = !model.LevelLimited.Value || model.LevelLimited.Value && model.Equipped.Value;
                    }
                    submit = () => Equip(model);
                    blocked = () => NotificationSystem.Push(MailType.System,
                        L10nManager.Localize("UI_EQUIP_FAILED"),
                        NotificationCell.NotificationType.Alert);

                    break;
                case ItemType.Material:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (submitText, interactable, submit, blocked);
        }

        private void OnClickStartButton()
        {
            AudioController.PlayClick();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    break;
                case WorldBossStatus.Season:
                    if (_cachedRaiderState is null)
                    {
                        var cost = GetEntranceFee(Game.Game.instance.States.CurrentAvatarState);
                        Find<PaymentPopup>()
                            .ShowWithAddCost("UI_TOTAL_COST", "UI_BOSS_JOIN_THE_SEASON",
                                CostType.Crystal, cost,
                                CostType.WorldBossTicket, 1,
                                () => StartCoroutine(CoRaid(false)));
                    }
                    else
                    {
                        Debug.Log($"[RAID] Ticket : {_headerMenu.WorldBossTickets.RemainTicket}");
                        if (_headerMenu.WorldBossTickets.RemainTicket > 0)
                        {
                            StartCoroutine(CoRaid(false));
                        }
                        else
                        {
                            ShowTicketPurchasePopup(currentBlockIndex);
                        }
                    }

                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CoRaid(bool payNcg)
        {
            coverToBlockClick.SetActive(true);
            var itemMoveAnimation = ShowMoveAnimation();
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);
            Raid(payNcg);
        }

        private ItemMoveAnimation ShowMoveAnimation()
        {
            var tickets = _headerMenu.WorldBossTickets;
            return ItemMoveAnimation.Show(tickets.IconImage.sprite,
                tickets.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
        }

        private void Raid(bool payNcg)
        {
            var equipments = _player.Equipments.Select(c => c.ItemId).ToList();
            var costumes = _player.Costumes.Select(c => c.ItemId).ToList();
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item)
                .Select(c => c.ItemId).ToList();

            ActionManager.Instance.Raid(costumes, equipments,  consumables, payNcg);
            Find<LoadingScreen>().Show();
            Close();
        }

        private void ShowTicketPurchasePopup(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
            {
                return;
            }

            var cur = States.Instance.GoldBalanceState.Gold.Currency;
            var cost = WorldBossHelper.CalculateTicketPrice(row, _cachedRaiderState, cur);
            var balance = States.Instance.GoldBalanceState;
            Find<TicketPurchasePopup>().Show(
                CostType.WorldBossTicket,
                CostType.NCG,
                balance.Gold,
                cost,
                _cachedRaiderState.PurchaseCount,
                row.MaxPurchaseCount,
                () =>
                {
                    coverToBlockClick.SetActive(true);
                    Raid(true);
                });
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(Game.Game.instance.States.CurrentAvatarState);
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
            _headerMenu.UpdateInventoryNotification(inventory.HasNotification);
        }

        private bool TryToFindSlotAlreadyEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    foreach (var consumableSlot in consumableSlots.Where(consumableSlot =>
                                 !consumableSlot.IsLock && !consumableSlot.IsEmpty))
                    {
                        if (!consumableSlot.Item.Equals(item))
                            continue;

                        slot = consumableSlot;
                        return true;
                    }

                    slot = null;
                    return false;
                case ItemType.Equipment:
                    return equipmentSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetAlreadyEquip(item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private bool TryToFindSlotToEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    slot = consumableSlots.FirstOrDefault(s => !s.IsLock && s.IsEmpty);
                    return slot;
                case ItemType.Equipment:
                    return equipmentSlots.TryGetToEquip((Equipment)item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetToEquip((Costume)item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private void UpdateStartButton(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var foodIds = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item).Select(food => food.Id);
            var canBattle = Util.CanBattle(_player, foodIds);
            startButton.gameObject.SetActive(canBattle);
            blockStartingTextObject.SetActive(!canBattle);
        }
    }
}
