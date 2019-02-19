using System.Collections.Generic;
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

        private void Awake()
        {
            items = new List<CartItem>();
            Game.Event.OnSlotClick.AddListener(SlotClick);
            Init();
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
            Debug.Log(totalPrice);
        }

        public void Init()
        {
            items.Clear();
            foreach (Transform child in cart.content.transform)
            {
                Destroy(child.gameObject);
            }
            CalcTotalPrice();
            GetComponentInChildren<Inventory>()?.Show();
        }

        public void SlotClick(InventorySlot slot)
        {
            if (gameObject.active)
            {
                GameObject newItem = Instantiate(itemBase, cart.content);
                CartItem item = newItem.GetComponent<CartItem>();
                var itemInfo = slot.Item;
                item.itemName.text = itemInfo.Data.Id.ToString();
                item.price.text = "1";
                item.info.text = "info";
                item.icon.sprite = slot.Icon.sprite;
                item.gameObject.SetActive(true);
                items.Add(item);
                CalcTotalPrice();
            }
        }
    }
}
