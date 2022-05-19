using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
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
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Game.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.TableData;
using Inventory = Nekoyume.UI.Module.Inventory;
using Toggle = Nekoyume.UI.Module.Toggle;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class BattlePreparation : Widget
    {
        private static readonly Vector3 PlayerPosition = new Vector3(1999.8f, 1999.3f, 3f);

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
        private Button boostPopupButton;

        [SerializeField]
        private GameObject mimisbrunnrBg;

        [SerializeField]
        private GameObject hasBg;

        [SerializeField]
        private GameObject blockStartingTextObject;

        private Stage _stage;
        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private Player _player;
        private GameObject _cachedCharacterTitle;

        private StageType _stageType = StageType.None;
        private int _worldId;
        private int _requiredCost;
        private bool _reset = true;

        private readonly IntReactiveProperty _stageId = new IntReactiveProperty();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay =>
            States.Instance.CurrentAvatarState.actionPoint >= _requiredCost;

        private bool IsStageCleared =>
            States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(_stageId.Value);

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

            _stageId.Subscribe(SubscribeStage).AddTo(gameObject);

            startButton.OnSubmitSubject.Where(_ => !_stage.IsInStage)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickBattle(repeatToggle.isOn))
                .AddTo(gameObject);

            sweepPopupButton.OnClickAsObservable()
                .Where(_ => IsStageCleared)
                .Subscribe(_ => Find<SweepPopup>().Show(_worldId, _stageId.Value));

            sweepPopupButton.OnClickAsObservable().Where(_ => !IsStageCleared)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_SWEEP_UNLOCK_CONDITION"),
                        NotificationCell.NotificationType.Alert))
                .AddTo(gameObject);

            boostPopupButton.OnClickAsObservable()
                .Where(_ => EnoughToPlay && !_stage.IsInStage)
                .Subscribe(_ => ShowBoosterPopup());

            boostPopupButton.OnClickAsObservable().Where(_ => !EnoughToPlay && !_stage.IsInStage)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("ERROR_ACTION_POINT"),
                        NotificationCell.NotificationType.Alert))
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(StageType stageType,
            int worldId,
            int stageId,
            string closeButtonName,
            bool ignoreShowAnimation = false)
        {
            Analyzer.Instance.Track("Unity/Click Stage");

            _stage = Game.Game.instance.Stage;
            _stage.IsRepeatStage = false;
            repeatToggle.isOn = false;
            repeatToggle.interactable = true;

            _player = _stage.GetPlayer(PlayerPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Player>();
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (_reset)
            {
                _reset = false;
                _player.gameObject.SetActive(false);
                _player.gameObject.SetActive(true);
                _player.SpineController.Appear();
                _player.Set(currentAvatarState);
            }

            _stageType = stageType;
            _worldId = worldId;
            _stageId.Value = stageId;

            UpdateInventory();
            UpdateBackground(stageType);
            UpdateTitle();
            UpdateStat(currentAvatarState);
            UpdateSlot(currentAvatarState, true);
            UpdateStartButton(currentAvatarState);

            closeButtonText.text = closeButtonName;
            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);
            ShowHelpTooltip(stageType);
            ReactiveAvatarState.ActionPoint.Subscribe(_ => ReadyToBattle()).AddTo(_disposables);
            ReactiveAvatarState.Inventory.Subscribe(_ =>
            {
                UpdateSlot(Game.Game.instance.States.CurrentAvatarState);
                UpdateStartButton(Game.Game.instance.States.CurrentAvatarState);
            }).AddTo(_disposables);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _reset = true;
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
                GetElementalTypes());
        }

        private void UpdateBackground(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.HackAndSlash:
                    hasBg.SetActive(true);
                    mimisbrunnrBg.SetActive(false);
                    break;
                case StageType.Mimisbrunnr:
                    hasBg.SetActive(false);
                    mimisbrunnrBg.SetActive(true);
                    break;
                case StageType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
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
                                Game.Game.instance.ActionManager.ChargeActionPoint(item as Material)
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

        private bool IsInteractableMaterial()
        {
            if (Find<HeaderMenuStatic>().ChargingAP) // is charging?
            {
                return false;
            }

            return States.Instance.CurrentAvatarState.actionPoint !=
                   States.Instance.GameConfigState.ActionPointMax;
        }


        private void ReadyToBattle()
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

        private void SubscribeStage(int stageId)
        {
            var stage =
                Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(
                    i => i.Id == stageId);
            if (stage is null)
                return;
            _requiredCost = stage.CostAP;
            startButton.SetCost(CostType.ActionPoint, _requiredCost);
        }

        private void OnClickBattle(bool repeat)
        {
            if (_stage.IsInStage)
            {
                return;
            }

            if (_stageType == StageType.Mimisbrunnr && !CheckEquipmentElementalType(_stageId.Value))
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            _stage.IsInStage = true;
            _stage.IsShowHud = true;
            StartCoroutine(CoBattleStart(_stageType, repeat));
            repeatToggle.interactable = false;
            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart(StageType stageType, bool repeat)
        {
            var actionPointImage = Find<HeaderMenuStatic>().ActionPointImage;
            var animation = ItemMoveAnimation.Show(actionPointImage.sprite,
                actionPointImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                -_requiredCost);
            yield return new WaitWhile(() => animation.IsPlaying);

            Battle(stageType, repeat);
            AudioController.PlayClick();
        }

        private void ShowBoosterPopup()
        {
            if (_stageType == StageType.Mimisbrunnr && !CheckEquipmentElementalType(_stageId.Value))
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_MIMISBRUNNR_START_FAILED"),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item).ToList();

            _stage.IsExitReserved = false;
            _stage.IsRepeatStage = false;
            _stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            Find<BoosterPopup>().Show(_stage, costumes, equipments, consumables,
                GetBoostMaxCount(_stageId.Value), _worldId, _stageId.Value);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(Game.Game.instance.States.CurrentAvatarState);
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

        private void Battle(StageType stageType, bool repeat)
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

            _stage.IsExitReserved = false;
            _stage.IsRepeatStage = repeat;
            _stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            switch (stageType)
            {
                case StageType.HackAndSlash:
                    Game.Game.instance.ActionManager.HackAndSlash(
                        costumes,
                        equipments,
                        consumables,
                        _worldId,
                        _stageId.Value
                    ).Subscribe();
                    break;
                case StageType.Mimisbrunnr:
                    Game.Game.instance.ActionManager.MimisbrunnrBattle(
                        costumes,
                        equipments,
                        consumables,
                        _worldId,
                        _stageId.Value,
                        1
                    ).Subscribe();
                    break;
                case StageType.None:
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

        private void ShowRefillConfirmPopup(Material material)
        {
            var confirm = Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton("UI_CONFIRM", "UI_AP_REFILL_CONFIRM_CONTENT",
                "UI_OK", "UI_CANCEL",
                true, IconAndButtonSystem.SystemType.Information);
            confirm.ConfirmCallback = () =>
                Game.Game.instance.ActionManager.ChargeActionPoint(material).Subscribe();
            confirm.CancelCallback = () => confirm.Close();
        }

        private int GetBoostMaxCount(int stageId)
        {
            if (!Game.Game.instance.TableSheets.GameConfigSheet.TryGetValue("action_point_max",
                    out var ap))
            {
                return 1;
            }

            var stage = Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(
                i => i.Id == stageId);

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
                case StageType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        private bool CheckEquipmentElementalType(int stageId)
        {
            return _player.Equipments.All(x => IsExistElementalType(x.ElementalType));
        }

        private bool IsExistElementalType(ElementalType elementalType)
        {
            return GetElementalTypes().Exists(x => x == elementalType);
        }

        private void UpdateStartButton(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var foodIds = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item).Select(food => food.Id);
            var canBattle = Util.CanBattle(_player, foodIds);
            startButton.gameObject.SetActive(canBattle);

            switch (_stageType)
            {
                case StageType.HackAndSlash:
                    boostPopupButton.gameObject.SetActive(false);
                    sweepPopupButton.gameObject.SetActive(avatarState.worldInformation.IsStageCleared(_stageId.Value));
                    break;
                case StageType.Mimisbrunnr:
                    boostPopupButton.gameObject.SetActive(canBattle);
                    sweepPopupButton.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            blockStartingTextObject.SetActive(!canBattle);
        }

        public List<ElementalType> GetElementalTypes()
        {
            var mimisbrunnrSheet = Game.Game.instance.TableSheets.MimisbrunnrSheet;
            if (mimisbrunnrSheet.TryGetValue(_stageId.Value, out var mimisbrunnrSheetRow))
            {
                return mimisbrunnrSheetRow.ElementalTypes;
            }

            return ElementalTypeExtension.GetAllTypes();
        }

        public void SimulateBattle()
        {
            var level = States.Instance.CurrentAvatarState.level;
            if (!string.IsNullOrEmpty(levelField.text))
                level = int.Parse(levelField.text);
            // 레벨 범위가 넘어간 값이면 만렙으로 설정
            if (!Game.Game.instance.TableSheets.CharacterLevelSheet.ContainsKey(level))
            {
                level = Game.Game.instance.TableSheets.CharacterLevelSheet.Keys.Last();
            }

            Find<LoadingScreen>().Show();

            startButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var stageId = _stageId.Value;
            if (!Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stageId,
                    out var worldRow))
                throw new KeyNotFoundException(
                    $"WorldSheet.TryGetByStageId() {nameof(stageId)}({stageId})");

            var avatarState = new AvatarState(States.Instance.CurrentAvatarState) { level = level };
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => ((Consumable)slot.Item).ItemId).ToList();
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment)slot.Item).ToList();
            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped).ToList();

            foreach (var equipment in inventoryEquipments)
            {
                equipment.Unequip();
            }

            foreach (var equipment in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(equipment,
                        out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment)outNonFungibleItem).Equip();
            }

            var tableSheets = Game.Game.instance.TableSheets;
            var simulator = new StageSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                consumables,
                worldRow.Id,
                stageId,
                tableSheets.GetStageSimulatorSheets(),
                tableSheets.CostumeStatSheet
            );
            simulator.Simulate(1);
            GoToStage(simulator.Log);
        }
    }
}
