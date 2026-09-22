// 원스토어 결제 경로. Play 경로(IAPStoreManager + Unity IAP)와 나란히 두고 ONESTORE 빌드에서만 쓴다.
//
// UNITY_ANDROID 를 함께 거는 이유: 플러그인 타입들이 `#if UNITY_ANDROID || !UNITY_EDITOR` 로 가드돼
// 있어서, 빌드 타깃이 Android 가 아닌 에디터에서는 존재하지 않는다. 같은 조건을 걸지 않으면
// StoreEnvironment.cs 가 냈던 것과 같은 CS0103 이 난다.
#if ONESTORE && UNITY_ANDROID

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OneStore.Auth;
using OneStore.Purchasing;
// OneStore.Auth 와 OneStore.Purchasing 이 각각 ResponseCode 를 갖고 있어 그냥 쓰면 CS0104 가 난다.
// 이 파일이 보는 것은 결제 쪽이다.
using ResponseCode = OneStore.Purchasing.ResponseCode;

namespace Nekoyume.IAPStore
{
    /// <summary>
    /// 원스토어 인앱결제 클라이언트를 감싼다. 서버 연동은 하지 않는다 — 영수증을 이벤트로 넘기고,
    /// 지급이 확정되면 호출자가 <see cref="Consume"/> 를 부른다.
    /// </summary>
    /// <remarks>
    /// **소비 순서가 중요하다.** 소비하면 원스토어에서 그 구매가 사라지므로, IAP 서버의 지급이
    /// 확정된 뒤에 소비해야 한다. 지급이 실패하면 소비하지 않고 두면 다음 회수(<see cref="OnResumed"/>)
    /// 때 다시 잡힌다. 우리 IAP 서버는 같은 영수증에 대해 멱등하게 응답하므로 재시도가 안전하다.
    ///
    /// 다만 **3일 안에 소비 또는 승인하지 않으면 원스토어가 자동 환불한다.** 그래서 회수는 앱 시작과
    /// 포그라운드 복귀 두 지점에서 반드시 돌아야 한다.
    ///
    /// 우리 상품은 전부 소비성이라(<c>ProductType.INAPP</c> + 소비) 승인(acknowledge)은 쓰지 않는다.
    /// 영구성 상품이 생기면 그때는 소비가 아니라 승인을 해야 한다 — 소비하면 재구매가 열린다.
    /// </remarks>
    public sealed class OneStorePurchaseService : IPurchaseCallback
    {
        private const string Tag = "[OneStoreIAP]";

        private readonly PurchaseClientImpl _client;
        private readonly ReadOnlyCollection<string> _productIds;
        private readonly Dictionary<string, ProductDetail> _productDetails = new();

        private OneStoreAuthClientImpl _authClient;
        private bool _connected;

        public bool IsConnected => _connected;

        public IReadOnlyDictionary<string, ProductDetail> ProductDetails => _productDetails;

        /// <summary>연결에 성공했다. 이 시점부터 조회·구매를 부를 수 있다.</summary>
        public event System.Action Connected;

        /// <summary>연결에 실패했다. (응답 코드, 메시지)</summary>
        public event System.Action<int, string> ConnectFailed;

        public event System.Action<IReadOnlyList<ProductDetail>> ProductsFetched;

        public event System.Action<int, string> ProductsFetchFailed;

        /// <summary>
        /// 신규 구매와 미처리 구매 회수가 **둘 다** 이 이벤트로 온다. 원스토어 SDK 가 같은 콜백
        /// (<c>OnPurchaseSucceeded</c>)으로 주기 때문이다. 호출자는 영수증을 서버로 보내 지급하고,
        /// 성공하면 <see cref="Consume"/> 를 불러야 한다.
        /// </summary>
        public event System.Action<IReadOnlyList<PurchaseData>> PurchasesReceived;

        /// <summary>구매 실패. 사용자 취소(<c>RESULT_USER_CANCELED</c>)도 여기로 오므로 구분해서 다뤄야 한다.</summary>
        public event System.Action<int, string> PurchaseFailed;

        public event System.Action<PurchaseData> ConsumeSucceeded;

        public event System.Action<int, string> ConsumeFailed;

