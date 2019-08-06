using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Manager;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public InventoryAndItemInfo inventoryAndItemInfo;

        public Text consumableTitleText;
        public EquipSlot[] consumableSlots;
        public Text equipmentTitleText;
        public EquipmentSlots equipmentSlots;
        public GameObject questBtn;
        public Text questBtnText;
        public Text questContinuousBtnText;
        public GameObject equipSlotGlow;
        public Text labelStage;

        private Stage _stage;
        private Player _player;
        private EquipSlot _weaponSlot;

        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private Model.QuestPreparation Model { get; set; }

        #region override

        public override void Show()
        {
            base.Show();

            consumableTitleText.text = LocalizationManager.Localize("UI_EQUIP_CONSUMABLES");
            equipmentTitleText.text = LocalizationManager.Localize("UI_EQUIP_EQUIPMENTS");
            questBtnText.text = LocalizationManager.Localize("UI_BATTLE");
            questContinuousBtnText.text = LocalizationManager.Localize("UI_BATTLE_CONTINUOUS");
            
            _stage = Game.Game.instance.stage;
            _stage.LoadBackground("dungeon");
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _weaponSlot = equipmentSlots.First(es => es.type == ItemBase.ItemType.Weapon);

            SetData(new Model.QuestPreparation(States.Instance.currentAvatarState.Value.inventory));

            // stop run immediately.
            _player.gameObject.SetActive(false);
            _player.gameObject.SetActive(true);
            _player.DoFade(1f, 0.3f);

            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.cls.ToEnumItemType();
                if (equipmentSlots.TryGet(type, out var es))
                    es.Set(equipment);
            }

            questBtn.SetActive(true);

            var worldMap = Find<WorldMap>();
            worldMap.SelectedStage = States.Instance.currentAvatarState.Value.worldStage;
            OnChangeStage();
        }

        public override void Close()
        {
            Clear();

            foreach (var slot in consumableSlots)
            {
                slot.Unequip();
            }

            equipmentSlots.Clear();

            base.Close();
        }

        #endregion

        public void QuestClick(bool repeat)
        {
            Quest(repeat);
            AudioController.PlayClick();
            AnalyticsManager.Instance.BattleEntrance(repeat);
        }

        public void Unequip(GameObject sender)
        {
            var slot = sender.GetComponent<EquipSlot>();
            if (slot.item == null)
            {
                equipSlotGlow.SetActive(false);
                foreach (var item in Model.inventory.Value.equipments)
                {
                    item.glowed.Value = item.item.Value.Data.cls.ToEnumItemType() == slot.type;
                }

                return;
            }

            var slotItem = slot.item;

            slot.Unequip();
            if (slot.type == ItemBase.ItemType.Armor)
            {
                var armor = (Armor) slot.item;
                var weapon = (Weapon) _weaponSlot.item;
                _player.UpdateSet(armor, weapon);
            }
            else if (slot.type == ItemBase.ItemType.Weapon)
            {
                _player.UpdateWeapon((Weapon) slot.item);
            }


            AudioController.instance.PlaySfx(slot.type == ItemBase.ItemType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);

            if (inventoryAndItemInfo.inventory.Model.TryGetEquipment(slotItem, out var inventoryItem))
            {
                inventoryItem.equipped.Value = false;
            }
        }

        public void SelectItem(Toggle item)
        {
            if (item.isOn)
            {
                var label = item.GetComponentInChildren<Text>();
                label.color = new Color(0.1960784f, 1, 0.1960784f, 1);
            }
        }

        public void BackClick()
        {
            _stage.LoadBackground("room");
            _player = _stage.GetPlayer(_stage.roomPosition);
            _player.UpdateSet(_player.model.armor);
            Find<Menu>().ShowRoom();
            Close();
            AudioController.PlayClick();
        }

        private void SetData(Model.QuestPreparation model)
        {
            _disposablesForSetData.DisposeAllAndClear();
            Model = model;
            Model.inventory.Value.selectedItemView.Subscribe(SubscribeInventorySelectedItem)
                .AddTo(_disposablesForSetData);
            Model.itemInfo.Value.item.Subscribe(OnItemInfoItem).AddTo(_disposablesForSetData);
            Model.itemInfo.Value.onClick.Subscribe(OnClickEquip).AddTo(_disposablesForSetData);

            inventoryAndItemInfo.SetData(Model.inventory.Value, Model.itemInfo.Value);
        }

        private void Clear()
        {
            inventoryAndItemInfo.Clear();
            Model = null;
            _disposablesForSetData.DisposeAllAndClear();
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }

            if (inventoryAndItemInfo.inventory.Tooltip.Model.target.Value == view.RectTransform)
            {
                inventoryAndItemInfo.inventory.Tooltip.Close();

                return;
            }

            inventoryAndItemInfo.inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model.dimmed.Value,
                LocalizationManager.Localize("UI_EQUIP"),
                tooltip =>
                {
                    OnClickEquip(tooltip.itemInformation.Model.item.Value);
                    inventoryAndItemInfo.inventory.Tooltip.Close();
                },
                tooltip =>
                {
                    equipSlotGlow.SetActive(false);
                });
        }

        private void OnItemInfoItem(InventoryItem data)
        {
            AudioController.PlaySelect();

            // Fix me. 이미 장착한 아이템일 경우 장착 버튼 비활성화 필요.
            // 현재는 왼쪽 부분인 인벤토리와 아이템 정보 부분만 뷰모델을 적용했는데, 오른쪽 까지 뷰모델이 확장되면 가능.
            if (ReferenceEquals(data, null) ||
                data.dimmed.Value)
            {
                SetGlowEquipSlot(false);
            }
            else
            {
                SetGlowEquipSlot(data.item.Value is ItemUsable);
            }
        }

        private void OnClickEquip(CountableItem countableItem)
        {
            var type = countableItem.item.Value.Data.cls.ToEnumItemType();
            var slot = FindSelectedItemSlot(type);
            if (slot != null)
            {
                if (inventoryAndItemInfo.inventory.Model.TryGetEquipment(slot.item, out var inventoryItem))
                {
                    inventoryItem.equipped.Value = false;
                }
                
                slot.Set(countableItem.item.Value as ItemUsable);
                SetGlowEquipSlot(false);
            }

            AudioController.instance.PlaySfx(type == ItemBase.ItemType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);

            if (type == ItemBase.ItemType.Armor)
            {
                var armor = (Armor) countableItem.item.Value;
                var weapon = (Weapon) _weaponSlot.item;
                _player.UpdateSet(armor, weapon);
            }
            else if (type == ItemBase.ItemType.Weapon)
            {
                _player.UpdateWeapon((Weapon) countableItem.item.Value);
            }

            if(countableItem is InventoryItem item)
            {
                item.equipped.Value = true;
            }
        }

        private void Quest(bool repeat)
        {
            Find<LoadingScreen>().Show();

            questBtn.SetActive(false);
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

            var foods = new List<Food>();
            foreach (var slot in consumableSlots)
            {
                if (slot.item?.Data != null)
                {
                    foods.Add((Food) slot.item);
                }
            }

            var selectedStage = Find<WorldMap>().SelectedStage;
            ActionManager.instance.HackAndSlash(equipments, foods, selectedStage)
                .Subscribe(eval =>
                {
                    Game.Event.OnStageStart.Invoke();
                    Find<LoadingScreen>().Close();
                    _stage.repeatStage = repeat;
                    Close();
                }).AddTo(this);
        }

        public EquipSlot FindSelectedItemSlot(ItemBase.ItemType type)
        {
            if (type == ItemBase.ItemType.Food)
            {
                var count = consumableSlots
                    .Select(s => s.item)
                    .OfType<Food>()
                    .Count(f => f.Data.id == Model.itemInfo.Value.item.Value.item.Value.Data.id);
                if (count >= Model.itemInfo.Value.item.Value.count.Value)
                {
                    return null;
                }

                var slot = consumableSlots.FirstOrDefault(s => s.item?.Data == null);
                if (slot == null)
                {
                    slot = consumableSlots[0];
                }

                return slot;
            }

            equipmentSlots.TryGet(type, out var es);
            return es;
        }

        private void SetGlowEquipSlot(bool isActive)
        {
            equipSlotGlow.SetActive(isActive);

            if (!isActive)
                return;

            var type = Model.itemInfo.Value.item.Value.item.Value.Data.cls.ToEnumItemType();
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

        public void OpenWorldMap()
        {
            Find<WorldMap>().Show();
        }

        public void OnChangeStage()
        {
            var worldMap = Find<WorldMap>();
            labelStage.text = $"Stage {worldMap.SelectedStage}";
        }
    }
}
