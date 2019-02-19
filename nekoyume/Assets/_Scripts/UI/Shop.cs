using UnityEngine;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public GameObject panelBuy;
        public GameObject panelSell;


        public void BuyClick()
        {
            panelSell.SetActive(false);
            GetComponent<Buy>().Init();
            panelBuy.SetActive(true);
        }

        public void SellClick()
        {
            panelBuy.SetActive(false);
            panelSell.SetActive(true);
        }
    }
}