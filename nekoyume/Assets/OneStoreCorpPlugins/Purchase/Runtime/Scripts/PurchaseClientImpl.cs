
#if UNITY_ANDROID || !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OneStore.Purchasing.Internal;
using OneStore.Common;
using Logger = OneStore.Common.OneStoreLogger;
using UnityEngine;

namespace OneStore.Purchasing
{
    /// <summary>
    /// Implementation of the One Store IAP client for Unity.
    /// This class handles purchase initialization, product queries, purchase flows, and subscription management.
    /// </summary>
    public class PurchaseClientImpl : IPurchaseExtensions
    {
        private IPurchaseCallback _callback;
        private AndroidJavaObject _purchaseClient;

        private PurchasesUpdatedListener _purchaseUpdatedListener;

        private readonly OneStorePurchasingInventory _inventory;

        private volatile string _productInPurchaseFlow;

        private readonly string _licenseKey;

        public string StoreCode { get; private set; }

        private Queue<Action> _requestQueue = new Queue<Action>();

        private volatile ConnectionStatus _connectionStatus = ConnectionStatus.DISCONNECTED;

        /// <summary>
        /// Enum representing the connection status of the purchase client.
        /// </summary>
        private enum ConnectionStatus {
            DISCONNECTED, CONNECTING, CONNECTED,
        }

        /// <summary>
        /// Dictionary to track query purchase status for different product types.
        /// </summary>
        private volatile Dictionary<ProductType, AsyncRequestStatus> _queryPurchasesCallStatus =
            new Dictionary<ProductType, AsyncRequestStatus>
            {
                {ProductType.INAPP, AsyncRequestStatus.Succeed},
                {ProductType.SUBS, AsyncRequestStatus.Succeed},
                {ProductType.AUTO, AsyncRequestStatus.Succeed},
            };

        /// <summary>
        /// Dictionary to track query product details status for different product types.
        /// </summary>
        private volatile Dictionary<ProductType, AsyncRequestStatus> _queryProductDetailsCallStatus =
            new Dictionary<ProductType, AsyncRequestStatus>
            {
                {ProductType.INAPP, AsyncRequestStatus.Succeed},
                {ProductType.SUBS, AsyncRequestStatus.Succeed},
                {ProductType.AUTO, AsyncRequestStatus.Succeed},
                {ProductType.ALL, AsyncRequestStatus.Succeed},
            };

        /// <summary>
        /// Enum representing the asynchronous request status.
        /// </summary>
        private enum AsyncRequestStatus
        {
            Pending, Failed, Succeed,
        }

        /// <summary>
        /// Initializes a new instance of the `PurchaseClientImpl` class with the specified license key.
        /// </summary>
        /// <param name="licenseKey">The license key required for authentication with the ONE store IAP SDK.</param>
        /// <remarks>
        /// - Checks if the application is running on an Android platform; otherwise, throws a `PlatformNotSupportedException`.  
        /// - Initializes `_inventory` as an instance of `OneStorePurchasingInventory` to manage purchase data.  
        /// - Stores the provided `licenseKey` in `_licenseKey` for later use.  
        /// </remarks>
        public PurchaseClientImpl(string licenseKey)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new PlatformNotSupportedException("Operation is not supported on this platform.");
            }

