using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public Widget itemInfoWidget = null;
        public SelectedItem itemInfoSelectedItem = null;

        public Image buttonSellImage = null;
        public Text buttonSellText = null;

        public GameObject[] usableSlots;
        public GameObject[] equipSlots;
        public GameObject btnQuest;
        public GameObject inventory;

        public Stage stage;

        private Player _player = null;
        private Inventory _inventory = null;

        #region Mono

        private void Awake()
        {
            stage = GameObject.Find("Stage").GetComponent<Stage>();

            _inventory = inventory.GetComponent<Inventory>();
        }

        private void OnEnable()
        {
            itemInfoSelectedItem.Clear();
            SetActiveButtonEquip(false);

            Game.Event.OnSlotClick.AddListener(SlotClick);
        }

        private void OnDisable()
        {
            Game.Event.OnSlotClick.RemoveListener(SlotClick);
        }

        #endregion

        public void QuestClick()
        {
            StartCoroutine(QuestAsync());
        }

        private IEnumerator QuestAsync()
        {
            var loadingScreen = Find<LoadingScreen>();
            if (!ReferenceEquals(loadingScreen, null))
            {
                loadingScreen.Show();
            }

            btnQuest.SetActive(false);
            var currentId = ActionManager.Instance.battleLog?.id;
            var equipments = new List<Equipment>();
            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.item?.Data != null)
                {
                    equipments.Add(es.item);
                }
            }

            ActionManager.Instance.HackAndSlash(equipments);
            while (currentId == ActionManager.Instance.battleLog?.id)
            {
                yield return null;
            }

            Game.Event.OnStageStart.Invoke();

            if (!ReferenceEquals(loadingScreen, null))
            {
                loadingScreen.Close();
            }
        }

        public override void Show()
        {
            stage.LoadBackground("dungeon");
            _player = FindObjectOfType<Player>();
            _player.gameObject.transform.position = new Vector2(1.8f, -0.4f);
            _inventory.Show();
            _inventory.SetItemTypesToDisable(ItemBase.ItemType.Material);
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.Cls.ToEnumItemType();
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
            }

            btnQuest.SetActive(true);
            base.Show();
        }

        public override void Close()
        {
            stage.LoadBackground("room");
            _player.gameObject.transform.position = new Vector2(-2.4f, -1.3f);
            _inventory.Close();
            itemInfoWidget.Close();
            Find<Menu>().Show();
            Find<Status>()?.Show();
            base.Close();
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
            SetActiveButtonEquip(slot.Item is ItemUsable);
        }

        public void EquipClick()
        {
            var type = itemInfoSelectedItem.item.Data.Cls.ToEnumItemType();
            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.type == type)
                {
                    es.Equip(itemInfoSelectedItem);
                }

                if (type == ItemBase.ItemType.Set)
                {
                    _player.UpdateSet((SetItem) itemInfoSelectedItem.item);
                }
            }
        }

        public void Unequip(GameObject sender)
        {
            var slot = sender.GetComponent<EquipSlot>();
            slot.Unequip();
            if (slot.type == ItemBase.ItemType.Set)
            {
                _player.UpdateSet((SetItem) slot.item);
            }
        }

        private void SetActiveButtonEquip(bool isActive)
        {
            buttonSellImage.enabled = isActive;
            buttonSellText.enabled = isActive;
        }
    }
}
