using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Manager;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public Module.Inventory inventory;

        public TextMeshProUGUI consumableTitleText;

        // todo: `EquipmentSlot`을 사용하지 않든가, 이름을 바꿔야 하겠다. 또한 `EquipmentSlots`와 같이 `ConsumableSlots`를 만들어도 좋겠다.
        public EquipmentSlot[] consumableSlots;
        public DetailedStatView[] statusRows;
        public TextMeshProUGUI equipmentTitleText;
        public EquipmentSlots equipmentSlots;

        public Button questButton;
        public GameObject equipSlotGlow;
        public TextMeshProUGUI requiredPointText;
        public ParticleSystem[] particles;
        public TMP_InputField levelField;
        public Button simulateButton;

        private Stage _stage;
        private Game.Character.Player _player;
        private EquipmentSlot _weaponSlot;

        private int _worldId;
        private readonly IntReactiveProperty _stageId = new IntReactiveProperty();
        private int _requiredCost;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly ReactiveProperty<bool> _buttonEnabled = new ReactiveProperty<bool>();

        private CharacterStats _tempStats;
        private bool _reset = true;
        private bool _questButtonClicked = false;

        [Header("ItemMoveAnimation")]
        [SerializeField]
        private Image actionPointImage = null;

        [SerializeField]
        private Transform buttonStarImageTransform = null;

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        protected override bool CanClose =>
            base.CanClose &&
            !_questButtonClicked;

        #region override

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
            simulateButton.gameObject.SetActive(GameConfig.IsEditor);
            levelField.gameObject.SetActive(GameConfig.IsEditor);
        }

        public override void Initialize()
        {
            base.Initialize();

            inventory.SharedModel.DimmedFunc.Value =
                inventoryItem => inventoryItem.ItemBase.Value.Data.ItemType == ItemType.Material;
            inventory.SharedModel.SelectedItemView.Subscribe(SubscribeInventorySelectedItem)
                .AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView.Subscribe(itemView =>
                {
                    if (itemView.Model.Dimmed.Value)
                    {
                        return;
                    }

                    Equip(itemView.Model);
                })
                .AddTo(gameObject);
            inventory.OnResetItems.Subscribe(SubscribeInventoryResetItems).AddTo(gameObject);

            _stageId.Subscribe(SubscribeStage).AddTo(gameObject);

            questButton.OnClickAsObservable().Subscribe(_ => QuestClick(false)).AddTo(gameObject);
            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            inventory.SharedModel.State.Value = ItemType.Equipment;

            consumableTitleText.text = LocalizationManager.Localize("UI_EQUIP_CONSUMABLES");
            equipmentTitleText.text = LocalizationManager.Localize("UI_EQUIP_EQUIPMENTS");

            _stage = Game.Game.instance.Stage;
            _stage.LoadBackground("dungeon");
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Game.Character.Player>();
            }

            if (_reset)
            {
                _reset = false;

                _player.UpdateEquipments(_player.Model.armor, _player.Model.weapon);
                _player.UpdateCustomize();
                // stop run immediately.
                _player.gameObject.SetActive(false);
                _player.gameObject.SetActive(true);
                _player.SpineController.Appear();
                equipmentSlots.SetPlayer(_player.Model, ShowTooltip, Unequip);
                foreach (var consumableSlot in consumableSlots)
                {
                    consumableSlot.Set(_player.Level);
                }

                var tuples = _player.Model.Stats.GetBaseAndAdditionalStats();

                var idx = 0;
                foreach (var (statType, value, additionalValue) in tuples)
                {
                    var info = statusRows[idx];
                    info.Show(statType, value, additionalValue);
                    ++idx;
                }

                _weaponSlot = equipmentSlots.First(es => es.ItemSubType == ItemSubType.Weapon);
            }

            // 인벤토리 아이템의 장착 여부를 `equipmentSlots`의 상태를 바탕으로 설정하기 때문에 `equipmentSlots.SetPlayer()`를 호출한 이후에 인벤토리 아이템의 장착 상태를 재설정한다.
            // 또한 인벤토리는 기본적으로 `OnEnable()` 단계에서 `OnResetItems` 이벤트를 일으키기 때문에 `equipmentSlots.SetPlayer()`와 호출 순서 커플링이 생기게 된다.
            // 따라서 강제로 상태를 설정한다.
            SubscribeInventoryResetItems(inventory);

            var worldMap = Find<WorldMap>();
            _worldId = worldMap.SelectedWorldId;
            _stageId.Value = worldMap.SelectedStageId;

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook);
            _buttonEnabled.Subscribe(SubscribeReadyToQuest).AddTo(_disposables);
            ReactiveAvatarState.ActionPoint.Subscribe(SubscribeActionPoint).AddTo(_disposables);
            _tempStats = _player.Model.Stats.Clone() as CharacterStats;
            questButton.gameObject.SetActive(true);
            _questButtonClicked = false;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _reset = true;
            Find<BottomMenu>().Close(true);

            foreach (var slot in consumableSlots)
            {
                slot.Clear();
            }

            equipmentSlots.Clear();
            base.Close(ignoreCloseAnimation);
            _disposables.DisposeAllAndClear();
        }

        #endregion

        #region Tooltip

        private void ShowTooltip(InventoryItemView view)
        {
            if (view is null ||
                view.RectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();

                return;
            }

            inventory.Tooltip.Show(view.RectTransform, view.Model,
                value => !view.Model.Dimmed.Value,
                view.Model.EquippedEnabled.Value
                    ? LocalizationManager.Localize("UI_UNEQUIP")
                    : LocalizationManager.Localize("UI_EQUIP"),
                tooltip => Equip(tooltip.itemInformation.Model.item.Value),
                tooltip =>
                {
                    equipSlotGlow.SetActive(false);
                    inventory.SharedModel.DeselectItemView();
                });
        }

        private void ShowTooltip(EquipmentSlot slot)
        {
            if (slot is null ||
                slot.RectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(slot.Item, out var item) ||
                inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out item))
            {
                inventory.Tooltip.Show(slot.RectTransform, item,
                    tooltip => inventory.SharedModel.DeselectItemView());
            }
        }

        #endregion

        #region Subscribe

        private void SubscribeInventoryResetItems(Module.Inventory value)
        {
            if (_reset)
            {
                return;
            }

            foreach (var inventoryItem in value.SharedModel.Equipments)
            {
                switch (inventoryItem.ItemBase.Value.Data.ItemType)
                {
                    case ItemType.Consumable:
                    case ItemType.Equipment:
                        inventoryItem.EquippedEnabled.Value =
                            TryToFindSlotAlreadyEquip((ItemUsable) inventoryItem.ItemBase.Value,
                                out var _);
                        break;
                    case ItemType.Material:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            // Fix me. 이미 장착한 아이템일 경우 장착 버튼 비활성화 필요.
            // 현재는 왼쪽 부분인 인벤토리와 아이템 정보 부분만 뷰모델을 적용했는데, 오른쪽 까지 뷰모델이 확장되면 가능.
            if (view is null ||
                view.Model is null ||
                view.Model.Dimmed.Value ||
                !(view.Model.ItemBase.Value is ItemUsable))
            {
                HideGlowEquipSlot();
            }
            else
            {
                UpdateGlowEquipSlot((ItemUsable) view.Model.ItemBase.Value);
            }

            ShowTooltip(view);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            Find<WorldMap>().Show(_worldId, _stageId.Value, false);
            gameObject.SetActive(false);
        }

        private void SubscribeReadyToQuest(bool ready)
        {
            questButton.interactable = ready;
            requiredPointText.color = ready ? Color.white : Color.red;
            foreach (var particle in particles)
            {
                if (ready)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }
        }

        private void SubscribeActionPoint(int point)
        {
            _buttonEnabled.Value = point >= _requiredCost;
        }

        private void SubscribeStage(int stageId)
        {
            var stage =
                Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(
                    i => i.Id == stageId);
            if (stage is null)
                return;
            _requiredCost = stage.CostAP;
            requiredPointText.text = _requiredCost.ToString();
        }

        #endregion

        public void QuestClick(bool repeat)
        {
            questButton.interactable = false;
            _questButtonClicked = true;
            StartCoroutine(CoQuestClick(repeat));
        }

        private IEnumerator CoQuestClick(bool repeat)
        {
            var animation = ItemMoveAnimation.Show(actionPointImage.sprite,
                actionPointImage.transform.position,
                buttonStarImageTransform.position,
                moveToLeft,
                animationTime,
                middleXGap);
            LocalStateModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                -_requiredCost);
            yield return new WaitWhile(() => animation.IsPlaying);
            Quest(repeat);
            AudioController.PlayClick();
            AnalyticsManager.Instance.BattleEntrance(repeat);
        }

        #region slot

        private void Equip(CountableItem countableItem)
        {
            if (!(countableItem is InventoryItem inventoryItem))
                return;

            var itemUsable = inventoryItem.ItemBase.Value as ItemUsable;
            // 이미 장착중인 아이템이라면 해제한다.
            if (TryToFindSlotAlreadyEquip(itemUsable, out var slot))
            {
                Unequip(slot);
                return;
            }

            // 아이템을 장착할 슬롯을 찾는다.
            if (!TryToFindSlotToEquip(itemUsable, out slot))
                return;

            // 이미 슬롯에 아이템이 있다면 해제한다.
            if (!slot.IsEmpty)
            {
                if (inventory.SharedModel.TryGetEquipment(slot.Item,
                        out var inventoryItemToUnequip) ||
                    inventory.SharedModel.TryGetConsumable(slot.Item as Consumable,
                        out inventoryItemToUnequip))
                {
                    inventoryItemToUnequip.EquippedEnabled.Value = false;
                }
            }

            inventoryItem.EquippedEnabled.Value = true;
            slot.Set(itemUsable, ShowTooltip, Unequip);
            HideGlowEquipSlot();
            PostEquipOrUnequip(slot);
        }

        private void Unequip(EquipmentSlot slot)
        {
            if (slot.IsEmpty)
            {
                equipSlotGlow.SetActive(false);
                foreach (var item in inventory.SharedModel.Equipments)
                {
                    item.GlowEnabled.Value =
                        item.ItemBase.Value.Data.ItemSubType == slot.ItemSubType;
                }

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(slot.Item, out var inventoryItem) ||
                inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out inventoryItem))
            {
                inventoryItem.EquippedEnabled.Value = false;
            }

            slot.Clear();
            PostEquipOrUnequip(slot);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot)
        {
            UpdateStats();
            inventory.Tooltip.Close();

            if (slot.ItemSubType == ItemSubType.Armor)
            {
                var armor = (Armor) slot.Item;
                var weapon = (Weapon) _weaponSlot.Item;
                _player.UpdateEquipments(armor, weapon);
                _player.UpdateCustomize();
            }
            else if (slot.ItemSubType == ItemSubType.Weapon)
            {
                _player.UpdateWeapon((Weapon) slot.Item);
            }

            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
        }

        private bool TryToFindSlotAlreadyEquip(ItemUsable item, out EquipmentSlot slot)
        {
            if (item.Data.ItemType == ItemType.Equipment)
                return equipmentSlots.TryGetAlreadyEquip((Equipment) item, out slot);

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
        }

        private bool TryToFindSlotToEquip(ItemUsable item, out EquipmentSlot slot)
        {
            if (item.Data.ItemType == ItemType.Equipment)
                return equipmentSlots.TryGetToEquip((Equipment) item, out slot);

            slot = consumableSlots.FirstOrDefault(s => !s.IsLock && s.IsEmpty)
                   ?? consumableSlots[0];
            return true;
        }

        private void UpdateGlowEquipSlot(ItemUsable itemUsable)
        {
            var itemType = itemUsable.Data.ItemType;
            EquipmentSlot equipmentSlot;
            switch (itemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    TryToFindSlotToEquip(itemUsable, out equipmentSlot);
                    break;
                case ItemType.Material:
                    HideGlowEquipSlot();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (equipmentSlot && equipmentSlot.transform.parent)
            {
                equipSlotGlow.transform.SetParent(equipmentSlot.transform);
                equipSlotGlow.transform.localPosition = Vector3.zero;
            }
            else
            {
                HideGlowEquipSlot();
            }
        }

        private void HideGlowEquipSlot()
        {
            equipSlotGlow.SetActive(false);
        }

        #endregion

        private void UpdateStats()
        {
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Consumable)
                .Where(item => !(item is null))
                .ToList();

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                consumables,
                Game.Game.instance.TableSheets
            );
            using (var enumerator = stats.GetBaseAndAdditionalStats().GetEnumerator())
            {
                foreach (var statView in statusRows)
                {
                    if (!enumerator.MoveNext())
                        break;

                    var (statType, baseValue, additionalValue) = enumerator.Current;
                    statView.Show(statType, baseValue, additionalValue);
                }
            }
        }

        private void Quest(bool repeat)
        {
            Find<LoadingScreen>().Show();

            questButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment) slot.Item)
                .ToList();

            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item)
                .ToList();

            _stage.isExitReserved = false;
            _stage.repeatStage = repeat;
            ActionRenderHandler.Instance.Pending = true;
            Game.Game.instance.ActionManager
                .HackAndSlash(equipments, consumables, _worldId, _stageId.Value)
                .Subscribe(
                    _ =>
                    {
                        LocalStateModifier.ModifyAvatarActionPoint(
                            States.Instance.CurrentAvatarState.address, _requiredCost);
                    }, e => Find<ActionFailPopup>().Show("Action timeout during HackAndSlash."))
                .AddTo(this);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
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

            questButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var stageId = _stageId.Value;
            if (!Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stageId,
                out var worldRow))
                throw new KeyNotFoundException(
                    $"WorldSheet.TryGetByStageId() {nameof(stageId)}({stageId})");

            var avatarState = new AvatarState(States.Instance.CurrentAvatarState) {level = level};
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item)
                .ToList();
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment) slot.Item)
                .ToList();
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

                ((Equipment) outNonFungibleItem).Equip();
            }

            var simulator = new StageSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                consumables,
                worldRow.Id,
                stageId,
                Game.Game.instance.TableSheets
            );
            simulator.Simulate();
            GoToStage(simulator.Log);
        }
    }
}
