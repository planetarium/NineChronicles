using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Game.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
using Inventory = Nekoyume.UI.Module.Inventory;
using Toggle = Nekoyume.UI.Module.Toggle;
using Material = Nekoyume.Model.Item.Material;
using Skill = Nekoyume.Model.Skill.Skill;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class BattlePreparation : Widget
    {
        private static readonly Vector3 PlayerPosition = new(1999.8f, 1999.3f, 3f);

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
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private ParticleSystem[] particles;

        [SerializeField]
        private TMP_InputField levelField;

        [SerializeField]
        private ConditionalCostButton startButton;

        [SerializeField]
        private BonusBuffButton randomBuffButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button simulateButton;

        [SerializeField]
        private Transform buttonStarImageTransform;

        [SerializeField]
        private Toggle repeatToggle; // It is not currently in use

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
        private Button sweepPopupButton;

        [SerializeField]
        private TextMeshProUGUI sweepButtonText;

        [SerializeField]
        private Button boostPopupButton;

        [SerializeField]
        private GameObject mimisbrunnrBg;

        [SerializeField]
        private GameObject eventDungeonBg;

        [SerializeField]
        private GameObject hasBg;

        [SerializeField]
        private GameObject blockStartingTextObject;

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
        private GameObject _cachedCharacterTitle;

        private StageType _stageType;
        private int? _scheduleId;
        private int _worldId;
        private int _stageId;
        private int _requiredCost;
        private bool _shouldResetPlayer = true;
        private bool _trackGuideQuest;

        private readonly List<IDisposable> _disposables = new();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay => _stageType switch
        {
            StageType.EventDungeon =>
                RxProps.EventDungeonTicketProgress.Value.currentTickets >= _requiredCost,
            _ =>
                States.Instance.CurrentAvatarState.actionPoint >= _requiredCost,
        };

        private bool IsFirstStage => _stageType switch
        {
            StageType.EventDungeon => _stageId.ToEventDungeonStageNumber() == 1,
            _ => _stageId == 1,
        };

        #region override

        protected override void Awake()
        {
            base.Awake();
            simulateButton.gameObject.SetActive(GameConfig.IsEditor);
            levelField.gameObject.SetActive(GameConfig.IsEditor);
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
                .Where(_ => !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            sweepPopupButton.OnClickAsObservable()
                .Where(_ => !IsFirstStage)
                .Subscribe(_ => Find<SweepPopup>().Show(_worldId, _stageId, SendBattleAction));

            boostPopupButton.OnClickAsObservable()
                .Where(_ => EnoughToPlay && !Game.Game.instance.IsInWorld)
                .Subscribe(_ => ShowBoosterPopup());

            boostPopupButton.OnClickAsObservable().Where(_ => !EnoughToPlay && !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("ERROR_ACTION_POINT"),
                        NotificationCell.NotificationType.Alert))
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            StageType stageType,
            int worldId,
            int stageId,
            string closeButtonName,
            bool ignoreShowAnimation = false,
            bool showByGuideQuest = false)
        {
            _trackGuideQuest = showByGuideQuest;
            Analyzer.Instance.Track("Unity/Click Stage", new Value
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
            });

            var stage = Game.Game.instance.Stage;
            repeatToggle.isOn = false;
            repeatToggle.interactable = true;

            _player = stage.GetPlayer(PlayerPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Player>();
            }

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (_shouldResetPlayer)
            {
                _shouldResetPlayer = false;
                _player.gameObject.SetActive(false);
                _player.gameObject.SetActive(true);
                _player.SpineController.Appear();
                _player.Set(currentAvatarState);
            }

            _stageType = stageType;
            _worldId = worldId;
            _stageId = stageId;

            UpdateInventory(stageType is StageType.HackAndSlash or StageType.Mimisbrunnr);
            UpdateBackground();
            UpdateTitle();
            UpdateStat(currentAvatarState);
            UpdateSlot(currentAvatarState, true);
            UpdateStartButton(currentAvatarState);
            UpdateRequiredCostByStageId();
            UpdateRandomBuffButton();

            closeButtonText.text = closeButtonName;
            sweepButtonText.text =
                States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId)
                    ? "Sweep"
                    : "Repeat";
            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);
            ShowHelpTooltip(_stageType);

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                    ReactiveAvatarState.ActionPoint
                        .Subscribe(_ => UpdateStartButton())
                        .AddTo(_disposables);
                    break;
                case StageType.EventDungeon:
                    RxProps.EventScheduleRowForDungeon
                        .Subscribe(value => _scheduleId = value?.Id)
                        .AddTo(_disposables);
                    RxProps.EventDungeonTicketProgress
                        .Subscribe(_ => UpdateStartButton())
                        .AddTo(_disposables);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ReactiveAvatarState.Inventory.Subscribe(_ =>
            {
                UpdateSlot(States.Instance.CurrentAvatarState);
                UpdateStartButton(States.Instance.CurrentAvatarState);
            }).AddTo(_disposables);

            base.Show(ignoreShowAnimation);
        }

        private void UpdateRandomBuffButton()
        {
            if (_stageType == StageType.EventDungeon)
            {
                randomBuffButton.gameObject.SetActive(false);
                return;
            }

            randomBuffButton.SetData(States.Instance.CrystalRandomSkillState, _stageId);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _shouldResetPlayer = true;
            consumableSlots.Clear();
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateInventory(bool useConsumable)
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
                GetElementalTypes(),
                useConsumable: useConsumable);
        }

        private void UpdateBackground()
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    hasBg.SetActive(true);
                    mimisbrunnrBg.SetActive(false);
                    eventDungeonBg.SetActive(false);
                    break;
                case StageType.Mimisbrunnr:
                    hasBg.SetActive(false);
                    mimisbrunnrBg.SetActive(true);
                    eventDungeonBg.SetActive(false);
                    break;
                case StageType.EventDungeon:
                    hasBg.SetActive(false);
                    mimisbrunnrBg.SetActive(false);
                    eventDungeonBg.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                GetElementalTypes());
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
                TableSheets.Instance.EquipmentItemSetEffectSheet;
            var costumeSheet = TableSheets.Instance.CostumeStatSheet;
            var s = _player.Model.Stats.SetAll(_player.Model.Stats.Level,
                equipments, costumes, consumables,
                equipmentSetEffectSheet, costumeSheet);
            stats.SetData(s);
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

            var currentAvatarState = States.Instance.CurrentAvatarState;
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
                case Consumable _:
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

            var currentAvatarState = States.Instance.CurrentAvatarState;
            var slotItem = slot.Item;
            slot.Clear();
            LocalLayerModifier.SetItemEquip(currentAvatarState.address, slotItem, false);

            if (!considerInventoryOnly)
            {
                var selectedPlayer = Game.Game.instance.Stage.GetPlayer();
                switch (slotItem)
                {
                    case Consumable _:
                        Game.Event.OnUpdatePlayerEquip.OnNext(selectedPlayer);
                        break;
                    case Costume costume:
                        selectedPlayer.UnequipCostume(costume, true);
                        selectedPlayer.EquipEquipmentsAndUpdateCustomize(
                            (Armor)_armorSlot.Item,
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
                    default:
                        return;
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
                            submit = () =>
                                ActionManager.Instance.ChargeActionPoint(item as Material)
                                    .Subscribe();
                        }

                        blocked = () => NotificationSystem.Push(MailType.System,
                            L10nManager.Localize("UI_AP_IS_FULL"),
                            NotificationCell.NotificationType.Alert);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (submitText, interactable, submit, blocked);
        }

        private static bool IsInteractableMaterial()
        {
            if (Find<HeaderMenuStatic>().ChargingAP) // is charging?
            {
                return false;
            }

            return States.Instance.CurrentAvatarState.actionPoint !=
                   States.Instance.GameConfigState.ActionPointMax;
        }

        private void UpdateStartButton()
        {
            startButton.UpdateObjects();
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
        }

        private void UpdateRequiredCostByStageId()
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    TableSheets.Instance.StageSheet.TryGetValue(
                        _stageId, out var stage, true);
                    _requiredCost = stage.CostAP;
                    var stakingLevel = States.Instance.StakingLevel;
                    if (_stageType is StageType.HackAndSlash && stakingLevel > 0)
                    {
                        _requiredCost =
                            TableSheets.Instance.StakeActionPointCoefficientSheet
                                .GetActionPointByStaking(
                                    _requiredCost,
                                    1,
                                    stakingLevel);
                    }

                    startButton.SetCost(CostType.ActionPoint, _requiredCost);
                    break;
                }
                case StageType.EventDungeon:
                {
                    _requiredCost = 1;
                    startButton.SetCost(CostType.EventDungeonTicket, _requiredCost);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                {
                    StartCoroutine(CoBattleStart(
                        _stageType,
                        CostType.ActionPoint));
                    break;
                }
                case StageType.Mimisbrunnr:
                {
                    if (!CheckEquipmentElementalType())
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                            NotificationCell.NotificationType.UnlockCondition);
                        return;
                    }

                    StartCoroutine(CoBattleStart(
                        _stageType,
                        CostType.ActionPoint));
                    break;
                }
                case StageType.EventDungeon:
                {
                    if (!_scheduleId.HasValue)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                            NotificationCell.NotificationType.Information);

                        return;
                    }

                    if (RxProps.EventDungeonTicketProgress.Value.currentTickets >=
                        _requiredCost)
                    {
                        StartCoroutine(CoBattleStart(
                            _stageType,
                            CostType.EventDungeonTicket));
                        break;
                    }

                    var ncgHas = States.Instance.GoldBalanceState.Gold;
                    var ncgCost = RxProps.EventScheduleRowForDungeon.Value
                        .GetDungeonTicketCost(
                            RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0,
                            States.Instance.GoldBalanceState.Gold.Currency);
                    if (ncgHas >= ncgCost)
                    {
                        // FIXME: `UI_CONFIRM_PAYMENT_CURRENCY_FORMAT_FOR_BATTLE_ARENA` key
                        //        is temporary.
                        var notEnoughTicketMsg = L10nManager.Localize(
                            "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT_FOR_BATTLE_ARENA",
                            ncgCost.ToString());
                        Find<PaymentPopup>().ShowAttract(
                            CostType.EventDungeonTicket,
                            _requiredCost.ToString(),
                            notEnoughTicketMsg,
                            L10nManager.Localize("UI_YES"),
                            () => StartCoroutine(
                                CoBattleStart(
                                    StageType.EventDungeon,
                                    CostType.NCG,
                                    true)));

                        return;
                    }

                    var notEnoughNCGMsg =
                        L10nManager.Localize("UI_NOT_ENOUGH_NCG_WITH_SUPPLIER_INFO");
                    Find<PaymentPopup>().ShowAttract(
                        CostType.NCG,
                        ncgCost.GetQuantityString(),
                        notEnoughNCGMsg,
                        L10nManager.Localize("UI_GO_TO_MARKET"),
                        () => GoToMarket(TradeType.Sell));
                    return;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            repeatToggle.interactable = false;
            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart(
            StageType stageType,
            CostType costType,
            bool buyTicketIfNeeded = false)
        {
            var game = Game.Game.instance;
            game.IsInWorld = true;
            game.Stage.IsShowHud = true;

            var headerMenuStatic = Find<HeaderMenuStatic>();
            var currencyImage = costType switch
            {
                CostType.NCG => headerMenuStatic.Gold.IconImage,
                CostType.ActionPoint => headerMenuStatic.ActionPoint.IconImage,
                CostType.Hourglass => headerMenuStatic.Hourglass.IconImage,
                CostType.Crystal => headerMenuStatic.Crystal.IconImage,
                CostType.ArenaTicket => headerMenuStatic.ArenaTickets.IconImage,
                CostType.WorldBossTicket => headerMenuStatic.WorldBossTickets.IconImage,
                CostType.EventDungeonTicket => headerMenuStatic.EventDungeonTickets.IconImage,
                _ or CostType.None => throw new ArgumentOutOfRangeException(
                    nameof(costType), costType, null)
            };
            var itemMoveAnimation = ItemMoveAnimation.Show(
                currencyImage.sprite,
                currencyImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);

            SendBattleAction(
                stageType,
                buyTicketIfNeeded: buyTicketIfNeeded);
        }

        private void ShowBoosterPopup()
        {
            if (_stageType == StageType.Mimisbrunnr && !CheckEquipmentElementalType())
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item).ToList();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            Find<BoosterPopup>().Show(
                stage,
                costumes,
                equipments,
                consumables,
                GetBoostMaxCount(_stageId),
                _worldId,
                _stageId);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(States.Instance.CurrentAvatarState);
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
            Find<HeaderMenuStatic>().UpdateInventoryNotification(inventory.HasNotification);
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

        private void SendBattleAction(
            StageType stageType,
            int playCount = 1,
            bool buyTicketIfNeeded = false)
        {
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<LoadingScreen>().Show();

            startButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item).ToList();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            switch (stageType)
            {
                case StageType.HackAndSlash:
                {
                    var skillState = States.Instance.CrystalRandomSkillState;
                    var skillId = PlayerPrefs.GetInt("HackAndSlash.SelectedBonusSkillId", 0);
                    if (skillId == 0)
                    {
                        if (skillState == null ||
                            !skillState.SkillIds.Any())
                        {
                            ActionManager.Instance.HackAndSlash(
                                costumes,
                                equipments,
                                consumables,
                                _worldId,
                                _stageId,
                                playCount: playCount,
                                trackGuideQuest: _trackGuideQuest
                            ).Subscribe();
                            break;
                        }

                        skillId = skillState.SkillIds
                            .Select(buffId =>
                                TableSheets.Instance.CrystalRandomBuffSheet
                                    .TryGetValue(buffId, out var bonusBuffRow)
                                    ? bonusBuffRow
                                    : null)
                            .Where(x => x != null)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.Id)
                            .First()
                            .Id;
                    }

                    ActionManager.Instance.HackAndSlash(
                        costumes,
                        equipments,
                        consumables,
                        _worldId,
                        _stageId,
                        skillId,
                        playCount,
                        _trackGuideQuest
                    ).Subscribe();
                    PlayerPrefs.SetInt("HackAndSlash.SelectedBonusSkillId", 0);
                    break;
                }
                case StageType.Mimisbrunnr:
                {
                    ActionManager.Instance.MimisbrunnrBattle(
                        costumes,
                        equipments,
                        consumables,
                        _worldId,
                        _stageId,
                        1
                    ).Subscribe();
                    break;
                }
                case StageType.EventDungeon:
                {
                    if (!_scheduleId.HasValue)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                            NotificationCell.NotificationType.Information);
                        break;
                    }

                    ActionManager.Instance.EventDungeonBattle(
                            _scheduleId.Value,
                            _worldId,
                            _stageId,
                            equipments,
                            costumes,
                            consumables,
                            buyTicketIfNeeded,
                            _trackGuideQuest)
                        .Subscribe();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToMarket(TradeType tradeType)
        {
            Close(true);
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            switch (tradeType)
            {
                case TradeType.Buy:
                    Find<ShopBuy>().Show();
                    break;
                case TradeType.Sell:
                    Find<ShopSell>().Show();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tradeType), tradeType, null);
            }
        }

        private static void ShowRefillConfirmPopup(Material material)
        {
            var confirm = Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton(
                "UI_CONFIRM",
                "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK",
                "UI_CANCEL",
                true,
                IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = () =>
                ActionManager.Instance.ChargeActionPoint(material).Subscribe();
            confirm.CancelCallback = () => confirm.Close();
        }

        private static int GetBoostMaxCount(int stageId)
        {
            if (!TableSheets.Instance.GameConfigSheet.TryGetValue(
                    "action_point_max",
                    out var ap))
            {
                return 1;
            }

            var stage = TableSheets.Instance.StageSheet.OrderedList
                .FirstOrDefault(i => i.Id == stageId);
            if (stage is null)
            {
                return 1;
            }

            var maxActionPoint = TableExtensions.ParseInt(ap.Value);
            return maxActionPoint / stage.CostAP;
        }

        private static void ShowHelpTooltip(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.HackAndSlash:
                    HelpTooltip.HelpMe(100004, true);
                    break;
                case StageType.Mimisbrunnr:
                    HelpTooltip.HelpMe(100020, true);
                    break;
                case StageType.EventDungeon:
                    // ignore.
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        private bool CheckEquipmentElementalType()
        {
            var elementalTypes = GetElementalTypes();
            return _player.Equipments.All(x =>
                elementalTypes.Contains(x.ElementalType));
        }

        private void UpdateStartButton(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var foodIds = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item.Id);
            var canBattle = Util.CanBattle(_player, foodIds);
            startButton.gameObject.SetActive(canBattle);

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    boostPopupButton.gameObject.SetActive(false);
                    sweepPopupButton.gameObject.SetActive(!IsFirstStage);
                    break;
                case StageType.Mimisbrunnr:
                    boostPopupButton.gameObject.SetActive(canBattle);
                    sweepPopupButton.gameObject.SetActive(false);
                    break;
                case StageType.EventDungeon:
                    boostPopupButton.gameObject.SetActive(false);
                    sweepPopupButton.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            blockStartingTextObject.SetActive(!canBattle);
        }

        public List<ElementalType> GetElementalTypes()
        {
            if (_stageType != StageType.Mimisbrunnr)
            {
                return ElementalTypeExtension.GetAllTypes();
            }

            var mimisbrunnrSheet = TableSheets.Instance.MimisbrunnrSheet;
            return mimisbrunnrSheet.TryGetValue(_stageId, out var mimisbrunnrSheetRow)
                ? mimisbrunnrSheetRow.ElementalTypes
                : ElementalTypeExtension.GetAllTypes();
        }

        public void SimulateBattle()
        {
            var level = States.Instance.CurrentAvatarState.level;
            if (!string.IsNullOrEmpty(levelField.text))
            {
                level = int.Parse(levelField.text);
            }

            // 레벨 범위가 넘어간 값이면 만렙으로 설정
            if (!TableSheets.Instance.CharacterLevelSheet.ContainsKey(level))
            {
                level = TableSheets.Instance.CharacterLevelSheet.Keys.Last();
            }

            Find<LoadingScreen>().Show();

            startButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var stageId = _stageId;
            if (!TableSheets.Instance.WorldSheet.TryGetByStageId(
                    stageId,
                    out var worldRow))
            {
                throw new KeyNotFoundException(
                    $"WorldSheet.TryGetByStageId() {nameof(stageId)}({stageId})");
            }

            var avatarState = new AvatarState(States.Instance.CurrentAvatarState)
            {
                level = level
            };
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => ((Consumable)slot.Item).ItemId)
                .ToList();
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment)slot.Item)
                .ToList();
            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped)
                .ToList();

            foreach (var equipment in inventoryEquipments)
            {
                equipment.Unequip();
            }

            foreach (var equipment in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(
                        equipment,
                        out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment)outNonFungibleItem).Equip();
            }

            var tableSheets = TableSheets.Instance;
            var random = new Cheat.DebugRandom();
            StageSheet.Row stageRow;
            StageWaveSheet.Row stageWaveRow;
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    stageRow = tableSheets.StageSheet[stageId];
                    stageWaveRow = tableSheets.StageWaveSheet[stageId];
                    break;
                }
                case StageType.EventDungeon:
                {
                    stageRow = tableSheets.EventDungeonStageSheet[stageId];
                    stageWaveRow = tableSheets.EventDungeonStageWaveSheet[stageId];
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var simulator = new StageSimulator(
                random,
                avatarState,
                consumables,
                new List<Skill>(),
                worldRow.Id,
                stageId,
                stageRow,
                stageWaveRow,
                avatarState.worldInformation.IsStageCleared(stageId),
                StageRewardExpHelper.GetExp(avatarState.level, stageId),
                tableSheets.GetSimulatorSheets(),
                tableSheets.EnemySkillSheet,
                tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    random,
                    tableSheets.StageSheet[stageId],
                    tableSheets.MaterialItemSheet)
            );
            simulator.Simulate();
            GoToStage(simulator.Log);
        }
    }
}
