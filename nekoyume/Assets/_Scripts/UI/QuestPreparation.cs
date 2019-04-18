using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public Widget itemInfoWidget = null;
        public SelectedItem itemInfoSelectedItem = null;
        public int selectedItemCount;

        public Image buttonSellImage = null;
        public Text buttonSellText = null;

        public EquipSlot[] usableSlots;
        public GameObject[] equipSlots;
        public GameObject btnQuest;
        public GameObject inventory;
        public Dropdown dropdown;

        public Stage stage;

        private Player _player = null;
        private Inventory _inventory = null;
        private int[] _stages;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            stage = GameObject.Find("Stage").GetComponent<Stage>();

            _inventory = inventory.GetComponent<Inventory>();
        }

        private void OnEnable()
        {
            itemInfoSelectedItem.Clear();
            selectedItemCount = 0;
            SetActiveButtonEquip(false);

            Game.Event.OnSlotClick.AddListener(SlotClick);
        }

        private void OnDisable()
        {
            foreach (var slot in usableSlots)
            {
                slot.Unequip();
            }

            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                es.Unequip();
            }
            Game.Event.OnSlotClick.RemoveListener(SlotClick);
        }

        #endregion

        public void QuestClick(bool repeat)
        {
            StartCoroutine(CoQuest(repeat));
            AudioController.PlayClick();
        }

        private IEnumerator CoQuest(bool repeat)
        {
            var loadingScreen = Find<LoadingScreen>();
            if (!ReferenceEquals(loadingScreen, null))
            {
                loadingScreen.Show();
            }

            btnQuest.SetActive(false);
            _player.StartRun();
            var currentId = ActionManager.instance.battleLog?.id;
            var equipments = new List<Equipment>();
            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.item?.Data != null)
                {
                    equipments.Add((Equipment)es.item);
                }
            }

            var foods = new List<Food>();
            foreach (var slot in usableSlots)
            {
                if (slot.item?.Data != null)
                {
                    foods.Add((Food)slot.item);
                }
            }

            ActionManager.instance.HackAndSlash(equipments, foods, _stages[dropdown.value]);
            while (currentId == ActionManager.instance.battleLog?.id)
            {
                yield return null;
            }

            Game.Event.OnStageStart.Invoke();

            if (!ReferenceEquals(loadingScreen, null))
            {
                loadingScreen.Close();
            }

            stage.repeatStage = repeat;
        }

        public override void Show()
        {
            stage.LoadBackground("dungeon");
            _player = stage.GetPlayer(stage.QuestPreparationPosition);
            _inventory.Show();
            _inventory.SetItemTypesToDisable(ItemBase.ItemType.Material);
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.cls.ToEnumItemType();
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
            }
            itemInfoWidget.Show();

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
            stage.LoadBackground("room");
            _player = stage.GetPlayer(stage.RoomPosition);
            _inventory.Close();
            itemInfoWidget.Close();
            Find<Menu>().Show();
            Find<Status>()?.Show();
            base.Close();
            AudioController.PlayClick();
        }

        private void SlotClick(InventorySlot slot, bool toggled)
        {
            if (ReferenceEquals(slot, null) ||
                ReferenceEquals(slot.Item, null) ||
                !toggled)
            {
                itemInfoSelectedItem.Clear();
                SetActiveButtonEquip(false);
                return;
            }

            itemInfoSelectedItem.SetItem(slot.Item);
            itemInfoSelectedItem.SetIcon(slot.Icon.sprite);
            selectedItemCount = Convert.ToInt32(slot.LabelCount.text);
            SetActiveButtonEquip(slot.Item is ItemUsable);
            AudioController.PlaySelect();
        }

        public void EquipClick()
        {
            var type = itemInfoSelectedItem.item.Data.cls.ToEnumItemType();
            if (type == ItemBase.ItemType.Food)
            {
                var count = usableSlots
                    .Select(s => s.item)
                    .OfType<Food>()
                    .Count(f => f.Equals(itemInfoSelectedItem.item));
                if (count < selectedItemCount)
                {
                    var slot = usableSlots.FirstOrDefault(s => s.item?.Data == null);
                    if (slot == null)
                    {
                        slot = usableSlots[0];
                    }
                    slot.Equip(itemInfoSelectedItem);
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Equipment);
                }
            }
            else
            {
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Equip(itemInfoSelectedItem);
                        AudioController.instance.PlaySfx(AudioController.SfxCode.Equipment);

                        if (type == ItemBase.ItemType.Set)
                        {
                            _player.UpdateSet((SetItem) itemInfoSelectedItem.item);
                        }
                    }

                }
            }
            
            // Fix me.
            // 소모 아이템을 장착할 때는 아래 코드를 사용해서 소리를 적용해주세요.
            // AudioController.instance.PlaySfx(AudioController.SfxCode.ChainMail1);
        }

        public void Unequip(GameObject sender)
        {
            var slot = sender.GetComponent<EquipSlot>();
            slot.Unequip();
            if (slot.type == ItemBase.ItemType.Set)
            {
                _player.UpdateSet((SetItem) slot.item);
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Equipment);
        }

        private void SetActiveButtonEquip(bool isActive)
        {
            buttonSellImage.enabled = isActive;
            buttonSellText.enabled = isActive;
        }

        public void SelectItem(Toggle item)
        {
            if (item.isOn)
            {
                var label = item.GetComponentInChildren<Text>();
                label.color = new Color(0.1960784f, 1, 0.1960784f, 1);
            }
        }
    }
}
