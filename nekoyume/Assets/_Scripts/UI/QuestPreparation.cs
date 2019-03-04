using System;
using System.Collections;
using Nekoyume.Action;
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

        private void Awake()
        {
            Game.Event.OnSlotClick.AddListener(SlotClick);
        }

        public void QuestClick()
        {
            StartCoroutine(QuestAsync());
        }

        private IEnumerator QuestAsync()
        {
            btnQuest.SetActive(false);
            var currentId = ActionManager.Instance.battleLog.id;
            ActionManager.Instance.HackAndSlash(_player.equipments);
            while (currentId == ActionManager.Instance.battleLog.id)
            {
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
        }

        public override void Show()
        {
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
                        es.Set(_player, equipment);
                    }
                }
            }
            btnQuest.SetActive(true);
            base.Show();
        }

        public override void Close()
        {
            _player.gameObject.transform.position = new Vector2(-2.4f, -1.3f);
            inventory.GetComponent<Inventory>().Close();
            itemInfo.GetComponent<Widget>().Close();
            base.Close();
        }
        public void SlotClick(InventorySlot slot)
        {
            if (gameObject.activeSelf && slot.Item != null)
            {
                var slotItem = slot.Item;
                var cartItem = itemInfo.GetComponent<CartItem>();
                cartItem.itemName.text = slotItem.Data.Id.ToString();
                cartItem.info.text = "info";
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
            var item = itemInfo.GetComponent<CartItem>();
            var type = (ItemBase.ItemType) Enum.Parse(typeof(ItemBase.ItemType), item.item.Data.Cls);
            foreach (var slot in equipSlots)
            {
                var es = slot.GetComponent<EquipSlot>();
                if (es.type == type)
                {
                    es.Equip(_player, item);
                }
            }
        }

        public void Unequip(GameObject sender)
        {
            if (_player.weapon != null)
            {
                var slot = sender.GetComponent<EquipSlot>();
                slot.Unequip(_player);
            }
        }

        public void CloseInfo()
        {
            selectedSlot.SlotClick();
            selectedSlot = null;
        }
    }
}
