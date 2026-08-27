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

            // 데스크톱 타겟에서는 위젯이 **생성조차 되지 않는다** — MainCanvas.cs:191 의
            //   `#if APPLY_MEMORY_IOS_OPTIMIZATION || UNITY_ANDROID || UNITY_IOS` 안에
            //   Widget.Create<MobileShop>() 이 있다. 그래서 Find 는 WidgetNotFoundException 을
            //   던진다(Widget.cs:184). 없으면 MainCanvas 와 **같은 경로**로 직접 만든다.
            //   Create 는 Pool 에 등록하므로 이후 Find 도 정상 동작한다.
            if (!Widget.TryFind<MobileShop>(out var shop))
            {
                shop = Widget.Create<MobileShop>();
            }

            if (shop == null)
            {
                EditorUtility.DisplayDialog("Mobile Shop",
                    "MobileShop 위젯을 만들지 못했습니다. 로비까지 진입했는지 확인하세요.", "OK");
                return;
            }

            shop.ShowAsTab().Forget();
            // 헤더도 같은 이유로 없을 수 있다 — Find 는 던지므로 TryFind 로 받는다(있으면 갱신).
            if (Widget.TryFind<HeaderMenuStatic>(out var header))
            {
                header.UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            }
        }
    }
}
