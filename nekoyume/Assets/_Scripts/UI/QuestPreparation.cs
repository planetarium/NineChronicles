using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public GameObject itemInfo;
        public GameObject[] usableSlots;
        public GameObject[] equipSlots;
        public GameObject btnEquip;
        public GameObject btnQuest;
        public GameObject inventory;
        private InventorySlot selectedSlot;
        private Player _player;
        public Stage stage;

        private void Awake()
        {
            Game.Event.OnSlotClick.AddListener(SlotClick);
            stage = GameObject.Find("Stage").GetComponent<Stage>();
        }

        public void QuestClick()
        {
            StartCoroutine(QuestAsync());
        }

        private IEnumerator QuestAsync()
        {
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
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
        }

        public override void Show()
        {
            stage.LoadBackground("dungeon");
            _player = FindObjectOfType<Player>();
            _player.gameObject.transform.position = new Vector2(1.8f, -0.4f);
            inventory.GetComponent<Inventory>().Show();
            foreach (var equipment in _player.equipments)
            {
                var type = (ItemBase.ItemType) Enum.Parse(typeof(ItemBase.ItemType), equipment.Data.Cls);
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
            inventory.GetComponent<Inventory>().Close();
            itemInfo.GetComponent<Widget>().Close();
            Find<Menu>().Show();
            Find<Status>()?.Show();
            base.Close();
        }
        public void SlotClick(InventorySlot slot)
        {
            if (gameObject.activeSelf && slot.Item != null)
            {
                var slotItem = slot.Item;
                var cartItem = itemInfo.GetComponent<SelectedItem>();
                cartItem.itemName.text = slotItem.Data.Name;
                cartItem.info.text = slotItem.ToItemInfo();
                cartItem.flavour.text = slotItem.Data.Flavour;
                cartItem.icon.sprite = slot.Icon.sprite;
                cartItem.item = slotItem;
                btnEquip.SetActive(slotItem is ItemUsable);
            }

            if (selectedSlot != slot)
            {
                itemInfo.GetComponent<Widget>().Show();
            }
            else
            {
                itemInfo.GetComponent<Widget>().Toggle();
            }
            selectedSlot = slot;
        }

        public void EquipClick()
        {
            var item = itemInfo.GetComponent<SelectedItem>();
            var type = (ItemBase.ItemType) Enum.Parse(typeof(ItemBase.ItemType), item.item.Data.Cls);
            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.type == type)
                {
                    es.Equip(item);
                }

                if (type == ItemBase.ItemType.Weapon)
                {
                    _player.UpdateWeapon((Weapon)item.item);
                }
            }
        }

        public void Unequip(GameObject sender)
        {
            var slot = sender.GetComponent<EquipSlot>();
            slot.Unequip();
            if (slot.type == ItemBase.ItemType.Weapon)
            {
                _player.UpdateWeapon((Weapon)slot.item);
            }

        }

        public void CloseInfo()
        {
            selectedSlot.SlotClick();
            selectedSlot = null;
        }
    }
}
