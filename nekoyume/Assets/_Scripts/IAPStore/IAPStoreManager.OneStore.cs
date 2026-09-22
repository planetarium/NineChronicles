// 원스토어 결제 경로. Play 경로(IAPStoreManager.cs, Unity IAP)와 완전히 갈린다.
//
// 왜 Unity IAP 커스텀 스토어로 안 갔나: UnityEngine.Purchasing.Product 의 생성자가 internal 이고
// Unity IAP 가 별도 어셈블리라, 미처리 구매 회수(FetchPurchases)에 넘길 Order 를 만들 수 없다.
// 그래서 원스토어 SDK 를 직접 쓰고, UI 에는 IapProductInfo 로 맞춰 준다.
#if ONESTORE

using System.Collections.Generic;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;

#if UNITY_ANDROID
using OneStore.Purchasing;
#endif

namespace Nekoyume.IAPStore
{
    public partial class IAPStoreManager
    {
#if UNITY_ANDROID

        /// <summary>
        /// 원스토어 결제 경로를 실제로 쓸 수 있는 상태인가.
        /// </summary>
        /// <remarks>
        /// 원스토어 SDK 는 JNI 로 안드로이드 네이티브를 부르기 때문에 **실기기에서만 동작한다.**
        /// <c>PurchaseClientImpl</c> 생성자가 <c>Application.platform != RuntimePlatform.Android</c> 이면
        /// <c>PlatformNotSupportedException</c> 을 던진다. 에디터에서는 이 값이 false 가 되어
        /// 기존 Unity IAP(에디터에서는 가짜 스토어) 경로로 떨어지고, 그래야 상점 UI 작업을 계속할 수 있다.
        /// </remarks>
        private static bool IsOneStoreActive =>
            UnityEngine.Application.platform == UnityEngine.RuntimePlatform.Android;

        private OneStorePurchaseService _oneStore;

        private IEnumerable<IapProductInfo> OneStoreProducts =>
            _oneStore is null
                ? Enumerable.Empty<IapProductInfo>()
                : _oneStore.ProductDetails.Values.Select(ToProductInfo);

        /// <summary>
        /// 원스토어는 가격을 마이크로 단위 정수(<c>priceAmountMicros</c>)로 준다. UI 는 할인 전 원가를
        /// 계산하므로 문자열이 아니라 수치가 필요하다 — 1,000,000 으로 나눠 통화 단위로 되돌린다.
        /// </summary>
        private static IapProductInfo ToProductInfo(ProductDetail detail)
        {
            return new IapProductInfo(
                detail.productId,
                detail.title,
                detail.priceCurrencyCode,
                detail.price,
                detail.priceAmountMicros / 1_000_000m);
        }

        private void OneStoreAwake(IEnumerable<string> productIds)
        {
            _oneStore = new OneStorePurchaseService(productIds);

            // 첫 정상 응답이 곧 연결 확인이다. 초기 조회·회수는 Start() 가 이미 건다.
            _oneStore.Connected += () =>
            {
                IsInitialized = true;
                Widget.Find<MobileShop>()?.RefreshGrid();
            };

            _oneStore.ConnectFailed += (code, message) =>
                NcDebug.LogError($"[OneStore] connect failed. code={code} {message}");

            _oneStore.ProductsFetched += _ => Widget.Find<MobileShop>()?.RefreshGrid();

            _oneStore.ProductsFetchFailed += (code, message) =>
                NcDebug.LogError($"[OneStore] product fetch failed. code={code} {message}");

            _oneStore.PurchasesReceived += OnOneStorePurchasesReceived;

            _oneStore.PurchaseFailed += OnOneStorePurchaseFailed;

            _oneStore.ConsumeFailed += (code, message) =>
                // 지급은 끝났는데 소비가 실패한 상태다. 그대로 두면 3일 뒤 자동 환불되므로
                // 다음 회수(OnApplicationPause 복귀)에서 다시 잡아 재시도해야 한다.
                NcDebug.LogError($"[OneStore] consume failed. code={code} {message}");

            _oneStore.Start();
        }

