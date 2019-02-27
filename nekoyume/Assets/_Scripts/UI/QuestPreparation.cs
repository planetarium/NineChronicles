using System.Collections;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public GameObject itemInfo;
        public GameObject[] usableSlots;
        public GameObject[] equipSlots;
        public GameObject btnEquip;
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
            Close();
            var currentAvatar = ActionManager.Instance.Avatar;
            ActionManager.Instance.HackAndSlash();
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
        }

        public override void Show()
        {
            _player = FindObjectOfType<Player>();
            inventory.GetComponent<Inventory>().Show();
            if (_player._weapon != null)
            {
                var slot = equipSlots[4].GetComponent<EquipSlot>();
                var sprite = Resources.Load<Sprite>($"images/item_{_player._weapon.Data.Id}");
                slot.icon.sprite = sprite;
                slot.item = _player._weapon;
            }
            base.Show();
        }

        public override void Close()
        {
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
                itemInfo.GetComponent<Widget>().Toggle();
            }

            selectedSlot = slot;
        }

        public void EquipClick()
        {
            var item = itemInfo.GetComponent<CartItem>();
            if (item.item is Weapon)
            {
                var slot = equipSlots[4].GetComponent<EquipSlot>();
                slot.Equip(item);
            }
            _player.Equip((Weapon) item.item);
        }

        public void UnEquip(GameObject sender)
        {
            if (_player._weapon != null)
            {
                var slot = sender.GetComponent<EquipSlot>();
                slot.UnEquip();
            }
        }

        public void CloseInfo()
        {
            selectedSlot.SlotClick();
            selectedSlot = null;
        }
    }
}
