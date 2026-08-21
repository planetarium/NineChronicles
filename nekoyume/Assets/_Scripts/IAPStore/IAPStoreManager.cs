#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using ProductType = UnityEngine.Purchasing.ProductType;

namespace Nekoyume.IAPStore
{
    public class IAPStoreManager : MonoBehaviour, IDetailedStoreListener
    {
        private IStoreController _controller;
        private IExtensionProvider _extensions;

        public IEnumerable<Product> IAPProducts => _controller.products.all;
        public bool IsInitialized { get; private set; }

        private Dictionary<string, InAppPurchaseServiceClient.ProductSchema> _initializedProductSchema = new();
        private IReadOnlyList<InAppPurchaseServiceClient.CategorySchema> _initializedCategorySchema;


        public Dictionary<string, InAppPurchaseServiceClient.ProductSchema> SeasonPassProduct = new();

        private async void Awake()
        {
            try
            {
                var initializationOptions = new InitializationOptions()
                    .SetEnvironmentName("dev");
                await UnityServices.InitializeAsync(initializationOptions);
            }
            catch (Exception exception)
            {
                // An error occurred during services initialization.
                NcDebug.LogException(exception);
            }

            _initializedCategorySchema = await ApiClients.Instance.IAPServiceManager.GetProductsAsync(
                States.Instance.AgentState.address, Game.Game.instance.CurrentPlanetId.ToString());

            if (_initializedCategorySchema is null)
            {
                // TODO: not initialized case handling
                NcDebug.LogError(
                    $"IAPServiceManager.GetProductsAsync({States.Instance.AgentState.address}): Product Catagorys is null.");
                return;
            }

            foreach (var category in _initializedCategorySchema)
            {
                foreach (var product in category.ProductList)
                {
                    _initializedProductSchema.TryAdd(product.Sku(), product);
                }

                if (category.Name == "NoShow")
                {
                    foreach (var product in category.ProductList)
                    {
                        SeasonPassProduct.Add(product.Name, product);
                    }
                }
            }

#if UNITY_EDITOR || RUN_ON_MOBILE
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var schema in _initializedProductSchema.Where(s => s.Value.Active))
            {
                builder.AddProduct(schema.Value.Sku(), ProductType.Consumable);
            }

            UnityPurchasing.Initialize(this, builder);
#endif
        }

        public bool ExistAvailableFreeProduct()
        {
            foreach (var item in _initializedProductSchema)
            {
                if (item.Value.ProductType != InAppPurchaseServiceClient.ProductType.FREE)
                {
                    continue;
                }

                if (!item.Value.Buyable)
                {
                    continue;
                }

                if (item.Value.RequiredLevel == null)
                {
                    return true;
                }

                if (item.Value.RequiredLevel.Value < States.Instance.CurrentAvatarState.level)
                {
                    return true;
                }
            }

            return false;
        }

        public InAppPurchaseServiceClient.ProductSchema GetProductSchema(string sku)
        {
            if (!_initializedProductSchema.TryGetValue(sku, out var result))
            {
                NcDebug.LogError($"ProductSchema not found at first search. sku: {sku}");
                result = _initializedProductSchema
                                .Where(p => p.Value.CheckSku(sku))
                                .Select(p => p.Value)
                                .FirstOrDefault();
                if (result is null)
                {
                    NcDebug.LogError($"ProductSchema not found. sku: {sku}");
                }
            }
            return result;
        }

        public bool CheckCategoryName(string categoryName)
        {
            return _initializedCategorySchema.Any(c => c.Name.ToLower() == categoryName.ToLower());
        }

        public bool TryGetCategoryName(int itemId, out string categoryName)
        {
            var level = States.Instance.CurrentAvatarState.level;
            var categoryInMobileShop = _initializedCategorySchema?
                .Where(c => c.Active && c.Name != "NoShow")
                .OrderBy(c => c.Order)
                .FirstOrDefault(c => c.ProductList
                    .Where(p => p.Active && p.Buyable && (p.RequiredLevel == null || (p.RequiredLevel != null && p.RequiredLevel <= level)))
                    .Any(p => p.FungibleItemList
                        .Any(fi => fi.SheetItemId == itemId)));

            if (categoryInMobileShop == null)
            {
                categoryName = string.Empty;
                return false;
            }

            categoryName = categoryInMobileShop.Name;
            return true;
        }

