using UnityEngine;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public GameObject panelBuy;
        public GameObject panelSell;
        public GameObject headerBuy;
        public GameObject headerSell;



        public void BuyClick()
        {
            headerSell.SetActive(false);
            headerBuy.SetActive(true);
            panelSell.SetActive(false);
            panelBuy.SetActive(true);
            GetComponent<Buy>().Init();
        }

        public void SellClick()
        {
            headerBuy.SetActive(false);
            headerSell.SetActive(true);
            panelBuy.SetActive(false);
            panelSell.SetActive(true);
            GetComponent<Sell>().Init();
        }
    }
}
