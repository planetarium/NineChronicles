using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MarketExchangePopup : PopupWidget
    {
        [SerializeField] private Button marketButton;
        [SerializeField] private Button exchangeButton;
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            marketButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
                Helper.Util.OpenWebMarketUrl();
            });

            exchangeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
                Find<ShopBuy>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            });

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });

            CloseWidget = () => Close(true);
            base.Awake();
        }

    }
}
