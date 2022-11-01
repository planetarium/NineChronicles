using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
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
using Nekoyume.Game.Factory;
using Nekoyume.Model.Elemental;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using TMPro;
using Nekoyume.Model;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Inventory = Nekoyume.UI.Module.Inventory;
using Toggle = UnityEngine.UI.Toggle;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.UI
{
    using UniRx;
    public class RaidPreparation : Widget
    {
        private class PracticeRandom : System.Random, IRandom
        {
            public PracticeRandom() : base(Guid.NewGuid().GetHashCode())
            {
            }

            public int Seed => throw new NotImplementedException();
        }

        private static readonly Vector3 PlayerPosition = new(1999.8f, 1999.3f, 3f);
        private const string RAID_EQUIPMENT_KEY = "RAID_EQUIPMENT";

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
        private Button startButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private TextMeshProUGUI crystalText;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private Transform crystalImage;

        [SerializeField]
        private Transform ticketImage;

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

        [SerializeField]
        private GameObject crystalContainer;

        [SerializeField]
        private GameObject currencyContainer;

        public Player Player { get; private set; }
        private EquipmentSlot _weaponSlot;
        private EquipmentSlot _armorSlot;
        private GameObject _cachedCharacterTitle;
        private AvatarState _avatarState;

        private int _requiredCost;
        private int _bossId;

        private readonly List<IDisposable> _disposables = new();
        private HeaderMenuStatic _headerMenu;

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent && startButton.enabled;

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

            startButton.onClick.AddListener(OnClickStartButton);

            Game.Event.OnRoomEnter.AddListener(b => Close());
            toggle.gameObject.SetActive(GameConfig.IsEditor);
        }

        public void Show(int bossId, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _bossId = bossId;
            _headerMenu = Find<HeaderMenuStatic>();
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            var value = currentAvatarState.inventory.Serialize();
            var cloneInventory = new Nekoyume.Model.Item.Inventory((List)value);
            _avatarState = new AvatarState(currentAvatarState)
            {
                inventory = cloneInventory
            };
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var raiderState = WorldBossStates.GetRaiderState(_avatarState.address);
            startButton.gameObject.SetActive(true);
            startButton.enabled = true;
            if (WorldBossFrontHelper.IsItInSeason(currentBlockIndex))
            {
                currencyContainer.SetActive(true);
                ticketText.color = _headerMenu.WorldBossTickets.RemainTicket > 0 ?
                    Palette.GetColor(ColorType.ButtonEnabled) :
                    Palette.GetColor(ColorType.TextDenial);
                if (raiderState is null)
                {
                    crystalContainer.SetActive(true);
                    UpdateCrystalCost();
                }
                else
                {
                    crystalContainer.SetActive(false);
                }
            }
            else
            {
                currencyContainer.SetActive(false);
                crystalContainer.SetActive(false);
            }

            closeButtonText.text = L10nManager.LocalizeCharacterName(bossId);
            if (Player == null)
            {
                Player = PlayerFactory.Create(_avatarState).GetComponent<Player>();
            }


            Player.transform.position = PlayerPosition;
            Player.SpineController.Appear();
            Player.Set(_avatarState);
            Player.gameObject.SetActive(true);

            _cachedEquipment.Clear();
            _cachedEquipment.AddRange(Player.Model.Equipments.Select(x=> x.ItemId).ToList());
            _cachedEquipment.AddRange(Player.Model.Costumes.Select(x=> x.ItemId).ToList());
            var loadEquipment = LoadEquipment();
            _avatarState.EquipItems(loadEquipment);

            UpdateInventory();
            UpdateTitle();
            UpdateStat(_avatarState);
            UpdateSlot(_avatarState, true);
            UpdateStartButton(_avatarState);


            coverToBlockClick.SetActive(false);
            costumeSlots.gameObject.SetActive(false);
            equipmentSlots.gameObject.SetActive(true);

            AgentStateSubject.Crystal
                .Subscribe(_ => UpdateCrystalCost())
                .AddTo(_disposables);
        }

        private void UpdateCrystalCost()
        {
            var crystalCost = GetEntranceFee(Game.Game.instance.States.CurrentAvatarState);
            crystalText.text = $"{crystalCost:#,0}";
            crystalText.color = States.Instance.CrystalBalance.MajorUnit >= crystalCost ?
                Palette.GetColor(ColorType.ButtonEnabled) :
                Palette.GetColor(ColorType.TextDenial);
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
                ElementalTypeExtension.GetAllTypes(),
                inventoryType:Inventory.InventoryType.Raid,
                _avatarState.inventory,
                onUpdateInventory:OnUpdateInventory,
                useConsumable : true);
        }

        private void UpdateTitle()
        {
            var title = Player.Costumes
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

        private readonly List<Guid> _cachedEquipment = new();
        private void UpdateSlot(AvatarState avatarState, bool isResetConsumableSlot = false)
        {
            Player.Set(avatarState);
            equipmentSlots.SetPlayerEquipments(Player.Model,
                OnClickSlot, OnDoubleClickSlot,
                ElementalTypeExtension.GetAllTypes());
            costumeSlots.SetPlayerCostumes(Player.Model, OnClickSlot, OnDoubleClickSlot);
            if (isResetConsumableSlot)
            {
                consumableSlots.SetPlayerConsumables(Player.Level,OnClickSlot, OnDoubleClickSlot);
            }
        }

        private void UpdateStat(AvatarState avatarState)
        {
            Player.Set(avatarState);
            var equipments = Player.Equipments;
            var costumes = Player.Costumes;
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item).ToList();
            var equipmentSetEffectSheet =
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var s = Player.Model.Stats.SetAll(Player.Model.Stats.Level,
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
            if (!(itemBase is INonFungibleItem nonFungibleItem))
            {
                return;
            }
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

            slot.Set(itemBase, OnClickSlot, OnDoubleClickSlot);
            if (!inventory.TryGetModel(slot.Item, out var targetInventoryItem) ||
                targetInventoryItem.ItemBase is not IEquippableItem equippableItem)
            {
                return;
            }

            equippableItem.Equip();
            inventoryItem.Equipped.Value = true;
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

            var slotItem = slot.Item;
            if (!inventory.TryGetModel(slot.Item, out var targetInventoryItem) ||
                targetInventoryItem.ItemBase is not IEquippableItem equippableItem)
            {
                return;
            }

            slot.Clear();
            targetInventoryItem.Equipped.Value = false;
            equippableItem.Unequip();
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

        private void OnUpdateInventory(Inventory updatedInventory, Nekoyume.Model.Item.Inventory inventoryModel)
        {
            foreach (var consumable in consumableSlots
                         .Where(consumableSlot =>
                             !consumableSlot.IsLock && !consumableSlot.IsEmpty)
                         .Select(slot => slot.Item))
            {
                if (updatedInventory.TryGetModel(consumable, out var inventoryItem))
                {
                    inventoryItem.Equipped.SetValueAndForceNotify(true);
                }
            }
        }

        private void OnClickStartButton()
        {
            AudioController.PlayClick();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);

            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    PracticeRaid();
                    break;
                case WorldBossStatus.Season:
                    var raiderState = WorldBossStates.GetRaiderState(_avatarState.address);
                    if (raiderState is null)
                    {
                        var cost = GetEntranceFee(_avatarState);
                        if (States.Instance.CrystalBalance.MajorUnit < cost)
                        {
                            Find<PaymentPopup>().ShowAttract(
                                CostType.Crystal,
                                cost,
                                L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                                L10nManager.Localize("UI_GO_GRINDING"),
                                () =>
                                {
                                    Find<Grind>().Show();
                                    Find<WorldBoss>().Close();
                                    Close();
                                });
                        }
                        else
                        {
                            Find<PaymentPopup>()
                                .ShowWithAddCost("UI_TOTAL_COST", "UI_BOSS_JOIN_THE_SEASON",
                                    CostType.Crystal, cost,
                                    CostType.WorldBossTicket, 1,
                                    () => StartCoroutine(CoRaid()));
                        }
                    }
                    else
                    {
                        if (_headerMenu.WorldBossTickets.RemainTicket > 0)
                        {
                            StartCoroutine(CoRaid());
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

        private void PracticeRaid()
        {
            var (_, _, foods, runes) = SaveCurrentEquipment();
            var tableSheets = Game.Game.instance.TableSheets;

            var simulator = new RaidSimulator(
                _bossId,
                new PracticeRandom(),
                _avatarState,
                foods,
                runes,
                tableSheets.GetRaidSimulatorSheets(),
                tableSheets.CostumeStatSheet
            );
            var log = simulator.Simulate();
            var digest = new ArenaPlayerDigest(_avatarState);
            var raidStage = Game.Game.instance.RaidStage;
            raidStage.Play(
                simulator.BossId,
                log,
                digest,
                simulator.DamageDealt,
                false,
                true,
                null,
                new List<FungibleAssetValue>());

            Find<WorldBoss>().Close();
            Close();
        }

        private IEnumerator CoRaid()
        {
            startButton.enabled = false;
            coverToBlockClick.SetActive(true);
            var ticketAnimation = ShowMoveTicketAnimation();
            var raiderState = WorldBossStates.GetRaiderState(_avatarState.address);
            if (raiderState is null)
            {
                var crystalAnimation = ShowMoveCrystalAnimation();
                yield return new WaitWhile(() => ticketAnimation.IsPlaying || crystalAnimation.IsPlaying);
            }
            else
            {
                yield return new WaitWhile(() => ticketAnimation.IsPlaying);
            }

            Raid(false);
        }

        private ItemMoveAnimation ShowMoveTicketAnimation()
        {
            var tickets = _headerMenu.WorldBossTickets;
            return ItemMoveAnimation.Show(tickets.IconImage.sprite,
                tickets.transform.position,
                ticketImage.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
        }

        private ItemMoveAnimation ShowMoveCrystalAnimation()
        {
            var crystal = _headerMenu.Crystal;
            return ItemMoveAnimation.Show(crystal.IconImage.sprite,
                crystal.transform.position,
                crystalImage.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
        }

        private void Raid(bool payNcg)
        {
            var (equipments, costumes, foods, runes) = SaveCurrentEquipment();
            ActionManager.Instance.Raid(costumes, equipments, foods, runes, payNcg);
            Find<LoadingScreen>().Show();
            Find<WorldBoss>().Close();
            Close();
        }

        private (List<Guid> equipments, List<Guid> costumes, List<Guid> foods, List<int> runes) SaveCurrentEquipment()
        {
            var equipments = Player.Equipments.Select(c => c.ItemId).ToList();
            var costumes = Player.Costumes.Select(c => c.ItemId).ToList();
            var foods = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable)slot.Item)
                .Select(c => c.ItemId).ToList();
            var runes = Player.Runes;

            var equipment = new List<Guid>();
            equipment.AddRange(equipments);
            equipment.AddRange(costumes);
            equipment.AddRange(foods);
            SaveEquipment(equipment);
            return (equipments, costumes, foods, runes);
        }

        private void ShowTicketPurchasePopup(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
            {
                return;
            }

            var raiderState = WorldBossStates.GetRaiderState(_avatarState.address);
            var cur = States.Instance.GoldBalanceState.Gold.Currency;
            var cost = WorldBossHelper.CalculateTicketPrice(row, raiderState, cur);
            var balance = States.Instance.GoldBalanceState;
            Find<TicketPurchasePopup>().Show(
                CostType.WorldBossTicket,
                CostType.NCG,
                balance.Gold,
                cost,
                raiderState.PurchaseCount,
                row.MaxPurchaseCount,
                () =>
                {
                    coverToBlockClick.SetActive(true);
                    Raid(true);
                });
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStat(_avatarState);
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
            Player.Set(avatarState);
            var foodIds = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item).Select(food => food.Id);
            var canBattle = Util.CanBattle(Player, foodIds);
            startButton.gameObject.SetActive(canBattle);
            blockStartingTextObject.SetActive(!canBattle);
        }

        public List<Guid> LoadEquipment()
        {
            var key = $"{RAID_EQUIPMENT_KEY}_{_avatarState.address}";
            if (!PlayerPrefs.HasKey(key))
            {
                return new List<Guid>();
            }

            var json = PlayerPrefs.GetString(key);
            var data =  JsonUtility.FromJson<EquipmentData>(json);
            return data.Guids.Select(Guid.Parse).ToList();
        }

        private void SaveEquipment(IEnumerable<Guid> guids)
        {
            var e = new EquipmentData(guids.Select(x=> x.ToString()).ToArray());
            var json = JsonUtility.ToJson(e);
            var key = $"{RAID_EQUIPMENT_KEY}_{_avatarState.address}";
            PlayerPrefs.SetString(key, json);
        }

        [Serializable]
        public class EquipmentData
        {
            public string[] Guids;

            public EquipmentData(string[] guids)
            {
                Guids = guids;
            }
        }
    }
}
