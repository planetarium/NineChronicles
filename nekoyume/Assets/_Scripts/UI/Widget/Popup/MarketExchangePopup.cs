using Cysharp.Threading.Tasks;
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
#if UNITY_ANDROID || UNITY_IOS
                // 모바일에서는 외부 웹 마켓 대신 기존 인앱 모바일 샵으로 이동한다.
                Find<MobileShop>().ShowAsTab().Forget();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
#else
                Helper.Util.OpenWebMarketUrl();
#endif
            });

            exchangeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
                // 모바일에서도 MobileShop 리다이렉트를 우회하고 실제 거래소를 연다.
                Find<ShopBuy>().ShowExchange();
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
