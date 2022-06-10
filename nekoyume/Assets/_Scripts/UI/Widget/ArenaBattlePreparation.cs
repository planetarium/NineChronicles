using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
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
using Inventory = Nekoyume.UI.Module.Inventory;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class ArenaBattlePreparation : Widget
    {
        private static readonly Vector3 PlayerPosition = new Vector3(1999.8f, 1999.3f, 3f);

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private EquipmentSlots equipmentSlots;

        [SerializeField]
        private EquipmentSlots costumeSlots;

        [SerializeField]
        private Transform titleSocket;

        [SerializeField]
        private AvatarStats stats;

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

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick;

        [SerializeField]
        private GameObject blockStartingTextObject;

        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private int _championshipId;
        private int _round;
        private AvatarState _chooseAvatarState;
        private bool _shouldResetPlayer = true;
        private Player _player;
        private GameObject _cachedCharacterTitle;
        private int _ticketCountToUse = 1;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay
        {
            get
            {
                var blockIndex = Game.Game.instance.Agent.BlockIndex;
                var currentRound =
                    TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
                var ticketCount = RxProps.PlayersArenaParticipant.HasValue
                    ? RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo.GetTicketCount(
                        Game.Game.instance.Agent.BlockIndex,
                        currentRound.StartBlockIndex,
                        States.Instance.GameConfigState.DailyArenaInterval)
                    : 0;
                return ticketCount >= _ticketCountToUse;
            }
        }

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

            startButton.SetCost(CostType.ArenaTicket, _ticketCountToUse);

            closeButton.onClick.AddListener(() =>
            {
                Close();
                Find<ArenaBoard>().Show();
                AudioController.PlayClick();
            });

            CloseWidget = () => Close(true);

            startButton.OnSubmitSubject
                .Where(_ => !Game.Game.instance.Stage.IsInStage)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            int championshipId,
            int round,
            AvatarState chooseAvatarState,
            bool ignoreShowAnimation = false)
        {
            _championshipId = championshipId;
            _round = round;
            _chooseAvatarState = chooseAvatarState;

            var stage = Game.Game.instance.Stage;
            stage.IsRepeatStage = false;

            // NOTE: [`stage.SelectedPlayer`] should be set to null
            // before calling [`stage.GetPlayer()`]
            stage.SelectedPlayer = null;
            _player = stage.GetPlayer(PlayerPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Player>();
            }

            var playerAvatarState = RxProps.PlayersArenaParticipant.Value.AvatarState;
            if (_shouldResetPlayer)
            {
                _shouldResetPlayer = false;
                _player.Set(playerAvatarState);
                _player.gameObject.SetActive(false);
                _player.gameObject.SetActive(true);
                _player.SpineController.Appear();
            }

            UpdateInventory();
            UpdateTitle();
            UpdateStat(playerAvatarState);
            UpdateSlot(playerAvatarState);
            UpdateStartButton(playerAvatarState);

            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);
            ReactiveAvatarState.ActionPoint
                .Subscribe(_ => ReadyToBattle())
                .AddTo(_disposables);
            ReactiveAvatarState.Inventory
                .Subscribe(_ =>
                {
                    UpdateSlot(Game.Game.instance.States.CurrentAvatarState);
                    UpdateStartButton(Game.Game.instance.States.CurrentAvatarState);
                })
                .AddTo(_disposables);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _shouldResetPlayer = true;
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
            var title = _player.Costumes.FirstOrDefault(x =>
                x.ItemSubType == ItemSubType.Title &&
                x.Equipped);
            if (title is null)
            {
                return;
            }

            Destroy(_cachedCharacterTitle);
            var clone = ResourcesHelper.GetCharacterTitle(
                title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        private void UpdateSlot(AvatarState avatarState)
        {
            _player.Set(avatarState);
            equipmentSlots.SetPlayerEquipments(
                _player.Model,
                OnClickSlot,
                OnDoubleClickSlot,
                ElementalTypeExtension.GetAllTypes());
            costumeSlots.SetPlayerCostumes(
                _player.Model,
                OnClickSlot,
                OnDoubleClickSlot);
        }

        private void UpdateStat(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var equipments = _player.Equipments;
            var costumes = _player.Costumes;
            var equipmentSetEffectSheet =
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var s = _player.Model.Stats.SetAll(
                _player.Model.Stats.Level,
                equipments, costumes, null,
                equipmentSetEffectSheet, costumeSheet);
            stats.SetData(s);
        }

        private void OnClickSlot(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                inventory.Focus(
                    slot.ItemType,
                    slot.ItemSubType,
                    ElementalTypeExtension.GetAllTypes());
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
            LocalLayerModifier.SetItemEquip(
                currentAvatarState.address,
                slot.Item,
                true);

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
                        var clone = ResourcesHelper.GetCharacterTitle(
                            costume.Grade,
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
            if (slot.IsEmpty)
            {
                return;
            }

            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var slotItem = slot.Item;
            slot.Clear();
            LocalLayerModifier.SetItemEquip(
                currentAvatarState.address,
                slotItem,
                false);

            if (!considerInventoryOnly)
            {
                var selectedPlayer = Game.Game.instance.Stage.GetPlayer();
                switch (slotItem)
                {
                    default:
                        return;
                    case Costume costume:
                        selectedPlayer.UnequipCostume(
                            costume,
                            true);
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
                }
            }

            PostEquipOrUnequip(slot);
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            var (submitText, interactable, submit, blocked)
                = GetToolTipParams(model);
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
                        interactable = !model.LevelLimited.Value ||
                                       model.LevelLimited.Value &&
                                       model.Equipped.Value;
                    }

                    submit = () => Equip(model);
                    blocked = () => NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_EQUIP_FAILED"),
                        NotificationCell.NotificationType.Alert);

                    break;
                case ItemType.Consumable:
                case ItemType.Material:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (submitText, interactable, submit, blocked);
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

        private void OnClickBattle()
        {
            var stage = Game.Game.instance.Stage;
            if (stage.IsInStage)
            {
                return;
            }

            stage.IsInStage = true;
            stage.IsShowHud = true;
            StartCoroutine(CoBattleStart());
            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart()
        {
            var crystalImage = Find<HeaderMenuStatic>().ArenaTickets.IconImage;
            var itemMoveAnimation = ItemMoveAnimation.Show(
                crystalImage.sprite,
                crystalImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            // LocalLayerModifier.ModifyAgentCrystalAsync(
            //     States.Instance.CurrentAvatarState.address,
            //     -_ticketCountToUse).Forget();
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);

            SendBattleArenaAction();
            AudioController.PlayClick();
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
                case ItemType.Equipment:
                    return equipmentSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Consumable:
                case ItemType.Material:
                default:
                    slot = null;
                    return false;
            }
        }

        private bool TryToFindSlotToEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Equipment:
                    return equipmentSlots.TryGetToEquip((Equipment)item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetToEquip((Costume)item, out slot);
                case ItemType.Consumable:
                case ItemType.Material:
                default:
                    slot = null;
                    return false;
            }
        }

        private void SendBattleArenaAction()
        {
            Find<ArenaBattleLoadingScreen>().Show(
                _chooseAvatarState.NameWithHash,
                _chooseAvatarState.level,
                _chooseAvatarState.inventory.GetEquippedFullCostumeOrArmorId());

            startButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);
            ActionRenderHandler.Instance.Pending = true;
            ActionManager.Instance.BattleArena(
                    _chooseAvatarState.address,
                    _player.Costumes
                        .Select(e => e.NonFungibleId)
                        .ToList(),
                    _player.Equipments
                        .Select(e => e.NonFungibleId)
                        .ToList(),
                    _championshipId,
                    _round,
                    _ticketCountToUse)
                .Subscribe();
        }

        public void OnRenderBattleArena(
            ActionBase.ActionEvaluation<BattleArena> eval,
            BattleLog battleLog,
            List<ItemBase> rewards)
        {
            if (eval.Exception is { })
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close(true);
            Find<ArenaBattleLoadingScreen>().Close();
            Game.Event.OnRankingBattleStart.Invoke((battleLog, rewards));
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);
        }

        private void UpdateStartButton(AvatarState avatarState)
        {
            _player.Set(avatarState);
            var canBattle = Util.CanBattle(
                _player,
                Array.Empty<int>());
            startButton.gameObject.SetActive(canBattle);
            blockStartingTextObject.SetActive(!canBattle);
        }
    }
}
