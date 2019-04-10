using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
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

        private Player _player;

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
            AudioController.PlayClick();
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
            AudioController.PlayClick();
        }

        public override void Show()
        {
            _player = FindObjectOfType<Player>();
            if (_player != null)
            {
                _player.gameObject.SetActive(false);
            }

            BuyClick();
            base.Show();
        }

        public override void Close()
        {
            if (_player != null)
            {
                _player.gameObject.SetActive(true);
            }

            stage.LoadBackground("room");
            Find<Status>()?.Show();
            Find<Menu>()?.Show();
            base.Close();
            AudioController.PlayClick();
        }
        public void RemoveItem(GameObject o)
        {
            var item = o.GetComponent<SelectedItem>();
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
