using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using Nekoyume.UI.Module;   // HeaderMenuStatic 은 Nekoyume.UI 가 아니라 .Module 에 있다
using UnityEditor;
using UnityEngine;

namespace Nekoyume.EditorTools
{
    /// <summary>
    /// 인게임 IAP 상점(MobileShop)을 플랫폼과 무관하게 강제로 연다.
    ///
    /// 왜 필요한가: 상점 진입이 호출부에서 `#if UNITY_ANDROID || UNITY_IOS` 로 갈린다
    /// (LobbyMenu.cs:489, MarketExchangePopup.cs). 데스크톱 타겟에서는 MarketExchangePopup →
    /// 외부 웹 마켓으로 빠져 인게임 상점 UI 를 볼 수 없다.
    /// 반대로 모바일 타겟으로 두면 에디터에서 SoftMask 셰이더(SOFTMASK_EDITOR 변형)가
    /// 컴파일에 실패해 화면이 마젠타로 깨진다.
    ///
    /// MobileShop 클래스 자체에는 플랫폼 분기가 없으므로, macOS 타겟(셰이더 정상)에서
    /// 이 메뉴로 위젯만 직접 열면 상점 UI 를 확인할 수 있다.
    /// </summary>
    public static class MobileShopOpener
    {
        [MenuItem("Tools/IAP/Open Mobile Shop (Play Mode)")]
        private static void Open()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Mobile Shop",
                    "Play 모드에서만 열 수 있습니다. Play 후 로비 진입한 뒤 다시 실행하세요.", "OK");
                return;
            }

            var shop = Widget.Find<MobileShop>();
            if (shop == null)
            {
                EditorUtility.DisplayDialog("Mobile Shop",
                    "MobileShop 위젯을 찾지 못했습니다. 로비까지 진입했는지 확인하세요.", "OK");
                return;
            }

            shop.ShowAsTab().Forget();
            var header = Widget.Find<HeaderMenuStatic>();
            if (header != null)
            {
                header.UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            }
        }
    }
}
