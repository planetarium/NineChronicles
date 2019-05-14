using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Buy : Widget
    {
        public GameObject btnConfirm;
        public ScrollRect cart;
        public Button sword;
        public List<SelectedItem> items;
        public List<Item> shopItems;
        public Text totalPrice;
        public GameObject itemBase;
        public GameObject sellItemBase;
        public Transform grid;

        protected override void Awake()
        {
            base.Awake();
            
            items = new List<SelectedItem>();
            shopItems = new List<Item>();
        }

        public void SwordClick()
        {
            GameObject newItem = Instantiate(itemBase, cart.content);
            SelectedItem item = newItem.GetComponent<SelectedItem>();
            var itemInfo = sword.GetComponent<Item>();
            item.itemName.text = itemInfo.itemName.text;
            item.price.text = itemInfo.price.text;
            item.info.text = itemInfo.info.text;
            item.icon.sprite = sword.GetComponent<Image>().sprite;
            item.gameObject.SetActive(true);
            items.Add(item);
            CalcTotalPrice();
            AudioController.PlayClick();
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
            AudioController.PlayClick();
        }

        public override void Show()
        {
            foreach (var pair in ActionManager.instance.shop.Value.items)
            {
                foreach (var itemInfo in pair.Value)
                {
                    GameObject newItem = Instantiate(sellItemBase, grid);
                    Item item = newItem.GetComponent<Item>();
                    item.itemName.text = itemInfo.item.Data.id.ToString();
                    item.price.text = "1";
                    item.info.text = "info";
                    item.icon.sprite = ItemBase.GetSprite(itemInfo.item);
                    item.seller = new Address(pair.Key);
                    item.gameObject.SetActive(true);
                    shopItems.Add(item);
                }
            }

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
