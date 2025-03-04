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
        private async void OnlyTxRetryPurchaseAsync(Product product)
        {
            var result = await ApiClients.Instance.IAPServiceManager
                .PurchaseRetryAsync(
                    product.receipt,
                    product.transactionID,
                    product.appleOriginalTransactionID);

            if (result is null)
            {
                NcDebug.LogError($"[OnlyTxRetryPurchaseAsync] Failed");
            }
            else
            {
                _controller.ConfirmPendingPurchase(product);
                RemoveLocalTransactions(product.transactionID);
            }
        }

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

            if (string.IsNullOrEmpty(pData.AgentAddressHex))
            {
                pData.AvatarAddressHex = states?.CurrentAvatarState?.address.ToHex();
            }

            if (string.IsNullOrEmpty(pData.PlanetId))
            {
                pData.PlanetId = Game.Game.instance?.CurrentPlanetId?.ToString();
            }

            var result = await ApiClients.Instance.IAPServiceManager
                .PurchaseRequestAsync(
                    product.receipt,
                    pData.AgentAddressHex != null ? pData.AgentAddressHex : string.Empty,
                    pData.AvatarAddressHex != null ? pData.AvatarAddressHex : string.Empty,
                    pData.PlanetId != null ? pData.PlanetId : string.Empty,
                    product.transactionID,
                    product.appleOriginalTransactionID);

            if (result is null)
            {
                NcDebug.LogError($"[RePurchaseTryAsync] Failed {pData.Receipt} AgentAddressHex: {pData.AgentAddressHex} AvatarAddressHex: {pData.AvatarAddressHex} PlanetId: {pData.PlanetId}");
            }
            else
            {
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
                        OnlyTxRetryPurchaseAsync(e.purchasedProduct);
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
                if (e.purchasedProduct.availableToPurchase)
                {
                    OnPurchaseRequestAsync(e);
                    return PurchaseProcessingResult.Pending;
                }

                Widget.Find<ShopListPopup>().PurchaseButtonLoadingEnd();
                Widget.Find<SeasonPassPremiumPopup>().PurchaseButtonLoadingEnd();
                NcDebug.LogWarning($"not availableToPurchase. e.purchasedProduct.availableToPurchase: {e.purchasedProduct.availableToPurchase}");
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
                ("product-id", p.productId),
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
