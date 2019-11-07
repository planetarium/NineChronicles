using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Manager;
using Nekoyume.Model;
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

        public Text consumableTitleText;
        public EquipSlot[] consumableSlots;
        public Text equipmentTitleText;
        public EquipmentSlots equipmentSlots;

        public Button questButton;
        public NormalButton questRepeatButton;
        public GameObject equipSlotGlow;
        public GameObject statusRowPrefab;
        public Transform statusRowParent;
        public TextMeshProUGUI requiredPointText;

        private Stage _stage;
        private Game.Character.Player _player;
        private EquipSlot _weaponSlot;

        private int _stageId;

        private readonly Dictionary<StatType, StatusInfo> _stats =
            new Dictionary<StatType, StatusInfo>(StatTypeComparer.Instance);

        private readonly Dictionary<StatType, int> _additionalStats = new Dictionary<StatType, int>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly ReactiveProperty<bool> _buttonEnabled = new ReactiveProperty<bool>();

        private CharacterStats _tempStats;


        #region override

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

                    OnClickEquip(itemView.Model);
                })
                .AddTo(gameObject);

            requiredPointText.text = GameConfig.HackAndSlashCostAP.ToString();
        }

        public override void Show()
        {
            base.Show();

            inventory.SharedModel.State.Value = ItemType.Equipment;

            consumableTitleText.text = LocalizationManager.Localize("UI_EQUIP_CONSUMABLES");
            equipmentTitleText.text = LocalizationManager.Localize("UI_EQUIP_EQUIPMENTS");

            _stage = Game.Game.instance.stage;
            _stage.LoadBackground("dungeon");
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Game.Character.Player>();
            }

            _weaponSlot = equipmentSlots.First(es => es.itemSubType == ItemSubType.Weapon);

            // stop run immediately.
            _player.gameObject.SetActive(false);
            _player.gameObject.SetActive(true);
            _player.DoFade(1f, 0.3f);

            foreach (var equipment in _player.Equipments)
            {
                if (!equipmentSlots.TryGet(equipment.Data.ItemSubType, out var es))
                    continue;

                es.Set(equipment);
                es.SetOnClickAction(ShowTooltip, Unequip);
            }

            var isStatInitialized = _stats.Count > 0;
            var tuples = _player.Model.Value.GetStatTuples();
            if (!isStatInitialized)
            {
                statusRowPrefab.SetActive(true);
                foreach (var (statType, value, additionalValue) in tuples)
                {
                    var go = Instantiate(statusRowPrefab, statusRowParent);
                    var info = go.GetComponent<StatusInfo>();
                    info.Set(statType, value, additionalValue);
                    _stats.Add(statType, info);
                    _additionalStats.Add(statType, additionalValue);
                }
                statusRowPrefab.SetActive(false);
            }
            else
            {
                foreach (var (statType, value, additionalValue) in tuples)
                {
                    _stats[statType].Set(statType, value, additionalValue);
                    _additionalStats[statType] = additionalValue;
                }
            }

            var worldMap = Find<WorldMap>();
            _stageId = worldMap.SelectedStageId;

            Find<BottomMenu>().Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick);
            _buttonEnabled.Subscribe(SubscribeReadyToQuest).AddTo(_disposables);
            ReactiveCurrentAvatarState.ActionPoint.Subscribe(SubscribeActionPoint).AddTo(_disposables);
            _tempStats = _player.Model.Value.Stats.Clone() as CharacterStats;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            
            Find<Inventory>().Close(ignoreCloseAnimation);
            Find<StatusDetail>().Close(ignoreCloseAnimation);
            Find<Quest>().Close(ignoreCloseAnimation);

            foreach (var slot in consumableSlots)
            {
                slot.Unequip();
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
                tooltip => OnClickEquip(tooltip.itemInformation.Model.item.Value),
                tooltip =>
                {
                    equipSlotGlow.SetActive(false);
                    inventory.SharedModel.DeselectItemView();
                });
        }

        private void ShowTooltip(EquipSlot slot)
        {
            if (slot is null ||
                slot.rectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(slot.item, out var item) ||
                inventory.SharedModel.TryGetConsumable(slot.item as Consumable, out item))
            {
                inventory.Tooltip.Show(slot.rectTransform, item, tooltip => inventory.SharedModel.DeselectItemView());
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
                view.Model.Dimmed.Value)
            {
                SetGlowEquipSlot(false);
            }
            else
            {
                SetGlowEquipSlot(view.Model.ItemBase.Value is ItemUsable);
            }

            ShowTooltip(view);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            Close();
            Find<WorldMap>().Show(_stageId);
        }

        private void SubscribeReadyToQuest(bool ready)
        {
            questButton.interactable = ready;
            questRepeatButton.button.interactable = ready;
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

        private void Unequip(EquipSlot slot)
        {
            if (slot.item is null)
            {
                equipSlotGlow.SetActive(false);
                foreach (var item in inventory.SharedModel.Equipments)
                {
                    item.GlowEnabled.Value = item.ItemBase.Value.Data.ItemSubType == slot.itemSubType;
                }

                return;
            }

            var slotItem = slot.item;

            slot.Unequip(); 
            if (slot.itemSubType == ItemSubType.Armor)
            {
                var armor = (Armor) slot.item;
                var weapon = (Weapon) _weaponSlot.item;
                _player.UpdateSet(armor, weapon);
            }
            else if (slot.itemSubType == ItemSubType.Weapon)
            {
                _player.UpdateWeapon((Weapon) slot.item);
            }

            AudioController.instance.PlaySfx(slot.itemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);

            if (inventory.SharedModel.TryGetEquipment(slotItem, out var inventoryItem) ||
                inventory.SharedModel.TryGetConsumable(slotItem as Consumable, out inventoryItem))
            {
                inventoryItem.EquippedEnabled.Value = false;
            }

            UpdateStats();

            inventory.Tooltip.Close();
        }

        private void OnClickEquip(CountableItem countableItem)
        {
            var item = countableItem as InventoryItem;
            var itemSubType = countableItem.ItemBase.Value.Data.ItemSubType;

            if (item != null && item.EquippedEnabled.Value)
            {
                var equipSlot = FindSlot(item.ItemBase.Value as ItemUsable, itemSubType);
                if (equipSlot) Unequip(equipSlot);
                return;
            }

            var slot = FindSelectedItemSlot(itemSubType);

            var equipable = countableItem.ItemBase.Value as ItemUsable;
            if (slot != null)
            {
                if (inventory.SharedModel.TryGetEquipment(slot.item, out var inventoryItem) ||
                    inventory.SharedModel.TryGetConsumable(slot.item as Consumable, out inventoryItem))
                {
                    inventoryItem.EquippedEnabled.Value = false;
                }

                slot.Set(equipable);
                slot.SetOnClickAction(ShowTooltip, Unequip);
                SetGlowEquipSlot(false);
            }

            AudioController.instance.PlaySfx(itemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);

            if (itemSubType == ItemSubType.Armor)
            {
                var armor = (Armor) equipable;
                var weapon = (Weapon) _weaponSlot.item;
                _player.UpdateSet(armor, weapon);
            }
            else if (itemSubType == ItemSubType.Weapon)
            {
                _player.UpdateWeapon((Weapon) countableItem.ItemBase.Value);
            }

            if (item != null)
            {
                item.EquippedEnabled.Value = true;
            }

            UpdateStats();
            
            inventory.Tooltip.Close();
        }

        private void UpdateStats()
        {
            var equipments = equipmentSlots.slots
                .Select(x => x.item as Equipment)
                .Where(x => !(x is null))
                .ToList();
            var consumables = consumableSlots
                .Select(x => x.item as Consumable)
                .Where(x => !(x is null))
                .ToList();

            var stats = _tempStats.SetAll(_tempStats.Level, equipments, consumables, null);
            var statMap = stats.GetAdditionalStats();
            foreach (var (type, value) in statMap)
            {
                _stats[type].SetAdditional(type, value);
            }
        }

        private void Quest(bool repeat)
        {
            Find<LoadingScreen>().Show();

            questButton.interactable = false;
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var equipments = new List<Equipment>();
            foreach (var es in equipmentSlots)
            {
                if (es.item?.Data != null)
                {
                    equipments.Add((Equipment) es.item);
                }
            }

            var consumables = new List<Consumable>();
            foreach (var slot in consumableSlots)
            {
                if (slot.item?.Data != null)
                {
                    consumables.Add((Consumable) slot.item);
                }
            }

            _stage.repeatStage = repeat;
            ActionManager.instance.HackAndSlash(equipments, consumables, _stageId)
                .Subscribe(_ => {}, e => Find<ActionFailPopup>().Show("Action timeout during HackAndSlash.")).AddTo(this);
        }

        public void GoToStage(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            Game.Event.OnStageStart.Invoke(eval.Action.Result);
            Find<LoadingScreen>().Close();
            Close();
        }

        public EquipSlot FindSelectedItemSlot(ItemSubType type)
        {
            if (type == ItemSubType.Food)
            {
                return consumableSlots.FirstOrDefault(s => s.item?.Data is null) ?? consumableSlots[0];
            }

            equipmentSlots.TryGet(type, out var es);
            return es;
        }

        private EquipSlot FindSlot(ItemUsable item, ItemSubType type)
        {
            if (type == ItemSubType.Food)
            {
                foreach (var slot in consumableSlots)
                {
                    if (item.Equals(slot.item))
                    {
                        return slot;
                    }
                }
            }

            return equipmentSlots.FindSlotWithItem(item);
        }

        private void SetGlowEquipSlot(bool isActive)
        {
            equipSlotGlow.SetActive(isActive);

            if (!isActive)
                return;

            var type = inventory.SharedModel.SelectedItemView.Value.Model.ItemBase.Value.Data.ItemSubType;
            var slot = FindSelectedItemSlot(type);
            if (slot && slot.transform.parent)
            {
                equipSlotGlow.transform.SetParent(slot.transform);
                equipSlotGlow.transform.localPosition = Vector3.zero;
            }
            else
            {
                equipSlotGlow.SetActive(false);
            }
        }
    }
}
