using System;
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
            var sellItems = items.Select(i => i.item).ToList();
            var currentAvatar = ActionManager.Instance.Avatar;
            ActionManager.Instance.Sell(sellItems, Convert.ToInt64(totalPrice.text));
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }
            Debug.Log("Sell");
        }

        public void SlotClick(InventorySlot slot)
        {
            if (gameObject.active)
            {
                GameObject newItem = Instantiate(itemBase, cart.content);
                CartItem cartItem = newItem.GetComponent<CartItem>();
                var itemInfo = slot.Item;
                cartItem.itemName.text = itemInfo.Data.Id.ToString();
                cartItem.price.text = "1";
                cartItem.info.text = "info";
                cartItem.icon.sprite = slot.Icon.sprite;
                cartItem.gameObject.SetActive(true);
                cartItem.item = itemInfo;
                items.Add(cartItem);
                CalcTotalPrice();
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
            base.Close();
        }
    }
}