            _inventory = new OneStorePurchasingInventory();
            _licenseKey = licenseKey;
        }

        /// <summary>
        /// Initializes the ONE store IAP client and establishes a connection to the service.
        /// </summary>
        /// <param name="callback">The callback interface to handle purchase-related events.</param>
        /// <remarks>
        /// - Assigns the provided `callback` to `_callback`.  
        /// - Creates an instance of `_purchaseClient` using the ONE store IAP SDK.  
        /// - Retrieves the application context using `JniHelper.GetApplicationContext()`.  
        /// - Initializes `_purchaseUpdatedListener` to handle purchase updates and assigns `ProcessPurchaseUpdatedResult` as the callback.  
        /// - Calls `_purchaseClient.Call()` to set up the purchase client with the application context, license key, and purchase listener.  
        /// - Initiates the service connection using `StartConnection()`, and upon successful connection, retrieves the `storeCode`.  
        /// </remarks>
        public void Initialize(IPurchaseCallback callback)
        {
            _callback = callback;
            _purchaseClient = new AndroidJavaObject(Constants.PurchaseClient, Constants.Version);

            var context = JniHelper.GetApplicationContext();
            _purchaseUpdatedListener = new PurchasesUpdatedListener();
            _purchaseUpdatedListener.OnPurchasesUpdated += ProcessPurchaseUpdatedResult;

            _purchaseClient.Call(
                Constants.PurchasseClientSetupMethod,
                context,
                _licenseKey,
                _purchaseUpdatedListener
            );

            StartConnection(()=> {
                Logger.Log("Initialize: Successfully connected to the service.");
                GetStoreCode();
            });
        }

        /// <summary>
        /// Initiates a connection to the ONE store IAP service and processes pending requests.
        /// </summary>
        /// <param name="action">The action to execute once the connection is successfully established.</param>
        /// <remarks>
        /// - If the client is already connecting (`ConnectionStatus.CONNECTING`), the action is added to `_requestQueue` and logged.  
        /// - Updates `_connectionStatus` to `CONNECTING` before starting the connection process.  
        /// - Creates a `PurchaseClientStateListener` to handle connection events:  
        ///   - `OnServiceDisconnected`: Logs a warning, resets `_productInPurchaseFlow`, and sets `_connectionStatus` to `DISCONNECTED`.  
        ///   - `OnSetupFinished`:  
        ///     - If successful, updates `_connectionStatus` to `CONNECTED`, executes the provided action, and processes queued requests.  
        ///     - If failed, logs an error, clears `_requestQueue`, and triggers `OnSetupFailed` callback with the error result.  
        /// - Calls `_purchaseClient.Call()` to start the connection process.  
        /// </remarks>
        private void StartConnection(Action action)
        {
            if (_connectionStatus == ConnectionStatus.CONNECTING) {
                _requestQueue.Enqueue(action);
                Logger.Log("Client is already in the process of connecting to service.");
                return;
            }

            _connectionStatus = ConnectionStatus.CONNECTING;

            var purchaseClientStateListener = new PurchaseClientStateListener();
            purchaseClientStateListener.OnServiceDisconnected += () =>
            {
                Logger.Warning("Client is disconnected from the service.");
                _productInPurchaseFlow = null;
                _connectionStatus = ConnectionStatus.DISCONNECTED;
            };

            purchaseClientStateListener.OnSetupFinished += (javaIapResult) =>
            {
                var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
                if (iapResult.IsSuccessful())
                {
                    _connectionStatus = ConnectionStatus.CONNECTED;
                    action?.Invoke();
                    
                    while (_requestQueue.Count > 0) {
                        _requestQueue.Dequeue()?.Invoke();
                    }
                }
                else
                {
                    Logger.Error("Failed to connect to service with error code '{0}' and message: '{1}'.",
                        iapResult.Code, iapResult.Message);

                    _requestQueue.Clear();
                    HandleErrorCode(iapResult, () => {
                        RunOnMainThread(() => _callback.OnSetupFailed(iapResult));
                    });
                }
            };
            
            _purchaseClient.Call(
                Constants.PurchaseClientStartConnectionMethod,
                purchaseClientStateListener
            );
        }

        /// <summary>
        /// Executes the given action if the IAP service is connected; otherwise, attempts to establish a connection first.
        /// </summary>
        /// <param name="action">The action to execute once the connection is established.</param>
        /// <remarks>
        /// - If `_connectionStatus` is `CONNECTED`, the provided action is immediately executed.  
        /// - If not connected, `StartConnection(action)` is called to establish a connection before executing the action.
        /// </remarks>
        private void ExecuteService(Action action)
        {
            if (_connectionStatus == ConnectionStatus.CONNECTED)
            {
                action?.Invoke();
            }
            else
            {
                StartConnection(action);
            }
        }

        /// <summary>
        /// Ends the connection to the One Store purchase service.
        /// This should be called when the application exits or the purchasing service is no longer needed.
        /// </summary>
        public void EndConnection()
        {        
            _productInPurchaseFlow = null;
            if (!IsPurchasingServiceAvailable()) return;
            _purchaseClient.Call(Constants.PurchaseClientEndConnectionMethod);
            _connectionStatus = ConnectionStatus.DISCONNECTED;
        }

        /// <summary>
        /// Retrieves detailed product information for the given product IDs using the ONE store IAP SDK.
        /// </summary>
        /// <param name="productIds">A collection of product IDs to query.</param>
        /// <param name="type">The type of product to query.</param>
        /// <remarks>
        /// - If there is an ongoing request for the given product type, the function returns without executing a new request.
        /// - If product details are already available in the local inventory and match the requested product IDs, 
        ///   the details are returned immediately without making a new request.
        /// - Otherwise, a request is constructed using `ProductDetailsParamBuilder` and sent through `_purchaseClient.Call()`.
        /// - The result is handled by `ProductDetailsListener`, which triggers the appropriate callback upon response.
        /// </remarks>
        public void QueryProductDetails(ReadOnlyCollection<string> productIds, ProductType type)
        {
            if (_queryProductDetailsCallStatus[type] == AsyncRequestStatus.Pending)
            {
                return;
            }

            _queryProductDetailsCallStatus[type] = AsyncRequestStatus.Pending;

            ExecuteService(() => {
                var details = _inventory.GetProductDetails(productIds);
                if (details.Count == productIds.Count)
                {
                    _queryProductDetailsCallStatus[type] = AsyncRequestStatus.Succeed;
                    RunOnMainThread(() => _callback.OnProductDetailsSucceeded(details));
                    return;
                }

                var productDetailsParamsBuilder = new AndroidJavaObject(Constants.ProductDetailsParamBuilder);
                productDetailsParamsBuilder.Call<AndroidJavaObject>(Constants.ProductDetailsParamBuilderSetProductIdListMethod,
                    JniHelper.CreateJavaArrayList(productIds.ToArray()));
                productDetailsParamsBuilder.Call<AndroidJavaObject>(Constants.ProductDetailParamBuilderSetProductTypeMethod,
                    type.ToString());

                var productDetailsParams = productDetailsParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod);

                var productDetailsListener = new ProductDetailsListener(type);
                productDetailsListener.OnProductDetailsResponse += ProcessProductDetailsResult;

                _purchaseClient.Call(
                    Constants.PurchaseClientQueryProductDetailsMethod,
                    productDetailsParams,
                    productDetailsListener
                );
            });
        }

        public Dictionary<string, string> GetProductJSONDictionary()
        {
            return IsPurchasingServiceAvailable() ? _inventory.GetAllProductDetails() : null;
        }

        /// <summary>
        /// Queries the non-consumed purchase history for managed products using the ONE store IAP SDK.
        /// </summary>
        /// <param name="type">The type of product to query. Must not be `ProductType.ALL` or `null`.</param>
        /// <remarks>
        /// - If `ProductType.ALL` is passed, an error is logged, and the request is rejected, as querying all product types is not supported.
        /// - If `type` is `null`, an error is returned indicating an illegal argument.
        /// - If there is an ongoing query for the given product type, the function returns without executing another request.
        /// - Otherwise, the function sets the request status to `Pending`, then initiates the query using `_purchaseClient.Call()`.
        /// </remarks>
        public void QueryPurchases(ProductType type)
        {
            if (ProductType.ALL == type)
            {
                var message = "ProductType.ALL is not supported. This is supported only by the QueryProductDetails.";
                Logger.Error(message);
                RunOnMainThread(() => _callback.OnPurchaseFailed(new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message)));
                return;
            }
            else if (type == null)
            {
                var message = "ProductType is null";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnPurchaseFailed(iapResult));
                return;
            }
            else if (_queryPurchasesCallStatus[type] == AsyncRequestStatus.Pending)
            {
                return;
            }

            _queryPurchasesCallStatus[type] = AsyncRequestStatus.Pending;

            ExecuteService(() => {
                var queryPurchasesListener = new QueryPurchasesListener(type);
                queryPurchasesListener.OnPurchasesResponse += ProcessQueryPurchasesResult;
                _purchaseClient.Call(
                    Constants.PurchaseClientQueryPurchasesMethod,
                    type.ToString(),
                    queryPurchasesListener
                );
            });
        }

        /// <summary>
        /// Initiates a purchase request for a product using the ONE store IAP SDK.
        /// </summary>
        /// <param name="purchaseFlowParams">The parameters required to process the purchase, including product ID, type, quantity, and additional metadata.</param>
        /// <remarks>
        /// - If a purchase is already in progress for another product, the request is rejected, and an error is logged.
        /// - The function constructs a `PurchaseFlowParams` object using the provided parameters.
        /// - The purchase flow is initiated using the `LaunchPurchaseFlow` method.
        /// - The request includes additional metadata such as `ProductName`, `GameUserId`, and whether the product is eligible for promotions.
        /// </remarks>
        public void Purchase(PurchaseFlowParams purchaseFlowParams)
        {
            if (_productInPurchaseFlow != null)
            {
                var message = string.Format("A purchase for {0} is already in progress.", _productInPurchaseFlow);
                Logger.Error(message);
                RunOnMainThread(() => _callback.OnPurchaseFailed(new IapResult((int) ResponseCode.RESULT_ERROR, message)));
                return;
            }

            ExecuteService(() => {
                _productInPurchaseFlow = purchaseFlowParams.ProductId;

                using (var purchaseFlowParamsBuilder = new AndroidJavaObject(Constants.PurchaseFlowParamsBuilder))
                {
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductIdMethod, purchaseFlowParams.ProductId);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductTypeMethod, purchaseFlowParams.ProductType);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetQuantityMethod, purchaseFlowParams.Quantity);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetDeveloperPayloadMethod, purchaseFlowParams.DeveloperPayload);
                    
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductNameMethod, purchaseFlowParams.ProductName);
                    
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetGameUserIdMethod, purchaseFlowParams.GameUserId);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetPromotionApplicableMethod, purchaseFlowParams.PromotinApplicable);
                    
                    LaunchPurchaseFlow(purchaseFlowParamsBuilder);
                }
            });
        }

        /// <summary>
        /// Updates an existing subscription product using the ONE store IAP SDK.
        /// </summary>
        /// <param name="purchaseFlowParams">The parameters required to process the subscription update, including product ID, type, developer payload, and proration mode.</param>
        /// <remarks>
        /// - If a subscription update is already in progress, the request is rejected, and an error is logged.
        /// - The function constructs a `PurchaseFlowParams` object using the provided parameters.
        /// - A `SubscriptionUpdateParams` object is created to include the proration mode and the old purchase token for the update.
        /// - The update request is initiated using the `LaunchPurchaseFlow` method.
        /// </remarks>
        public void UpdateSubscription(PurchaseFlowParams purchaseFlowParams)
        {
            if (_productInPurchaseFlow != null)
            {
                var message = string.Format("The update subscription for {0} is already in progress.", _productInPurchaseFlow);
                Logger.Error(message);
                RunOnMainThread(() => _callback.OnPurchaseFailed(new IapResult((int) ResponseCode.RESULT_ERROR, message)));
                return;
            }

            ExecuteService(() => {
                _productInPurchaseFlow = purchaseFlowParams.ProductId;

                using (var purchaseFlowParamsBuilder = new AndroidJavaObject(Constants.PurchaseFlowParamsBuilder))
                {
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductIdMethod, purchaseFlowParams.ProductId);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductTypeMethod, purchaseFlowParams.ProductType);
                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetDeveloperPayloadMethod, purchaseFlowParams.DeveloperPayload);

                    purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                        Constants.PurchaseFlowParamsBuilderSetProductNameMethod, purchaseFlowParams.ProductName);
                    
                    using (var updateParamsBuilder = new AndroidJavaObject(Constants.SubscriptionUpdateParamsBuilder))
                    {
                        updateParamsBuilder.Call<AndroidJavaObject>(
                            Constants.SubscriptionUpdateParamsBuilderSetProrationModeMehtod, purchaseFlowParams.ProrationMode);
                        updateParamsBuilder.Call<AndroidJavaObject>(
                            Constants.SubscriptionUpdateParamsBuilderSetOldPurchaseTokenMethod, purchaseFlowParams.OldPurchaseToken);

                        purchaseFlowParamsBuilder.Call<AndroidJavaObject>(
                            Constants.PurchaseFlowParamsBuilderSetSubscriptionUpdateParamsMethod,
                            updateParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod)
                        );
                    }

                    LaunchPurchaseFlow(purchaseFlowParamsBuilder);
                }
            });
        }

        private void LaunchPurchaseFlow(AndroidJavaObject purchaseFlowParamsBuilder)
        {
            _purchaseClient.Call<AndroidJavaObject>(
                Constants.PurchaseClientLaunchPurchaseFlowMethod,
                JniHelper.GetUnityAndroidActivity(),
                purchaseFlowParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod));
        }

        /// <summary>
        /// Consumes a purchased managed product using the ONE store IAP SDK.
        /// </summary>
        /// <param name="purchaseData">The purchase data of the managed product to be consumed.</param>
        /// <remarks>
        /// - If `purchaseData` is null, the request is rejected, and an error is logged.
        /// - The function constructs a `ConsumeParams` object using the provided `purchaseData`.
        /// - The consumption request is sent using `_purchaseClient.Call()`.
        /// - The result is handled by `ConsumeListener`, which triggers the appropriate callback upon response.
        /// </remarks>
        public void ConsumePurchase(PurchaseData purchaseData)
        {
            if (purchaseData == null)
            {
                var message = "PurchaseData is null";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnConsumeFailed(iapResult));
                return;
            }

            ExecuteService(() => {
                using (var consumeParamsBuilder = new AndroidJavaObject(Constants.ConsumeParamsBuilder))
                {
                    consumeParamsBuilder.Call<AndroidJavaObject>(
                        Constants.ConsumeParamsBuilderSetPurchaseDataMethod, purchaseData.ToJava());
                    
                    var consumeListener = new ConsumeListener();
                    consumeListener.OnConsumeResponse += ProcessConsumePurchaseResult;
                    
                    _purchaseClient.Call(
                        Constants.PurchaseClientConsumePurchaseMethod,
                        consumeParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod),
                        consumeListener
                    );
                }
            });
        }

        /// <summary>
        /// Acknowledges a purchased non-consumable or subscription product using the ONE store IAP SDK.
        /// </summary>
        /// <param name="purchaseData">The purchase data of the product to be acknowledged.</param>
        /// <param name="type">The type of product being acknowledged (must not be `ProductType.ALL` or `null`).</param>
        /// <remarks>
        /// - If `purchaseData` is null, the request is rejected, and an error is logged.
        /// - If `type` is null, the request is rejected, and an error is logged.
        /// - If `type` is `ProductType.ALL`, the request is rejected, as acknowledging all product types is not supported.
        /// - The function constructs an `AcknowledgeParams` object using the provided `purchaseData`.
        /// - The acknowledgment request is sent using `_purchaseClient.Call()`.
        /// - The result is handled by `AcknowledgeListener`, which triggers the appropriate callback upon response.
        /// </remarks>
        public void AcknowledgePurchase(PurchaseData purchaseData, ProductType type)
        {
            if (purchaseData == null)
            {
                var message = "PurchaseData is null";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnAcknowledgeFailed(iapResult));
                return;
            }
            else if (type == null)
            {
                var message = "ProductType is null";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnAcknowledgeFailed(iapResult));
                return;
            }
            else if (ProductType.ALL == type)
            {
                var message = "ProductType.ALL is not supported. This is supported only by the QueryProductDetails.";
                Logger.Error(message);
                RunOnMainThread(() => _callback.OnAcknowledgeFailed(new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message)));
                return;
            }

            ExecuteService(() => {
                using (var acknowledgeParamsBuilder = new AndroidJavaObject(Constants.AcknowledgeParamsBuilder))
                {
                    acknowledgeParamsBuilder.Call<AndroidJavaObject>(
                        Constants.AcknowledgeParamsBuilderSetPurchaseDataMethod, purchaseData.ToJava());
                    
                    var acknowledgeListener = new AcknowledgeListener(purchaseData.ProductId, type);
                    acknowledgeListener.OnAcknowledgeResponse += ProcessAcknowledgePurchaseResult;
                    
                    _purchaseClient.Call(
                        Constants.PurchaseClientAcknowledgePurchaseMethod,
                        acknowledgeParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod),
                        acknowledgeListener
                    );
                }
            });
        }

        /// <summary>
        /// Manages an auto product (auto-renewable subscription) using the ONE store IAP SDK.  
        /// This method is obsolete.
        /// </summary>
        /// <param name="purchaseData">The purchase data of the auto product to be managed.</param>
        /// <param name="action">The recurring action to be performed (e.g., cancel, pause, resume).</param>
        /// <remarks>
        /// - If `purchaseData` is null, the request is rejected, and an error is logged.
        /// - If the product type is not `AUTO`, the request is rejected, as only auto products support this feature.
        /// - The function constructs a `RecurringProductParams` object using the provided `purchaseData` and `action`.
        /// - The request is sent using `_purchaseClient.Call()`.
        /// - The result is handled by `RecurringProductListener`, which triggers the appropriate callback upon response.
        /// </remarks>
        [Obsolete]
        public void ManageRecurringProduct(PurchaseData purchaseData, RecurringAction action)
        {
            if (purchaseData == null)
            {
                var message = "PurchaseData is null";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnManageRecurringProduct(iapResult, null, action));
                return;
            }
            else if (ProductType.AUTO != _inventory.GetProductType(purchaseData.ProductId))
            {
                var message = "Purchasing data is not auto-type and does not support this feature.";
                var iapResult = new IapResult((int) ResponseCode.ERROR_ILLEGAL_ARGUMENT, message);
                RunOnMainThread(() => _callback.OnManageRecurringProduct(iapResult, null, action));
                return;
            }

            ExecuteService(() => {
                using (var recurringParamsBuilder = new AndroidJavaObject(Constants.RecurringProductParamsBuilder))
                {
                    recurringParamsBuilder.Call<AndroidJavaObject>(
                        Constants.RecurringProductParamsBuilderSetPurchaseDataMethod, purchaseData.ToJava());
                    recurringParamsBuilder.Call<AndroidJavaObject>(
                        Constants.RecurringProductParamsBuilderSetRecurringActionMethod, action.ToString());
                    
                    var recurringProductListener = new RecurringProductListener(purchaseData.ProductId);
                    recurringProductListener.OnRecurringResponse += ProcessRecurringProductResult;

                    _purchaseClient.Call(
                        Constants.PurchaseClientManageRecurringProductMethod,
                        recurringParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod),
                        recurringProductListener
                    );
                }
            });
        }

        /// <summary>
        /// Launches the update or installation flow for the ONE store IAP SDK.  
        /// This ensures that the latest version of the ONE store service is installed or updated if necessary.
        /// </summary>
        /// <param name="callback">A callback function that receives the result of the update or installation process.</param>
        /// <remarks>
        /// - In-app payments cannot be used if the ONE store service is outdated or not installed.
        /// - The first API call attempts to connect to the ONE store service.
        /// - If `RESULT_NEED_UPDATE` occurs, you must call this method to update or install the service.
        /// - The function creates an `IapResultListener` to handle the response.
        /// - The result from the Java IAP service is parsed using `IapHelper.ParseJavaIapResult()`.
        /// - If the update or installation is successful, the provided callback is invoked with the `IapResult`.
        /// - The process is executed via `_purchaseClient.Call()`, which triggers the update or installation flow.
        /// </remarks>
        public void LaunchUpdateOrInstallFlow(Action<IapResult> callback)
        {
            var iapResultListener = new IapResultListener();
            iapResultListener.OnResponse += (javaIapResult) =>
            {
                var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => callback?.Invoke(iapResult));
                });
            };

            _purchaseClient.Call(
                Constants.PurchaseClientLaunchUpdateOrInstallMethod,
                JniHelper.GetUnityAndroidActivity(),
                iapResultListener
            );
        }

        /// <summary>
        /// Launches the subscription management screen using the ONE store IAP SDK.  
        /// </summary>
        /// <param name="purchaseData">
        /// The purchase data of the subscription product to manage.  
        /// If `purchaseData` is provided, the management screen for that specific subscription product is displayed.  
        /// If `purchaseData` is `null`, the user's subscription list screen is launched.
        /// </param>
        /// <remarks>
        /// - The function creates a `SubscriptionParamsBuilder` to set the purchase data if available.
        /// - If `purchaseData` is provided, the management screen for that specific subscription product is shown.
        /// - If `purchaseData` is `null`, the general subscription list screen is displayed.
        /// - The function executes `_purchaseClient.Call()` to open the subscription management screen.
        /// </remarks>
        public void LaunchManageSubscription(PurchaseData purchaseData)
        {
            using (var subscriptionParamsBuilder = new AndroidJavaObject(Constants.SubscriptionParamsBuilder))
            {
                if (purchaseData != null) {
                    subscriptionParamsBuilder.Call<AndroidJavaObject>(
                        Constants.SubscriptionParamsBuilderSetPurchaseDataMethod, purchaseData.ToJava());
                }

                _purchaseClient.Call(
                    Constants.PurchaseClientLaunchManageSubscriptionMethod,
                    JniHelper.GetUnityAndroidActivity(),
                    subscriptionParamsBuilder.Call<AndroidJavaObject>(Constants.BuildMethod)
                );
            }
        }

        /// <summary>
        /// Retrieves the market distinction code (storeCode) using the ONE store IAP SDK.  
        /// This code is required for using the S2S API from SDK v19 onward.
        /// </summary>
        /// <remarks>
        /// - When the `PurchaseClientImpl` object is initialized, the SDK attempts to connect to the payment module.  
        /// - Upon successful connection, the `storeCode` is automatically obtained and assigned to `PurchaseClientImpl.storeCode`.  
        /// - This function requests the `storeCode` using `_purchaseClient.Call()` and assigns it upon response.
        /// </remarks>
        private void GetStoreCode()
        {
            var storeCodeListener = new StoreInfoListener();
            storeCodeListener.OnStoreInfoResponse += (storeCode) => StoreCode = storeCode;
            
            _purchaseClient.Call(
                Constants.PurchaseClientGetStoreInfoMethod,
                storeCodeListener
            );
        }

        /// <summary>
        /// Checks whether the purchasing service is available by verifying the connection status.
        /// </summary>
        /// <returns>Returns `true` if the service is connected; otherwise, logs a warning and returns `false`.</returns>
        /// <remarks>
        /// - If `_connectionStatus` is `CONNECTED`, the function returns `true`, indicating that the purchasing service is available.  
        /// - If not connected, a warning message is logged, and the function returns `false`.  
        /// </remarks>
        private bool IsPurchasingServiceAvailable()
        {
            if (_connectionStatus == ConnectionStatus.CONNECTED)
            {
                return true;
            }
            var message = string.Format("Purchasing service unavailable. ConnectionStatus: {0}", _connectionStatus.ToString());
            Logger.Warning(message);
            return false;
        }

        /// <summary>
        /// Processes the result of a product details query and updates the inventory accordingly.
        /// </summary>
        /// <param name="type">The type of product being queried.</param>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="javaProductDetailList">The list of product details retrieved from the query.</param>
        /// <remarks>
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the query fails, logs a warning with the error details and updates the query status as `Failed`.  
        /// - Handles error cases by invoking `HandleErrorCode(iapResult)`, triggering the `OnProductDetailsFailed` callback.  
        /// - If successful, parses the product details list and updates the inventory using `_inventory.UpdateProductDetailInventory()`.  
        /// - Marks the query status as `Succeed` and invokes the `OnProductDetailsSucceeded` callback with the retrieved product details.  
        /// </remarks>
        private void ProcessProductDetailsResult(ProductType type, AndroidJavaObject javaIapResult, AndroidJavaObject javaProductDetailList)
        {
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Warning("Retrieve product failed with error code '{0}' and message: '{1}'",
                    iapResult.Code, iapResult.Message);

                _queryProductDetailsCallStatus[type] = AsyncRequestStatus.Failed;

                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnProductDetailsFailed(iapResult));
                });
                return;
            }
            
            var productDetailList = IapHelper.ParseProductDetailsResult(javaIapResult, javaProductDetailList);
            _inventory.UpdateProductDetailInventory(productDetailList);
            _queryProductDetailsCallStatus[type] = AsyncRequestStatus.Succeed;
            RunOnMainThread(() => _callback.OnProductDetailsSucceeded(productDetailList.ToList()));
        }

        /// <summary>
        /// Processes the result of a purchase update and updates the inventory accordingly.
        /// </summary>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="javaPurchasesList">The list of purchases retrieved from the query.</param>
        /// <remarks>
        /// - Resets `_productInPurchaseFlow` to `null` after the purchase process completes.  
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the purchase fails, logs a warning with the error details and invokes `OnPurchaseFailed` callback.  
        /// - If the purchase is successful, retrieves the purchase list using `IapHelper.ParseJavaPurchasesList(javaPurchasesList)`.  
        /// - If any purchases exist, updates the inventory using `_inventory.UpdatePurchaseInventory()`.  
        /// - Invokes the `OnPurchaseSucceeded` callback with the retrieved purchase list.  
        /// </remarks>
        private void ProcessPurchaseUpdatedResult(AndroidJavaObject javaIapResult, AndroidJavaObject javaPurchasesList)
        {
            _productInPurchaseFlow = null;
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Warning("Purchase failed with error code '{0}' and message: '{1}'",
                    iapResult.Code, iapResult.Message);

                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnPurchaseFailed(iapResult));
                });
                return;
            }

            var purchasesList = IapHelper.ParseJavaPurchasesList(javaPurchasesList);
            if (purchasesList.Any())
            {
                _inventory.UpdatePurchaseInventory(purchasesList);
                RunOnMainThread(() => _callback.OnPurchaseSucceeded(purchasesList.ToList()));
            }
        }

        /// <summary>
        /// Processes the result of a purchase query and updates the inventory accordingly.
        /// </summary>
        /// <param name="type">The type of product being queried.</param>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="javaPurchasesList">The list of purchases retrieved from the query.</param>
        /// <remarks>
        /// - Resets `_productInPurchaseFlow` to `null` after processing the query.  
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the query fails, logs a warning, sets the query status to `Failed`, and triggers the `OnPurchaseFailed` callback.  
        /// - If the query is successful, updates `_queryPurchasesCallStatus[type]` to `Succeed`.  
        /// - Retrieves the purchase list using `IapHelper.ParseJavaPurchasesList(javaPurchasesList)`.  
        /// - Updates the inventory with the retrieved purchases using `_inventory.UpdatePurchaseInventory()`.  
        /// - Invokes the `OnPurchaseSucceeded` callback with the purchase list.  
        /// </remarks>
        private void ProcessQueryPurchasesResult(ProductType type, AndroidJavaObject javaIapResult, AndroidJavaObject javaPurchasesList)
        {
            _productInPurchaseFlow = null;
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Warning("Purchase failed with error code '{0}' and message: '{1}'",
                    iapResult.Code, iapResult.Message);

                _queryPurchasesCallStatus[type] = AsyncRequestStatus.Failed;

                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnPurchaseFailed(iapResult));
                });
                return;
            }

            _queryPurchasesCallStatus[type] = AsyncRequestStatus.Succeed;
            var purchasesList = IapHelper.ParseJavaPurchasesList(javaPurchasesList);
            _inventory.UpdatePurchaseInventory(purchasesList);
            RunOnMainThread(() => _callback.OnPurchaseSucceeded(purchasesList.ToList()));
        }

        /// <summary>
        /// Processes the result of a consume purchase request and updates the inventory accordingly.
        /// </summary>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="javaPurchaseData">The purchase data of the consumed product.</param>
        /// <remarks>
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the consumption request fails, logs an error, handles the error code, and triggers the `OnConsumeFailed` callback.  
        /// - If successful, parses the consumed purchase data using `IapHelper.ParseJavaPurchaseData(javaPurchaseData)`.  
        /// - Removes the consumed product from the inventory using `_inventory.RemovePurchase(purchaseData.ProductId)`.  
        /// - Invokes the `OnConsumeSucceeded` callback with the consumed purchase data.  
        /// </remarks>
        private void ProcessConsumePurchaseResult(AndroidJavaObject javaIapResult, AndroidJavaObject javaPurchaseData)
        {
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Error("Failed to finish the consume purchase with error code {0} and message: {1}",
                    iapResult.Code, iapResult.Message);

                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnConsumeFailed(iapResult));
                });
                return;
            }

            var purchaseData = IapHelper.ParseJavaPurchaseData(javaPurchaseData);
            _inventory.RemovePurchase(purchaseData.ProductId);
            RunOnMainThread(() => _callback.OnConsumeSucceeded(purchaseData));
        }

        /// <summary>
        /// Processes the result of an acknowledge purchase request and verifies the updated purchase status.
        /// </summary>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="productId">The product ID of the acknowledged purchase.</param>
        /// <param name="type">The type of product being acknowledged.</param>
        /// <remarks>
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the acknowledgment request fails, logs an error, handles the error code, and triggers the `OnAcknowledgeFailed` callback.  
        /// - If successful, calls `QueryPurchasesInternal()` to verify the purchase status.  
        /// - Runs the result on the main thread and invokes `OnAcknowledgeSucceeded` if the query is successful.  
        /// - If the query fails, invokes the `OnAcknowledgeFailed` callback.  
        /// </remarks>
        private void ProcessAcknowledgePurchaseResult(AndroidJavaObject javaIapResult, string productId, ProductType type)
        {
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Error("Failed to finish the acknowledge purchase with error code {0} and message: {1}",
                    iapResult.Code, iapResult.Message);
             
                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnAcknowledgeFailed(iapResult));
                });
                return;
            }
            
            QueryPurchasesInternal(productId, type, (iapRseult, purchaseData) => {
                RunOnMainThread(() => {
                    if (iapResult.IsSuccessful())
                    {
                        _callback.OnAcknowledgeSucceeded(purchaseData, type);
                    }
                    else
                    {
                        _callback.OnAcknowledgeFailed(iapResult);
                    }
                });
            });
        }

        /// <summary>
        /// Processes the result of a recurring product management request and verifies the updated purchase status.
        /// </summary>
        /// <param name="javaIapResult">The result of the IAP request from the ONE store SDK.</param>
        /// <param name="productId">The product ID of the recurring product being managed.</param>
        /// <param name="recurringAction">The recurring action performed (e.g., cancel, pause, resume).</param>
        /// <remarks>
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the request fails, logs an error, handles the error code, and triggers the `OnManageRecurringProduct` callback with a failure response.  
        /// - If successful, calls `QueryPurchasesInternal()` to verify the updated purchase status.  
        /// - Runs the result on the main thread and invokes `OnManageRecurringProduct` with the retrieved purchase data.  
        /// </remarks>
        private void ProcessRecurringProductResult(AndroidJavaObject javaIapResult, string productId, string recurringAction)
        {
            var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
            if (!iapResult.IsSuccessful())
            {
                Logger.Error("Failed to finish the manage recurring with error code {0} and message: {1}",
                    iapResult.Code, iapResult.Message);

                HandleErrorCode(iapResult, () => {
                    RunOnMainThread(() => _callback.OnManageRecurringProduct(iapResult, null, RecurringAction.Get(recurringAction)));
                });
                return;
            }

            QueryPurchasesInternal(productId, ProductType.AUTO, (result, purchaseData) => {
                RunOnMainThread(() => _callback.OnManageRecurringProduct(result, purchaseData, RecurringAction.Get(recurringAction)));
            });
        }

        /// <summary>
        /// Internally queries the latest purchase status after an `AcknowledgePurchase()` or `ManageRecurringProduct()` API call,  
        /// ensuring that the product's state is updated accordingly by refreshing the inventory.
        /// </summary>
        /// <param name="productId">The ID of the product to query.</param>
        /// <param name="type">The type of product being queried.</param>
        /// <param name="callback">The callback function to return the query result and purchase data.</param>
        /// <remarks>
        /// - This function is called internally after an `AcknowledgePurchase()` or `ManageRecurringProduct()` API call  
        ///   to refresh the product's state based on the latest purchase information.  
        /// - It executes `queryPurchasesAsync` internally to update the inventory with the latest state values.  
        /// - Creates a `QueryPurchasesListener` instance to listen for purchase query responses.  
        /// - Parses the IAP result using `IapHelper.ParseJavaIapResult(javaIapResult)`.  
        /// - If the query fails, logs a warning, handles the error code, and invokes the callback with a failure response.  
        /// - If the query succeeds, retrieves and parses the purchase list using `IapHelper.ParseJavaPurchasesList(javaPurchasesList)`.  
        /// - Updates `_inventory` with the retrieved purchases to maintain the latest product state.  
        /// - If a purchase matching `productId` exists in `_inventory`, invokes the callback with the corresponding `PurchaseData`.  
        /// - Calls `_purchaseClient.Call()` to execute the purchase query asynchronously for the specified product type.  
        /// </remarks>
        private void QueryPurchasesInternal(string productId, ProductType type, Action<IapResult, PurchaseData> callback)
        {
            var queryPurchasesListener = new QueryPurchasesListener(type);
            queryPurchasesListener.OnPurchasesResponse += (_, javaIapResult, javaPurchasesList) => {
                var iapResult = IapHelper.ParseJavaIapResult(javaIapResult);
                if (!iapResult.IsSuccessful())
                {
                    Logger.Warning("QueryPurchasesInternal failed with error code '{0}' and message: '{1}'",
                        iapResult.Code, iapResult.Message);
                    
                    HandleErrorCode(iapResult, () => {
                        callback?.Invoke(iapResult, null);
                    });
                    return;
                }

                var purchasesList = IapHelper.ParseJavaPurchasesList(javaPurchasesList);
                if (purchasesList.Any())
                {
                    _inventory.UpdatePurchaseInventory(purchasesList);
                    PurchaseData purchaseData;
                    if (_inventory.GetPurchaseData(productId, out purchaseData))
                    {
                        callback?.Invoke(iapResult, purchaseData);
                    }
                }
            };

            _purchaseClient.Call(
                Constants.PurchaseClientQueryPurchasesMethod,
                type.ToString(),
                queryPurchasesListener
            );
        }

        /// <summary>
        /// Handles specific IAP error codes and executes the appropriate response.
        /// </summary>
        /// <param name="iapResult">The IAP result containing the error code.</param>
        /// <param name="action">An optional action to execute if the error code does not require a special response.</param>
        /// <remarks>
        /// - Retrieves the response code from the IAP result using `IapHelper.GetResponseCodeFromIapResult(iapResult)`.  
        /// - If the response code is `RESULT_NEED_UPDATE`, triggers the `OnNeedUpdate` callback to prompt the user to update the ONE store service.  
        /// - If the response code is `RESULT_NEED_LOGIN`, triggers the `OnNeedLogin` callback to prompt the user to log in.  
        /// - If the response code is `ERROR_SERVICE_DISCONNECTED`, resets `_productInPurchaseFlow` and updates `_connectionStatus` to `DISCONNECTED`.  
        /// - If none of the above cases match, the provided `action` (if any) is executed.  
        /// </remarks>
        private void HandleErrorCode(IapResult iapResult, Action action = null)
        {
            var responseCode = IapHelper.GetResponseCodeFromIapResult(iapResult);
            switch (responseCode)
            {
                case ResponseCode.RESULT_NEED_UPDATE:
                    RunOnMainThread(() => _callback.OnNeedUpdate());
                    break;
                case ResponseCode.RESULT_NEED_LOGIN:
                    RunOnMainThread(() => _callback.OnNeedLogin());
                    break;
                case ResponseCode.ERROR_SERVICE_DISCONNECTED:
                    _productInPurchaseFlow = null;
                    _connectionStatus = ConnectionStatus.DISCONNECTED;
                    break;
                default:
                    action?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Executes the specified action on the main thread using the ONE store dispatcher.
        /// </summary>
        /// <param name="action">The action to execute on the main thread.</param>
        /// <remarks>
        /// - Calls `OneStoreDispatcher.RunOnMainThread()` to ensure that the provided action runs on the main UI thread.  
        /// - This is useful for updating UI elements or triggering callbacks that require execution on the main thread.  
        /// </remarks>
        private void RunOnMainThread(Action action)
        {
            OneStoreDispatcher.RunOnMainThread(() => action());
        }
    }
}

#endif
