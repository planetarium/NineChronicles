using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Sell : Widget
    {
        public GameObject btnConfirm;
        public ScrollRect cart;
        public List<SelectedItem> items;
        public Text totalPrice;
        public GameObject itemBase;
        public Widget itemInfoWidget = null;
        public SelectedItem itemInfoSelectedItem = null;
        public Image buttonSellImage = null;
        public Text buttonSellText = null;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            items = new List<SelectedItem>();
        }

        private void OnEnable()
        {
            itemInfoSelectedItem.Clear();
            SetActiveButtonSell(false);

            Game.Event.OnSlotClick.AddListener(SlotClick);
        }

        private void OnDisable()
        {
            Game.Event.OnSlotClick.RemoveListener(SlotClick);
        }

        #endregion

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
            AudioController.PlayClick();
        }

        public IEnumerator SellAsync()
        {
            btnConfirm.SetActive(false);
            var sellItems = items.Select(i => i.item).ToList();
            var currentAvatar = AvatarManager.Avatar;
//            ActionManager.instance.Sell(sellItems, decimal.Parse(totalPrice.text));
            while (currentAvatar.Equals(AvatarManager.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log("Sell");
            btnConfirm.SetActive(true);
        }

        private void SlotClick(InventorySlot slot, bool toggled)
        {
            if (ReferenceEquals(slot, null) ||
                ReferenceEquals(slot.Item, null) ||
                !toggled)
            {
                itemInfoSelectedItem.Clear();
                SetActiveButtonSell(false);
                return;
            }

            itemInfoSelectedItem.SetItem(slot.Item);
            itemInfoSelectedItem.SetIcon(slot.Icon.sprite);
            SetActiveButtonSell(true);
            AudioController.PlaySelect();
        }

        public override void Show()
        {
            GetComponentInChildren<Inventory>()?.Show();
            itemInfoWidget.Show();
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
            itemInfoWidget.Close();
            base.Close();
        }

        public void SellClick(GameObject sender)
        {
            GameObject newItem = Instantiate(itemBase, cart.content);
            var cartItem = newItem.GetComponent<SelectedItem>();
            var item = sender.GetComponent<SelectedItem>();
            cartItem.itemName.text = item.itemName.text;
            cartItem.price.text = item.price.text;
            cartItem.info.text = item.info.text;
            cartItem.icon.sprite = item.icon.sprite;
            cartItem.gameObject.SetActive(true);
            cartItem.item = item.item;
            items.Add(cartItem);
            CalcTotalPrice();
            AudioController.PlayClick();
        }

        private void SetActiveButtonSell(bool isActive)
        {
            buttonSellImage.enabled = isActive;
            buttonSellText.enabled = isActive;
        }
    }
}
