using System.Collections.Generic;
using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public GameObject panelBuy;
        public GameObject panelSell;
        public GameObject headerBuy;
        public GameObject headerSell;
        public Stage stage;

        private void Awake()
        {
            stage = GameObject.Find("Stage").GetComponent<Stage>();
        }

        public void BuyClick()
        {
            stage.LoadBackground("shopBuy");
            headerSell.SetActive(false);
            headerBuy.SetActive(true);
            panelSell.SetActive(false);
            panelBuy.SetActive(true);
            GetComponent<Sell>().Close();
            GetComponent<Buy>().Show();
        }

        public void SellClick()
        {
            stage.LoadBackground("shopSell");
            headerBuy.SetActive(false);
            headerSell.SetActive(true);
            panelBuy.SetActive(false);
            panelSell.SetActive(true);
            GetComponent<Buy>().Close();
            GetComponent<Sell>().Show();
        }

        public override void Show()
        {
            BuyClick();
            base.Show();
        }

        public override void Close()
        {
            stage.LoadBackground("room");
            base.Close();
        }
        public void RemoveItem(GameObject o)
        {
            var item = o.GetComponent<CartItem>();
            if (panelBuy.activeSelf)
            {
                var buy = GetComponent<Buy>();
                buy.items.Remove(item);
                buy.CalcTotalPrice();
            }
            else
            {
                var sell = GetComponent<Sell>();
                sell.items.Remove(item);
                sell.CalcTotalPrice();
            }
            Destroy(o);
        }
    }
}