        public OneStorePurchaseService(IEnumerable<string> productIds)
        {
            if (productIds is null)
            {
                throw new ArgumentNullException(nameof(productIds));
            }

            _productIds = new ReadOnlyCollection<string>(productIds.ToList());

            // SDK 내부 로그(VERBOSE). 원스토어가 상품조회에 뭐라고 응답했는지는 이걸 켜야 보인다 —
            // 요청한 상품 중 일부만 돌아올 때 그 이유가 여기 남는다.
            //
            // TODO(원스토어 출시 전): 이 줄을 지우거나 개발 빌드로 제한할 것.
            //   VERBOSE 로그에는 구매 토큰·서명이 실릴 수 있고, 그대로 내보내면 사용자 기기
            //   logcat 에 결제 자격증명이 남는다. 지금은 ONESTORE 빌드가 테스트 전용이라 켜 둔다.
            OneStore.Common.OneStoreLogger.EnableDebugLog(true);

            _client = new PurchaseClientImpl(OneStoreLicenseKey.Get());
        }

#region Lifecycle

        /// <summary>앱 시작 시 한 번. 연결되면 <see cref="Connected"/> 가 온다.</summary>
        public void Start()
        {
            NcDebug.Log($"{Tag} Initialize. products={_productIds.Count}");
            _client.Initialize(this);

            // Initialize() 는 연결 성공을 콜백으로 알리지 않는다(내부 로그만 남긴다). 실패했을 때만
            // OnSetupFailed 가 온다. 그래서 여기서 바로 요청을 건다 — SDK 가 연결을 보장한 뒤 실행한다.
            FetchProducts();
            QueryUnprocessedPurchases();
        }

        /// <summary>
        /// 액티비티 <c>onResume</c> 에 해당하는 지점에서 부른다.
        /// **빠뜨리면 백그라운드에서 돌아왔을 때 미처리 구매가 회수되지 않아 3일 뒤 자동 환불된다.**
        /// </summary>
        public void OnResumed()
        {
            // 연결이 끊겼어도 Initialize 를 다시 부르지 않는다 — 리스너가 중복 등록된다.
            // SDK 가 요청 시점에 재연결한다.
            QueryUnprocessedPurchases();
        }

        public void Release()
        {
            NcDebug.Log($"{Tag} EndConnection");
            _connected = false;
            _client.EndConnection();
        }

#endregion Lifecycle

#region Requests

        // SDK 가 연결을 알아서 관리한다 — PurchaseClientImpl 의 각 메서드가 ExecuteService() 로
        // 감싸져 있어서, 연결이 안 된 상태면 먼저 연결한 뒤 요청을 실행한다. 그래서 여기서 연결
        // 여부를 막지 않는다. 막으면 "연결 확인은 응답으로 하는데 요청은 연결돼야 보낸다"는
        // 순환에 빠져 아무것도 시작되지 않는다.

        public void FetchProducts()
        {
            NcDebug.Log($"{Tag} QueryProductDetails. count={_productIds.Count}");
            _client.QueryProductDetails(_productIds, ProductType.INAPP);
        }

        /// <summary>소비되지 않은 구매를 조회한다. 결과는 <see cref="PurchasesReceived"/> 로 온다.</summary>
        public void QueryUnprocessedPurchases()
        {
            NcDebug.Log($"{Tag} QueryPurchases");
            _client.QueryPurchases(ProductType.INAPP);
        }

        public void Purchase(string productId, string gameUserId = null, string developerPayload = null)
        {
            NcDebug.Log($"{Tag} Purchase. productId={productId}");

            var builder = new PurchaseFlowParams.Builder()
                .SetProductId(productId)
                .SetProductType(ProductType.INAPP);

            if (!string.IsNullOrEmpty(gameUserId))
            {
                builder.SetGameUserId(gameUserId);
            }

            if (!string.IsNullOrEmpty(developerPayload))
            {
                builder.SetDeveloperPayload(developerPayload);
            }

            _client.Purchase(builder.Build());
        }

        /// <summary>
        /// 구매를 소비한다. **IAP 서버 지급이 확정된 뒤에만 부른다** — 소비하면 원스토어에서 그 구매가
        /// 사라져 회수로 되살릴 수 없다.
        /// </summary>
        public void Consume(PurchaseData purchase)
        {
            if (purchase is null)
            {
                NcDebug.LogError($"{Tag} Consume called with null purchase.");
                return;
            }

            NcDebug.Log($"{Tag} ConsumePurchase. productId={purchase.ProductId}");
            _client.ConsumePurchase(purchase);
        }

#endregion Requests

#region IPurchaseCallback

        void IPurchaseCallback.OnSetupFailed(IapResult iapResult)
        {
            _connected = false;
            NcDebug.LogError($"{Tag} setup failed. {iapResult}");
            ConnectFailed?.Invoke(iapResult.Code, iapResult.Message);
        }