        /// <summary>
        /// 마지막으로 구매를 요청한 상품 ID. 원스토어 실패 콜백은 <c>IapResult</c>(코드·메시지)만
        /// 주고 어떤 상품이 실패했는지 알려주지 않아, 로그·계측에 쓰려면 여기서 기억해야 한다.
        /// </summary>
        private string _oneStoreLastRequestedProductId;

        private void OneStorePurchase(string productId)
        {
            if (_oneStore is null)
            {
                NcDebug.LogError("[OneStore] purchase requested before init.");
                return;
            }

            _oneStoreLastRequestedProductId = productId;
            _oneStore.Purchase(productId, States.Instance.AgentState.address.ToHex());
        }

        /// <summary>
        /// 포그라운드 복귀 시 미처리 구매를 회수한다.
        /// **빠뜨리면 백그라운드에서 돌아온 구매가 회수되지 않아 3일 뒤 자동 환불된다.**
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                _oneStore?.OnResumed();
            }
        }

        private void OnDestroy()
        {
            _oneStore?.Release();
        }

        /// <summary>
        /// 신규 구매와 회수분이 모두 여기로 온다. 서버 지급이 확정된 것만 소비한다.
        /// </summary>
        /// <remarks>
        /// **같은 구매가 두 번 들어온다.** 원스토어 SDK 는 신규 구매와 <c>QueryPurchases</c> 회수를
        /// 같은 콜백(<c>OnPurchaseSucceeded</c>)으로 주는데, 구매 직후 회수가 겹쳐 돌기 때문이다.
        /// 실측에서 배송 요청이 2회, <c>ConsumePurchase</c> 도 2회 나가고 두 번째가
        /// <c>RESULT_ITEM_NOT_OWNED(8)</c> 로 실패했다. 서버가 멱등해서 중복 지급은 없었지만
        /// 불필요한 요청과 오류 로그가 남으므로 여기서 건다.
        ///
        /// **두 번이 겹쳐서 오지 않고 1초쯤 벌어져서 온다.** 그래서 배송 중에만 표시를 두면
        /// (첫 배송이 끝나며 표시가 지워져) 두 번째를 못 막는다. 성공한 구매는 표시를 남기고,
        /// 실패·보류일 때만 지워서 다음 회수가 재시도하게 한다.
        /// </remarks>
        private readonly HashSet<string> _oneStoreHandled = new();

        private async void OnOneStorePurchasesReceived(IReadOnlyList<PurchaseData> purchases)
        {
            foreach (var purchase in purchases)
            {
                var key = purchase.PurchaseId;
                if (!_oneStoreHandled.Add(key))
                {
                    NcDebug.Log($"[OneStore] already handled. skip. productId={purchase.ProductId}");
                    continue;
                }

                var handled = false;
                try
                {
                    handled = await DeliverOneStorePurchaseAsync(purchase);
                }
                finally
                {
                    // 성공했으면 표시를 **남긴다**. 지우면 잠시 뒤 같은 구매가 다시 올라올 때
                    // 또 배송·소비하게 된다.
                    // 실패·보류였으면 지워서 다음 회수(OnResumed)가 재시도할 수 있게 한다.
                    if (!handled)
                    {
                        _oneStoreHandled.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// 지급과 그 뒤처리. 완료 UI 는 Play 경로(<c>OnPurchaseRequestAsync</c>)와 같은 것을 보여준다 —
        /// 사용자 입장에서 스토어가 달라도 결제 경험은 같아야 한다.
        /// </summary>
        private async System.Threading.Tasks.Task<bool> DeliverOneStorePurchaseAsync(PurchaseData purchase)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;

            // Play 경로의 ProcessPurchase 와 같은 계측·로그를 남긴다.
            try
            {
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/ProcessPurchase",
                    ("product-id", purchase.ProductId),
                    ("transaction-id", purchase.PurchaseId),
                    ("agent-address", states?.AgentState?.address.ToHex()),
                    ("avatar-address", states?.CurrentAvatarState?.address.ToHex()),
                    ("planet-id", Game.Game.instance?.CurrentPlanetId?.ToString()));
            }
            catch (System.Exception error)
            {
                NcDebug.LogError("[OneStore] analyzer error " + error);
            }

            PurchaseLog(purchase.ProductId, purchase.PurchaseId, "PurchaseSuccess");

            // 체인 상태가 준비되지 않았으면 보내지 않는다. 주소 없이 보내면 서버가 지급할 대상을
            // 알 수 없다. 소비하지 않고 두면 다음 회수(OnResumed)에서 다시 잡힌다.
            if (states?.AgentState?.address == null ||
                states?.CurrentAvatarState?.address == null ||
                Game.Game.instance?.CurrentPlanetId == null)
            {
                NcDebug.LogWarning(
                    $"[OneStore] state not ready. defer delivery. productId={purchase.ProductId}");
                return false;
            }

            try
            {
                var result = await ApiClients.Instance.IAPServiceManager.PurchaseRequestAsync(
                    BuildOneStoreReceipt(purchase),
                    states.AgentState.address.ToHex(),
                    states.CurrentAvatarState.address.ToHex(),
                    Game.Game.instance.CurrentPlanetId.ToString(),
                    purchase.PurchaseId,
                    string.Empty);

                Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();

                if (!IsDelivered(result))
                {
                    // 소비하지 않는다. 소비하면 원스토어에서 구매가 사라져 되살릴 수 없다.
                    // 남겨두면 다음 회수에서 다시 잡혀 재시도된다.
                    NcDebug.LogError(
                        $"[OneStore] not delivered. productId={purchase.ProductId} " +
                        $"status={result?.Status.ToString() ?? "null"}");
                    Analyzer.Instance.Track(
                        "Unity/Shop/IAP/PurchaseResult",
                        ("product-id", purchase.ProductId),
                        ("result", "NotDelivered"),
                        ("status", result?.Status.ToString() ?? "null"));
                    popup.Show("UI_ERROR", "UI_IAP_PURCHASE_FAILED", "UI_OK", true);
                    return false;
                }

                // 지급이 확정된 뒤에만 소비한다.
                _oneStore.Consume(purchase);

                Widget.Find<MobileShop>()?.PurchaseComplete(purchase.ProductId);
                PurchaseCountRefresh(purchase.ProductId);
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/PurchaseResult",
                    ("product-id", purchase.ProductId),
                    ("result", "Complete"),
                    ("transaction-id", purchase.PurchaseId));

                // 상세 팝업을 완료 팝업보다 **먼저**, 애니메이션 없이 닫는다.
                //
                // 같은 프레임에 완료 팝업을 띄우면서 닫으면 닫힘 애니메이션이 끝나지 않아
                // Widget 의 `_isClosed` 만 true 로 남고 GameObject 는 활성 상태로 화면에 그대로
                // 보인다. 그 뒤로는 Close() 가 `if (_isClosed && !ignoreCloseAnimation) return;`
                // 에서 즉시 빠져나가 X 버튼을 몇 번 눌러도 닫히지 않는다. 게다가 WidgetStack 에서는
                // 이미 빠졌으므로 레이캐스트 차단막만 남아 다른 버튼도 눌리지 않는다.
                // (실측: Close 가 13회 불렸는데 전부 가드에서 반환됨)
                Widget.Find<ShopListPopup>()?.Close(true);
                Widget.Find<MobileShop>()?.RefreshGrid();

                if (_initializedProductSchema.TryGetValue(purchase.ProductId, out var product) &&
                    product.Mileage > 0)
                {
                    popup.Show(
                        "UI_COMPLETED",
                        "UI_IAP_PURCHASE_WITH_MILEAGE_COMPLETE",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information,
                        product.Mileage);
                }
                else
                {
                    popup.Show(
                        "UI_COMPLETED",
                        "UI_IAP_PURCHASE_COMPLETE",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information);
                }

                // 결제한 계정의 비밀번호가 캐시에 없으면 재설정을 유도한다(Play 경로와 동일).
                popup.ConfirmCallback = () =>
                {
                    var cachedPassphrase = KeyManager.GetCachedPassphrase(
                        states.AgentState.address,
                        Util.AesDecrypt,
                        string.Empty);
                    if (cachedPassphrase.Equals(string.Empty))
                    {
                        Widget.Find<LoginSystem>().ShowResetPassword();
                    }
                };

                return true;
            }
            catch (System.Exception e)
            {
                // 서버 호출 자체가 실패했다. 소비하지 않고 두면 회수에서 재시도된다.
                NcDebug.LogError($"[OneStore] deliver failed. productId={purchase.ProductId} {e}");
                Widget.Find<MobileShop>()?.RefreshGrid();
                Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();
                popup.Show(L10nManager.Localize("UI_ERROR"), e.Message, L10nManager.Localize("UI_OK"), false);
                return false;
            }
        }

        /// <summary>
        /// IAP 서버가 읽는 봉투로 감싼다. Google 경로의 Unity IAP 영수증과 **같은 모양**이라
        /// 서버는 <c>Store</c> 값만 분기하면 된다 — <c>Payload</c> 안이 <c>{json, signature}</c> 인 것도 동일하다.
        /// </summary>
        /// <remarks>
        /// 서버는 <c>data["Payload"]</c> 를 파싱한 뒤 그 안의 <c>json</c> 을 다시 파싱한다
        /// (<c>shared/schemas/receipt.py</c> 의 Google 분기). 그 구조를 그대로 맞춘다.
        /// </remarks>
        private static string BuildOneStoreReceipt(PurchaseData purchase)
        {
            return UnityEngine.JsonUtility.ToJson(new OneStoreReceiptEnvelope
            {
                Store = "OneStore",
                TransactionID = purchase.PurchaseId,
                // JsonReceipt 가 이미 {"json":"...","signature":"..."} 문자열이다. 문자열 필드로 넣으면
                // JsonUtility 가 이스케이프해 주므로 Google 경로와 같은 중첩 모양이 된다.
                Payload = purchase.JsonReceipt,
            });
        }

        [System.Serializable]
        private class OneStoreReceiptEnvelope
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        /// <summary>
        /// 구매 실패. Play 경로의 <c>HandlePurchaseFailed</c> 와 같은 처리를 한다.
        /// </summary>
        private void OnOneStorePurchaseFailed(int code, string message)
        {
            var reason = (OneStore.Purchasing.ResponseCode)code;

            NcDebug.LogError($"[OneStore] purchase failed. code={code} {message}");
            PurchaseLog(_oneStoreLastRequestedProductId ?? "unknown", string.Empty, $"PurchaseFailed[{reason}]");
            Analyzer.Instance.Track(
                "Unity/Shop/IAP/PurchaseResult",
                ("product-id", _oneStoreLastRequestedProductId ?? "unknown"),
                ("result", reason.ToString()),
                ("message", message));

            // 스피너를 반드시 끈다. 안 끄면 취소·거부 후에도 팝업을 닫을 때까지 로딩이 계속 돈다.
            Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();
            Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
            Widget.Find<MobileShop>()?.RefreshGrid();

            // 사용자가 스스로 취소한 경우는 알릴 것이 없다.
            if (reason == OneStore.Purchasing.ResponseCode.RESULT_USER_CANCELED)
            {
                return;
            }

            Widget.Find<IconAndButtonSystem>()?.Show(
                "UI_ERROR", "UI_IAP_PURCHASE_FAILED", "UI_OK", true);
        }

#else // ONESTORE 는 켜졌지만 빌드 타깃이 Android 가 아닌 경우 — 플러그인 타입이 존재하지 않는다.

        private static bool IsOneStoreActive => false;

        private IEnumerable<IapProductInfo> OneStoreProducts => Enumerable.Empty<IapProductInfo>();

        private void OneStoreAwake(IEnumerable<string> productIds)
        {
            NcDebug.LogWarning("[OneStore] skipped: build target is not Android.");
        }

        private void OneStorePurchase(string productId)
        {
            NcDebug.LogWarning("[OneStore] purchase skipped: build target is not Android.");
        }

#endif // UNITY_ANDROID
    }
}

#endif // ONESTORE
