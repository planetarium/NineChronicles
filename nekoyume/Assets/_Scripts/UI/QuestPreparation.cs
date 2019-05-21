using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public Module.InventoryAndItemInfo inventoryAndItemInfo;
        
        public EquipSlot[] consumableSlots;
        public GameObject[] equipmentSlots;
        public Dropdown dropdown;
        public GameObject btnQuest;
        public GameObject equipSlotGlow;


        private Stage _stage;
        private Player _player;
        private int[] _stages;

        private Model.QuestPreparation _data;
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region override

        public override void Show()
        {
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            if (ReferenceEquals(_stage, null))
            {
                throw new NotFoundComponentException<Stage>();
            }
            _stage.LoadBackground("dungeon");
            
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }
            
            SetData(new Model.QuestPreparation(ActionManager.instance.Avatar.Items));
            
            // stop run immediately.
            _player.gameObject.SetActive(false);
            _player.gameObject.SetActive(true);
            
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.cls.ToEnumItemType();
                foreach (var slot in equipmentSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
            }

            btnQuest.SetActive(true);

            dropdown.ClearOptions();
            _stages = Enumerable.Range(1, ActionManager.instance.Avatar.WorldStage).ToArray();
            var list = _stages.Select(i => $"Stage {i}").ToList();
            dropdown.AddOptions(list);
            dropdown.value = _stages.Length - 1;
            base.Show();
        }

        public override void Close()
        {
            Clear();
            
            foreach (var slot in consumableSlots)
            {
                slot.Unequip();
            }

            foreach (var slot in equipmentSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                es.Unequip();
            }
            
            base.Close();
        }

        #endregion
        
        public void QuestClick(bool repeat)
        {
            Quest(repeat);
            AudioController.PlayClick();
            AnalyticsManager.instance.BattleEntrance(repeat);
        }

        public void Unequip(GameObject sender)
        {
            var slot = sender.GetComponent<EquipSlot>();
            if (slot.item == null)
            {
                equipSlotGlow.SetActive(false);
                foreach (var item in _data.inventory.Value.items)
                {
                    item.glowed.Value = _data.inventory.Value.glowedFunc.Value(item, slot.type);
                }
                return;
            }
            slot.Unequip();
            if (slot.type == ItemBase.ItemType.Set)
            {
                _player.UpdateSet((SetItem) slot.item);
            }

            AudioController.instance.PlaySfx(slot.type == ItemBase.ItemType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
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
            _player.UpdateSet(_player.model.set);
            Find<Menu>().Show();
            Find<Status>()?.Show();
            Close();
            AudioController.PlayClick();
        }

        private void SetData(Model.QuestPreparation value)
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = value;
            _data.itemInfo.Value.item.Subscribe(OnItemInfoItem).AddTo(_disposablesForSetData);
            _data.itemInfo.Value.onClick.Subscribe(OnClickEquip).AddTo(_disposablesForSetData);
            
            inventoryAndItemInfo.SetData(_data.inventory.Value, _data.itemInfo.Value);
        }

        private void Clear()
        {
            inventoryAndItemInfo.Clear();
            _data = null;
            _disposablesForSetData.DisposeAllAndClear();
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

        private void OnClickEquip(ItemInfo itemInfo)
        {
            var slot = FindSelectedItemSlot();
            if (slot != null)
            {
                slot.Set(itemInfo.item.Value.item.Value as ItemUsable);
                SetGlowEquipSlot(false);
            }
            
            var type = itemInfo.item.Value.item.Value.Data.cls.ToEnumItemType();
            AudioController.instance.PlaySfx(type == ItemBase.ItemType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);

            if (type == ItemBase.ItemType.Set)
            {
                _player.UpdateSet((SetItem)itemInfo.item.Value.item.Value);
            }
        }

        private void Quest(bool repeat)
        {
            Find<LoadingScreen>().Show();

            btnQuest.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var equipments = new List<Equipment>();
            foreach (var slot in equipmentSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.item?.Data != null)
                {
                    equipments.Add((Equipment)es.item);
                }
            }

            var foods = new List<Food>();
            foreach (var slot in consumableSlots)
            {
                if (slot.item?.Data != null)
                {
                    foods.Add((Food)slot.item);
                }
            }

            IObservable<ActionBase.ActionEvaluation<HackAndSlash>> observable =
                ActionManager.instance.HackAndSlash(equipments, foods, _stages[dropdown.value]);

            observable.ObserveOnMainThread().Subscribe(eval =>
            {
                var avatar = (AvatarState)eval.OutputStates.GetState(eval.InputContext.Signer);
                ActionManager.instance.battleLog = avatar.battleLog;
                
                Game.Event.OnStageStart.Invoke();
                Find<LoadingScreen>().Close();
                _stage.repeatStage = repeat;
                Close();
            }).AddTo(this);
        }

        private EquipSlot FindSelectedItemSlot()
        {
            var type = _data.itemInfo.Value.item.Value.item.Value.Data.cls.ToEnumItemType();
            if (type == ItemBase.ItemType.Food)
            {
                var count = consumableSlots
                    .Select(s => s.item)
                    .OfType<Food>()
                    .Count(f => f.Data.id == _data.itemInfo.Value.item.Value.item.Value.Data.id);
                if (count >= _data.itemInfo.Value.item.Value.count.Value)
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

            foreach (var slot in equipmentSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.type == type)
                {
                    return es;
                }
            }

            return null;
        }

        private void SetGlowEquipSlot(bool isActive)
        {
            equipSlotGlow.SetActive(isActive);

            if (!isActive)
                return;

            var slot = FindSelectedItemSlot();
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