        public void OnPurchaseClicked(string productId)
        {
            try
            {
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/OnPurchaseClicked",
                    ("product-id", productId),
                    ("agent-address", States.Instance.AgentState.address.ToHex()),
                    ("avatar-address", States.Instance.CurrentAvatarState.address.ToHex()),
                    ("planet-id", Game.Game.instance.CurrentPlanetId.ToString()));
            }
            catch (Exception error)
            {
                NcDebug.LogError("[OnPurchaseClicked] Log Error " + error);
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/OnPurchaseClicked/Error",
                    ("error", error.Message));
            }

            PurchaseLog(productId, "", $"PurchaseOnClicked");
            _controller.InitiatePurchase(productId);
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        void IStoreListener.OnInitialized(IStoreController controller,
            IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            NcDebug.Log("IAP Store Manager Initialized successfully!");
            foreach (var product in _controller.products.all)
            {
                NcDebug.Log(
                    $"{product.definition.id}: {product.metadata.localizedTitle}, {product.metadata.localizedDescription}, {product.metadata.localizedPriceString}");
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            ((IStoreListener)this).OnInitializeFailed(error, string.Empty);
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            NcDebug.LogError($"Initializing failed for the specified reason: {error}\n{message}");
        }


        [Serializable] private struct PurchaseReciept
        {
            public string Receipt;
            public string AgentAddressHex;
            public string AvatarAddressHex;
            public string PlanetId;
        }

        [Serializable]
        public class LocalTransactionsWrapper
        {
            public List<string> data;
        }

        private const string LOCAL_TRANSACTIONS = "LOCAL_TRANSACTIONS";

        private List<string> GetLocalTransactions()
        {
            var listString = PlayerPrefs.GetString(LOCAL_TRANSACTIONS, string.Empty);
            if (string.IsNullOrEmpty(listString))
            {
                return new List<string>();
            }

            return JsonUtility.FromJson<LocalTransactionsWrapper>(listString).data;
        }

        private void AddLocalTransactions(string transaction)
        {
            var transactionList = GetLocalTransactions();
            if (transactionList.Contains(transaction))
            {
                NcDebug.LogWarning($"[AddLocalTransactions] duplicate Transaction {transaction}");
            }
            else
            {
                transactionList.Add(transaction);
                PlayerPrefs.SetString(LOCAL_TRANSACTIONS, JsonUtility.ToJson(new LocalTransactionsWrapper { data = transactionList }));
            }
        }

        private void RemoveLocalTransactions(string transaction)
        {
            var transactionList = GetLocalTransactions();
            if (transactionList.Remove(transaction))
            {
                PlayerPrefs.SetString(LOCAL_TRANSACTIONS, JsonUtility.ToJson(new LocalTransactionsWrapper { data = transactionList }));
            }
        }

        private async void PurchaseLog(string productId, string orderId, string data)
        {
            var states = States.Instance;
            try
            {
                var result = await ApiClients.Instance.IAPServiceManager
                    .PurchaseLogAsync(
                        states?.AgentState?.address.ToHex(),
                        states?.CurrentAvatarState?.address.ToHex(),
                        Game.Game.instance?.CurrentPlanetId?.ToString(),
                        productId,
                        orderId,
                        data);

                NcDebug.Log("[PurchaseLog] Log " + result);
            }
            catch (Exception error)
            {
                NcDebug.LogError("[PurchaseLog] Log Error " + error);
            }
        }

        /// <summary>
        /// Tx정보만 남아 있는 경우 구매처리
        /// </summary>
        /// <param name="product"></param>
        // 배송이 실제로 끝났는가. 서버 /purchase/request 는 같은 order_id 의 영수증이 이미
        //   있으면 **상태와 무관하게** 200 + 그 영수증을 돌려준다(purchase.py 의 prev_receipt
        //   early-return). 그래서 result != null 만 보고 확정하면, 첫 시도가 INVALID /
        //   PURCHASE_LIMIT_EXCEED 등으로 실패한 결제가 두 번째 시도에서 200 을 받아
        //   consume/acknowledge 되어버린다 — 배송도 환불도 못 받는 상태로 굳는다.
        //   확정은 VALID 일 때만 한다. 아니면 Pending 을 유지해 스토어 환불로 흘려보낸다.
        //   판정은 서버와 맞춘다. 서버(#481)는 `tx_status is not None` 이면 "배송 디스패치됨"
        //   으로 보고 status 를 더 보지 않는다 — status = VALID 는 유일한 commit 보다 훨씬
        //   앞에서 메모리에만 세팅되고 그 사이에 배송이 실행되므로, 배송 후 죽으면 DB 에
        //   INIT 이 남는다. 그래서 tx 가 있으면 status 를 신뢰하지 않는다.
        //   시즌패스류는 tx_status 가 영구 NULL 이라 status == VALID 로 판정한다.
        private static bool IsDelivered(InAppPurchaseServiceClient.ReceiptDetailSchema result)
        {
            if (result == null)
            {
                return false;
            }

            return result.TxStatus != null
                   || result.Status == InAppPurchaseServiceClient.ReceiptStatus.VALID;
        }

        // 체인 상태(AgentState/CurrentAvatarState/PlanetId)가 준비될 때까지 기다린 뒤
        //   정상 구매 요청 경로로 보낸다. 결제는 이미 끝났고 스토어는 Pending 으로 들고 있으므로
        //   여기서 놓치면 배송이 사라진다 — 타임아웃 시에도 Pending 을 유지해
        //   다음 기동의 재전달에 맡기고, 무슨 일이 있었는지는 남긴다.
        private async void DeferPurchaseUntilStateReadyAsync(Product product)
        {
            var timeout = TimeSpan.FromSeconds(StateWaitTimeoutSeconds);
            var cts = new System.Threading.CancellationTokenSource(timeout);
            try
            {
                await UniTask.WaitUntil(
                    () => States.Instance?.AgentState?.address != null
                          && States.Instance?.CurrentAvatarState?.address != null
                          && Game.Game.instance?.CurrentPlanetId != null,
                    cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                NcDebug.LogError(
                    $"[DeferPurchaseUntilStateReadyAsync] Timeout({timeout.TotalSeconds}s). tx: {product.transactionID}");
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/ProcessPurchase/StateWaitTimeout",
                    ("product-id", product.definition.id),
                    ("transaction-id", product.transactionID));
                return;
            }
            finally
            {
                cts.Dispose();
            }

            NcDebug.Log($"[DeferPurchaseUntilStateReadyAsync] State ready. tx: {product.transactionID}");
            RePurchaseTryAsync(product);
        }

        private async void OnlyTxRetryPurchaseAsync(Product product)
        {
            var result = await ApiClients.Instance.IAPServiceManager
                .PurchaseRetryAsync(
                    product.receipt,
                    product.transactionID,
                    product.appleOriginalTransactionID);

            if (!IsDelivered(result))
            {
                NcDebug.LogError($"[OnlyTxRetryPurchaseAsync] Not delivered (status: {result?.Status.ToString() ?? "null"})");
            }
            else
            {
                _controller.ConfirmPendingPurchase(product);
                RemoveLocalTransactions(product.transactionID);
            }
        }

        // 로그인/상태 초기화가 늦는 기기를 감안한 상한. 넘으면 다음 기동 재전달에 맡긴다.
        private const int StateWaitTimeoutSeconds = 60;

        private async void RePurchaseTryAsync(Product product)
        {
            var purchaseData = PlayerPrefs.GetString("PURCHASE_TX_" + product.transactionID, string.Empty);
            PurchaseReciept pData;
            var states = States.Instance;
            if (string.IsNullOrEmpty(purchaseData))
            {
                pData = new PurchaseReciept
                {
                    Receipt = product.receipt,
                    AgentAddressHex = states?.AgentState?.address.ToHex(),
                    AvatarAddressHex = states?.CurrentAvatarState?.address.ToHex(),
                    PlanetId = Game.Game.instance?.CurrentPlanetId?.ToString()
                };
            }
            else
            {
                pData = JsonUtility.FromJson<PurchaseReciept>(purchaseData);
            }

            if (string.IsNullOrEmpty(pData.AgentAddressHex))
            {
                pData.AgentAddressHex = states?.AgentState?.address.ToHex();
            }

            if (string.IsNullOrEmpty(pData.AvatarAddressHex))
            {
                pData.AvatarAddressHex = states?.CurrentAvatarState?.address.ToHex();
            }

            if (string.IsNullOrEmpty(pData.PlanetId))
            {
                pData.PlanetId = Game.Game.instance?.CurrentPlanetId?.ToString();
            }

            // 주소/행성 중 하나라도 비면 보내지 않는다. 서버는 agentAddress 만 null 검사하므로
            //   avatar_addr="" 로 영수증이 만들어지고 검증까지 통과해(status=VALID) 클라가
            //   확정해버린다 — 아무도 못 받는 결제가 확정된다. 상태를 기다렸다가 다시 온다.
            if (string.IsNullOrEmpty(pData.AgentAddressHex)
                || string.IsNullOrEmpty(pData.AvatarAddressHex)
                || string.IsNullOrEmpty(pData.PlanetId))
            {
                NcDebug.LogWarning(
                    $"[RePurchaseTryAsync] Incomplete owner data, deferring. tx: {product.transactionID} "
                    + $"agent: {pData.AgentAddressHex} avatar: {pData.AvatarAddressHex} planet: {pData.PlanetId}");
                DeferPurchaseUntilStateReadyAsync(product);
                return;
            }

            var result = await ApiClients.Instance.IAPServiceManager
                .PurchaseRequestAsync(
                    product.receipt,
                    pData.AgentAddressHex != null ? pData.AgentAddressHex : string.Empty,
                    pData.AvatarAddressHex != null ? pData.AvatarAddressHex : string.Empty,
                    pData.PlanetId != null ? pData.PlanetId : string.Empty,
                    product.transactionID,
                    product.appleOriginalTransactionID);

            if (!IsDelivered(result))
            {
                NcDebug.LogError($"[RePurchaseTryAsync] Not delivered (status: {result?.Status.ToString() ?? "null"}) {pData.Receipt} AgentAddressHex: {pData.AgentAddressHex} AvatarAddressHex: {pData.AvatarAddressHex} PlanetId: {pData.PlanetId}");
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/RePurchaseTry/NotDelivered",
                    ("product-id", product.definition.id),
                    ("transaction-id", product.transactionID),
                    ("status", result?.Status.ToString() ?? "null"));
            }
            else
            {
                NcDebug.Log($"[RePurchaseTryAsync] Delivered. agent: {pData.AgentAddressHex} avatar: {pData.AvatarAddressHex}");
                _controller.ConfirmPendingPurchase(product);
                RemoveLocalTransactions(product.transactionID);
            }
        }

        /// <summary>
        /// Called when a purchase completes.
        /// May be called at any time after OnInitialized().
        /// </summary>
        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            try
            {
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/ProcessPurchase",
                    ("product-id", e.purchasedProduct.definition.id),
                    ("transaction-id", e.purchasedProduct.transactionID),
                    ("agent-address", States.Instance?.AgentState?.address.ToHex()),
                    ("avatar-address", States.Instance?.CurrentAvatarState?.address.ToHex()),
                    ("planet-id", Game.Game.instance?.CurrentPlanetId?.ToString()));
            }
            catch (Exception error)
            {
                NcDebug.LogError("[ProcessPurchase] Log Error " + error);
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/ProcessPurchase/Error",
                    ("error", error.Message));
            }

            PurchaseLog(e.purchasedProduct.definition.id, e.purchasedProduct.transactionID, "PurchaseSuccess");

            if (e == null)
            {
                NcDebug.Log("[ProcessPurchase] PurchaseEventArgs is null");
                return PurchaseProcessingResult.Pending;
            }

            var existTxInfo = false;
            try
            {
                var states = States.Instance;
                existTxInfo = PlayerPrefs.HasKey("PURCHASE_TX_" + e.purchasedProduct.transactionID);
                if (!existTxInfo)
                {
                    //로컬에 트랜잭션 정보가 없는데 체인정보가 아직 초기화되지않은경우 영수증만 남은경우이므로 영수증 정보만을 가지고 구매처리 시도.
                    if (states?.AgentState?.address == null
                        || states?.CurrentAvatarState?.address == null
                        || Game.Game.instance?.CurrentPlanetId == null)
                    {
                        NcDebug.Log($"[ProcessPurchase] AgentState{states?.AgentState?.address.ToHex()}, AvatarState{states?.CurrentAvatarState?.address.ToHex()} or PlanetId{Game.Game.instance?.CurrentPlanetId.ToString()} is null");
                        // 예전에는 여기서 바로 OnlyTxRetryPurchaseAsync(=/purchase/retry)를 불렀다.
                        //   retry 는 order_id 로 기존 영수증을 찾는 API 라, 아직 /request 가 한 번도
                        //   가지 않은 최초 배송에서는 "Receipt not found" 로 영구 실패한다.
                        //   상태가 준비되기를 기다렸다가 정상 경로(/request)로 보낸다.
                        DeferPurchaseUntilStateReadyAsync(e.purchasedProduct);
                        return PurchaseProcessingResult.Pending;
                    }

                    var purchaseReciepe = new PurchaseReciept
                    {
                        Receipt = e.purchasedProduct.receipt,
                        AgentAddressHex = states.AgentState.address.ToHex(),
                        AvatarAddressHex = states.CurrentAvatarState.address.ToHex(),
                        PlanetId = Game.Game.instance.CurrentPlanetId.ToString()
                    };
                    PlayerPrefs.SetString("PURCHASE_TX_" + e.purchasedProduct.transactionID, JsonUtility.ToJson(purchaseReciepe));
                    AddLocalTransactions(e.purchasedProduct.transactionID);
                }
            }
            catch (Exception error)
            {
                NcDebug.LogError("[ProcessPurchase] AddLocalTransactions Error " + error);
            }

            try
            {
                //로컬에 트랜잭션 정보가 있는채로 동일한 재품 구매시도이므로 로컬정보를 가지고 구매처리 재시도.
                if (existTxInfo)
                {
                    NcDebug.Log("[ProcessPurchase] Is not PurchasePage");
                    RePurchaseTryAsync(e.purchasedProduct);
                    return PurchaseProcessingResult.Pending;
                }
            }
            catch (Exception error)
            {
                NcDebug.LogError("[ProcessPurchase] RePurchaseTryAsync Error " + error);
            }

            try
            {
                // availableToPurchase 로 배송 여부를 가르지 않는다.
                //   이 플래그는 "지금 구매를 시도할 수 있는가"(카탈로그에 있는가)를 뜻하며
                //   ProcessPurchase 는 이미 결제가 끝난 뒤 호출되므로 판단 근거가 아니다.
                //   게이트는 IAP 4.9.3 시절에 들어왔고 그때는 카탈로그 인스턴스가 그대로
                //   전달돼 true 였다. 5.0.4 부터 Google 경로가 Product 를 복제하면서
                //   플래그를 잃어(항상 false) 안드로이드 결제가 전부 막혔다.
                //   영수증 검증은 서버가 한다 — 서버가 모르는 상품이면 서버가 거절한다.
                if (!e.purchasedProduct.availableToPurchase)
                {
                    NcDebug.LogWarning(
                        $"[ProcessPurchase] availableToPurchase=false but proceeding. product: {e.purchasedProduct.definition.id}");
                }

                OnPurchaseRequestAsync(e);
                return PurchaseProcessingResult.Pending;
            }
            catch (Exception error)
            {
                NcDebug.LogError("[ProcessPurchase] " + error);
                return PurchaseProcessingResult.Pending;
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// IStoreListener.OnPurchaseFailed is deprecated,
        /// use IDetailedStoreListener.OnPurchaseFailed instead.
        /// </summary>
        void IStoreListener.OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            NcDebug.LogError($"[IStoreListener PurchaseFail] reason: {p}, Product: {i.metadata.localizedTitle}");
            PurchaseLog(i.definition.id, i.transactionID, $"PurchaseFailed[{p}]");
            if (p == PurchaseFailureReason.PurchasingUnavailable)
            {
                // IAP may be disabled in device settings.
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        void IDetailedStoreListener.OnPurchaseFailed(Product i, PurchaseFailureDescription p)
        {
            NcDebug.LogError($"[IDetailedStoreListener PurchaseFail] reason: {p.reason}, Product: {i.metadata.localizedTitle}");
            PurchaseLog(i.definition.id, i.transactionID, $"PurchaseFailed[{p.reason}][{p.message}]");
            Analyzer.Instance.Track(
                "Unity/Shop/IAP/PurchaseResult",
                ("product-id", i.definition.id),
                ("result", p.reason.ToString()),
                ("message", p.message.ToString()));

            Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
            Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();

            switch (p.reason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    break;
                case PurchaseFailureReason.ExistingPurchasePending:
                    break;
                case PurchaseFailureReason.ProductUnavailable:
                    break;
                case PurchaseFailureReason.SignatureInvalid:
                    break;
                case PurchaseFailureReason.UserCancelled:
                    break;
                case PurchaseFailureReason.PaymentDeclined:
                    break;
                case PurchaseFailureReason.DuplicateTransaction:
                    break;
                case PurchaseFailureReason.Unknown:
                    break;
                default:
                    break;
            }
        }

        public async UniTaskVoid OnPurchaseFreeAsync(string sku)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;
            try
            {
                var result = await ApiClients.Instance.IAPServiceManager.PurchaseFreeAsync(
                    states.AgentState.address.ToHex(),
                    states.CurrentAvatarState.address.ToHex(),
                    Game.Game.instance.CurrentPlanetId.ToString(),
                    sku);

                Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();

                if (result is null)
                {
                    popup.Show(
                        "UI_ERROR",
                        "UI_IAP_PURCHASE_FAILED",
                        "UI_OK",
                        true);
                }
                else
                {
                    Widget.Find<MobileShop>()?.PurchaseComplete(sku);

                    PurchaseCountRefresh(sku);

                    if (_initializedProductSchema.TryGetValue(sku, out var product) && product.Mileage > 0)
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

                    Widget.Find<MobileShop>()?.RefreshGrid();
                    Widget.Find<ShopListPopup>()?.Close();
                }
            }
            catch (Exception exc)
            {
                Widget.Find<MobileShop>()?.RefreshGrid();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<IconAndButtonSystem>().Show("UI_ERROR", exc.Message, localize: false);
            }
            return;
        }

        public async UniTaskVoid OnPurchaseMileageAsync(string sku)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;
            try
            {
                var result = await ApiClients.Instance.IAPServiceManager.PurchaseMileageAsync(
                    states.AgentState.address.ToHex(),
                    states.CurrentAvatarState.address.ToHex(),
                    Game.Game.instance.CurrentPlanetId.ToString(),
                    sku);

                Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();

                if (result is null)
                {
                    popup.Show(
                        "UI_ERROR",
                        "UI_IAP_PURCHASE_FAILED",
                        "UI_OK",
                        true);
                }
                else
                {
                    Widget.Find<MobileShop>()?.PurchaseComplete(sku);
                    PurchaseCountRefresh(sku);

                    if(_initializedProductSchema.TryGetValue(sku, out var product) && product.Mileage > 0)
                    {
                        popup.Show(
                            "UI_COMPLETED",
                            "UI_IAP_PURCHASE_WITH_MILEAGE_COMPLETE",
                            "UI_OK",
                            true,
                            IconAndButtonSystem.SystemType.Information,
                            product?.Mileage);
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

                    Widget.Find<MobileShop>()?.RefreshGrid();
                    Widget.Find<ShopListPopup>()?.Close();
                }
            }
            catch (Exception exc)
            {
                Widget.Find<MobileShop>()?.RefreshGrid();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<IconAndButtonSystem>().Show("UI_ERROR", exc.Message, localize: false);
            }
            return;
        }

        private void PurchaseCountRefresh(string sku)
        {
            if (_initializedProductSchema.TryGetValue(sku, out var p))
            {
                p.PurchaseCount++;
                if (p.DailyLimit != null)
                {
                    p.Buyable = p.PurchaseCount < p.DailyLimit.Value;
                }
                else if (p.WeeklyLimit != null)
                {
                    p.Buyable = p.PurchaseCount < p.WeeklyLimit.Value;
                }
            }
        }

        private async void OnPurchaseRequestAsync(PurchaseEventArgs e)
        {
            var popup = Widget.Find<IconAndButtonSystem>();
            var states = States.Instance;

            try
            {
                var result = await ApiClients.Instance.IAPServiceManager
                    .PurchaseRequestAsync(
                        e.purchasedProduct.receipt,
                        states.AgentState.address.ToHex(),
                        states.CurrentAvatarState.address.ToHex(),
                        Game.Game.instance.CurrentPlanetId.ToString(),
                        e.purchasedProduct.transactionID,
                        e.purchasedProduct.appleOriginalTransactionID);

                Widget.Find<ShopListPopup>()?.PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>()?.PurchaseButtonLoadingEnd();

                // 배송 완료(VALID)가 아니면 확정하지 않는다 — IsDelivered 주석 참조.
                if (!IsDelivered(result))
                {
                    NcDebug.LogError(
                        $"[OnPurchaseRequestAsync] Not delivered (status: {result?.Status.ToString() ?? "null"}) tx: {e.purchasedProduct.transactionID}");
                    Analyzer.Instance.Track(
                        "Unity/Shop/IAP/PurchaseResult",
                        ("product-id", e.purchasedProduct.definition.id),
                        ("result", "NotDelivered"),
                        ("status", result?.Status.ToString() ?? "null"));
                    popup.Show(
                        "UI_ERROR",
                        "UI_IAP_PURCHASE_FAILED",
                        "UI_OK",
                        true);
                }
                else
                {
                    try
                    {
                        Widget.Find<MobileShop>()?.PurchaseComplete(e.purchasedProduct.definition.id);
                        PurchaseCountRefresh(e.purchasedProduct.definition.id);
                        Analyzer.Instance.Track(
                            "Unity/Shop/IAP/PurchaseResult",
                            ("product-id", e.purchasedProduct.definition.id),
                            ("result", "Complete"),
                            ("transaction-id", e.purchasedProduct.transactionID));

                        if (_initializedProductSchema.TryGetValue(e.purchasedProduct.definition.id, out var product) && product.Mileage > 0)
                        {
                            popup.Show(
                                "UI_COMPLETED",
                                "UI_IAP_PURCHASE_WITH_MILEAGE_COMPLETE",
                                "UI_OK",
                                true,
                                IconAndButtonSystem.SystemType.Information,
                                product?.Mileage);
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

                        Widget.Find<MobileShop>()?.RefreshGrid();
                        Widget.Find<ShopListPopup>()?.Close();
                        _controller.ConfirmPendingPurchase(e.purchasedProduct);
                        RemoveLocalTransactions(e.purchasedProduct.transactionID);
                    }
                    catch (Exception error)
                    {
                        NcDebug.LogError("[OnPurchaseRequestAsync] Log Error " + error);
                        _controller.ConfirmPendingPurchase(e.purchasedProduct);
                        RemoveLocalTransactions(e.purchasedProduct.transactionID);
                    }
                }
            }
            catch (Exception exc)
            {
                Widget.Find<MobileShop>()?.RefreshGrid();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<IconAndButtonSystem>().Show(L10nManager.Localize("UI_ERROR"), exc.Message, L10nManager.Localize("UI_OK"), false);
            }
        }
    }
}
