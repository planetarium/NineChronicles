using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Manager;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
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

        private Stage _stage;
        private Game.Character.Player _player;
        private EquipmentSlot _weaponSlot;

        private int _worldId;
        private int _stageId;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly ReactiveProperty<bool> _buttonEnabled = new ReactiveProperty<bool>();

        private CharacterStats _tempStats;

        #region override

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
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
                        return;

                    Equip(itemView.Model);
                })
                .AddTo(gameObject);
            inventory.OnResetItems.Subscribe(_ =>
            {
                foreach (var inventoryItem in inventory.SharedModel.Equipments)
                {
                    switch (inventoryItem.ItemBase.Value.Data.ItemType)
                    {
                        case ItemType.Consumable:
                        case ItemType.Equipment:
                            inventoryItem.EquippedEnabled.Value =
                                TryToFindSlotAlreadyEquip((ItemUsable) inventoryItem.ItemBase.Value, out var _);
                            break;
                        case ItemType.Material:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }).AddTo(gameObject);

            requiredPointText.text = GameConfig.HackAndSlashCostAP.ToString();

            questButton.OnClickAsObservable().Subscribe(_ => QuestClick(false)).AddTo(gameObject);
            Game.Event.OnRoomEnter.AddListener(() => Close());
        }

        public override void Show()
        {
            base.Show();

            inventory.SharedModel.State.Value = ItemType.Equipment;

            consumableTitleText.text = LocalizationManager.Localize("UI_EQUIP_CONSUMABLES");
            equipmentTitleText.text = LocalizationManager.Localize("UI_EQUIP_EQUIPMENTS");

            _stage = Game.Game.instance.Stage;
            _stage.LoadBackground("dungeon");
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (_player is null)
                throw new NotFoundComponentException<Game.Character.Player>();

            // stop run immediately.
            _player.UpdateEquipments(_player.Model.armor, _player.Model.weapon);
            _player.UpdateCustomize();
            _player.gameObject.SetActive(false);
            _player.gameObject.SetActive(true);
            _player.SpineController.Appear();

            equipmentSlots.SetPlayer(_player.Model);
            foreach (var equipment in _player.Equipments)
            {
                equipmentSlots.TryToEquip(equipment, ShowTooltip, Unequip);
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

            var worldMap = Find<WorldMap>();
            _worldId = worldMap.SelectedWorldId;
            _stageId = worldMap.SelectedStageId;

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
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
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
                inventory.Tooltip.Show(slot.RectTransform, item, tooltip => inventory.SharedModel.DeselectItemView());
            }
        }

        #endregion

        #region Subscribe

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
            Find<WorldMap>().Show(_worldId, _stageId, false);
            gameObject.SetActive(false);
        }

        private void SubscribeReadyToQuest(bool ready)
        {
            questButton.interactable = ready;
            requiredPointText.color = ready ? Color.white : Color.red;
        }

        private void SubscribeActionPoint(int point)
        {
            _buttonEnabled.Value = point >= GameConfig.HackAndSlashCostAP;
        }

        #endregion

        public void QuestClick(bool repeat)
        {
            Quest(repeat);
            AudioController.PlayClick();
            AnalyticsManager.Instance.BattleEntrance(repeat);
        }

        public void ToggleWorldMap()
        {
            if (isActiveAndEnabled)
            {
                var worldMap = Find<WorldMap>();
                _worldId = worldMap.SelectedWorldId;
                _stageId = worldMap.SelectedStageId;
                Find<BottomMenu>().Show(
                    UINavigator.NavigationType.Back,
                    SubscribeBackButtonClick,
                    true,
                    BottomMenu.ToggleableType.Mail,
                    BottomMenu.ToggleableType.Quest,
                    BottomMenu.ToggleableType.Chat,
                    BottomMenu.ToggleableType.IllustratedBook,
                    BottomMenu.ToggleableType.Inventory);
            }
            else
            {
                Show();
            }
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
                if (inventory.SharedModel.TryGetEquipment(slot.Item, out var inventoryItemToUnequip) ||
                    inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out inventoryItemToUnequip))
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
                    item.GlowEnabled.Value = item.ItemBase.Value.Data.ItemSubType == slot.ItemSubType;
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

            foreach (var consumableSlot in consumableSlots)
            {
                if (!item.Equals(consumableSlot.Item))
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

            slot = consumableSlots.FirstOrDefault(s => s.IsEmpty)
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
                .Select(x => x.Item as Equipment)
                .Where(x => !(x is null))
                .ToList();
            var consumables = consumableSlots
                .Select(x => x.Item as Consumable)
                .Where(x => !(x is null))
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

            questButton.interactable = false;
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var equipments = new List<Equipment>();
            foreach (var slot in equipmentSlots)
            {
                if (!slot.IsLock &&
                    !slot.IsEmpty)
                {
                    equipments.Add((Equipment) slot.Item);
                }
            }

            var consumables = new List<Consumable>();
            foreach (var slot in consumableSlots)
            {
                if (!slot.IsLock &&
                    !slot.IsEmpty)
                {
                    consumables.Add((Consumable) slot.Item);
                }
            }

            _stage.isExitReserved = false;
            _stage.repeatStage = repeat;
            ActionRenderHandler.Instance.Pending = true;
            Game.Game.instance.ActionManager.HackAndSlash(equipments, consumables, _worldId, _stageId)
                .Subscribe(_ => { }, e => Find<ActionFailPopup>().Show("Action timeout during HackAndSlash."))
                .AddTo(this);
        }

        public void GoToStage(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            Game.Event.OnStageStart.Invoke(eval.Action.Result);
            Find<LoadingScreen>().Close();
            Close();
        }
    }
}
