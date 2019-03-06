using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Sell : Widget
    {
        public GameObject btnConfirm;
        public ScrollRect cart;
        public List<CartItem> items;
        public Text totalPrice;
        public GameObject itemBase;
        public GameObject itemInfo;
        private InventorySlot _selectedSlot;

        private void Awake()
        {
            items = new List<CartItem>();
            Game.Event.OnSlotClick.AddListener(SlotClick);
        }

        public void CalcTotalPrice()
        {
            var total = 0;
            foreach (var item in items)
            {
                int price;
                int.TryParse(item.price.text, out price);
                total += price;
            }
            totalPrice.text = total.ToString();
        }

        public void ConfirmClick()
        {
            StartCoroutine(SellAsync());
        }

        public IEnumerator SellAsync()
        {
            btnConfirm.SetActive(false);
            var sellItems = items.Select(i => i.item).ToList();
            var currentAvatar = ActionManager.Instance.Avatar;
            ActionManager.Instance.Sell(sellItems, decimal.Parse(totalPrice.text));
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }
            Debug.Log("Sell");
            btnConfirm.SetActive(true);
        }

        public void SlotClick(InventorySlot slot)
        {
            if (gameObject.active && slot.Item != null)
            {
                var slotItem = slot.Item;
                var cartItem = itemInfo.GetComponent<CartItem>();
                cartItem.itemName.text = slotItem.Data.Id.ToString();
                cartItem.info.text = "info";
                cartItem.price.text = "1";
                cartItem.icon.sprite = slot.Icon.sprite;
                cartItem.item = slotItem;
                if (_selectedSlot != slot)
                {
                    itemInfo.GetComponent<Widget>().Show();
                }
                else
                {
                    itemInfo.GetComponent<Widget>().Toggle();
                }
                _selectedSlot = slot;
            }
        }

        public override void Show()
        {
            GetComponentInChildren<Inventory>()?.Show();
            CalcTotalPrice();
            base.Show();
        }

        public override void Close()
        {
            items.Clear();
            foreach (Transform child in cart.content.transform)
            {
                Destroy(child.gameObject);
            }
            CalcTotalPrice();
            itemInfo.GetComponent<Widget>().Close();
            base.Close();
        }

        public void SellClick(GameObject sender)
        {
            GameObject newItem = Instantiate(itemBase, cart.content);
            var cartItem = newItem.GetComponent<CartItem>();
            var item = sender.GetComponent<CartItem>();
            cartItem.itemName.text = item.itemName.text;
            cartItem.price.text = item.price.text;
            cartItem.info.text = item.info.text;
            cartItem.icon.sprite = item.icon.sprite;
            cartItem.gameObject.SetActive(true);
            cartItem.item = item.item;
            items.Add(cartItem);
            CalcTotalPrice();
        }

        public void CloseInfo()
        {
            _selectedSlot.SlotClick();
            _selectedSlot = null;
        }
    }
}