        void IPurchaseCallback.OnProductDetailsSucceeded(List<ProductDetail> productDetails)
        {
            // 연결 성공을 알리는 별도 콜백이 없다. 첫 정상 응답이 곧 연결 확인이다.
            MarkConnected();

            _productDetails.Clear();
            foreach (var detail in productDetails)
            {
                _productDetails[detail.productId] = detail;
            }

            NcDebug.Log(
                $"{Tag} product details fetched. count={productDetails.Count} " +
                $"ids=[{string.Join(", ", productDetails.Select(d => d.productId))}]");
            ProductsFetched?.Invoke(productDetails);
        }

        void IPurchaseCallback.OnProductDetailsFailed(IapResult iapResult)
        {
            NcDebug.LogError($"{Tag} product details failed. {iapResult}");
            ProductsFetchFailed?.Invoke(iapResult.Code, iapResult.Message);
        }

        void IPurchaseCallback.OnPurchaseSucceeded(List<PurchaseData> purchases)
        {
            MarkConnected();

            // 구매 토큰·서명은 찍지 않는다. 어디까지 갔는지 가리는 데는 상품 ID 와 상태로 충분하다.
            foreach (var purchase in purchases)
            {
                NcDebug.Log(
                    $"{Tag} purchase received. productId={purchase.ProductId} " +
                    $"state={purchase.PurchaseState} acknowledged={purchase.Acknowledged}");
            }

            PurchasesReceived?.Invoke(purchases);
        }

        void IPurchaseCallback.OnPurchaseFailed(IapResult iapResult)
        {
            var code = (ResponseCode)iapResult.Code;

            // 사용자 취소는 오류가 아니다. 오류 창을 띄우면 흔한 불만이 된다.
            if (code == ResponseCode.RESULT_USER_CANCELED)
            {
                NcDebug.Log($"{Tag} purchase canceled by user.");
            }
            else
            {
                NcDebug.LogError($"{Tag} purchase failed. {iapResult}");
            }

            PurchaseFailed?.Invoke(iapResult.Code, iapResult.Message);
        }

        void IPurchaseCallback.OnConsumeSucceeded(PurchaseData purchase)
        {
            NcDebug.Log($"{Tag} consume succeeded. productId={purchase.ProductId}");
            ConsumeSucceeded?.Invoke(purchase);
        }

        void IPurchaseCallback.OnConsumeFailed(IapResult iapResult)
        {
            // 소비 실패는 지급이 끝난 뒤에 생긴다. 그대로 두면 3일 뒤 자동 환불되므로 회수에서
            // 다시 잡아 재시도해야 한다.
            NcDebug.LogError($"{Tag} consume failed. {iapResult}");
            ConsumeFailed?.Invoke(iapResult.Code, iapResult.Message);
        }

        // 우리 상품은 전부 소비성이라 승인 경로를 쓰지 않는다. 인터페이스 구현으로만 둔다.
        void IPurchaseCallback.OnAcknowledgeSucceeded(PurchaseData purchase, ProductType type)
        {
            NcDebug.Log($"{Tag} acknowledge succeeded. productId={purchase.ProductId} type={type}");
        }

        void IPurchaseCallback.OnAcknowledgeFailed(IapResult iapResult)
        {
            NcDebug.LogError($"{Tag} acknowledge failed. {iapResult}");
        }

        // 구독 상품이 없으므로 사용하지 않는다.
        void IPurchaseCallback.OnManageRecurringProduct(
            IapResult iapResult,
            PurchaseData purchase,
            RecurringAction action)
        {
            NcDebug.Log($"{Tag} manage recurring product. action={action} {iapResult}");
        }

        void IPurchaseCallback.OnNeedUpdate()
        {
            // 메시지가 아니라 복구 흐름이 필요하다. SDK 최소 버전을 만족하지 못한 상태다.
            NcDebug.LogWarning($"{Tag} store update required. launching update flow.");
            _client.LaunchUpdateOrInstallFlow(result =>
                NcDebug.Log($"{Tag} update flow finished. {result}"));
        }

        void IPurchaseCallback.OnNeedLogin()
        {
            // 로그인도 마찬가지로 복구 흐름이다. 로그인 후 다시 시도해야 결제가 이어진다.
            NcDebug.LogWarning($"{Tag} store login required. launching sign-in flow.");

            _authClient ??= new OneStoreAuthClientImpl();
            _authClient.LaunchSignInFlow(result =>
            {
                NcDebug.Log($"{Tag} sign-in flow finished. {result}");
                if (result.IsSuccessful())
                {
                    QueryUnprocessedPurchases();
                }
            });
        }

#endregion IPurchaseCallback

        private void MarkConnected()
        {
            if (_connected)
            {
                return;
            }

            _connected = true;
            NcDebug.Log($"{Tag} connected.");
            Connected?.Invoke();
        }
    }
}

#endif
