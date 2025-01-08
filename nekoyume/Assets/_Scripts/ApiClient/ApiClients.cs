using Nekoyume.GraphQL;
using Nekoyume.Helper;

namespace Nekoyume.ApiClient
{
    public class ApiClients
    {
#region Singleton

        private static class Singleton
        {
            internal static readonly ApiClients Value = new();
        }

        public static ApiClients Instance => Singleton.Value;

#endregion Singleton

        private static readonly string DccUrlJsonPath =
            Platform.GetStreamingAssetsPath("dccUrl.json");

        public NineChroniclesAPIClient WorldBossClient { get; private set; }

        public NineChroniclesAPIClient RpcGraphQlClient { get; private set; }

        public MarketServiceClient MarketServiceClient { get; private set; }

        public SeasonPassServiceManager SeasonPassServiceManager { get; private set; }

        public ArenaServiceManager Arenaservicemanager { get; private set; }

        // Game.IAPStoreManager와 기능 정리 가능할지도?
        public IAPServiceManager IAPServiceManager { get; private set; }

        public bool IsInitialized { get; private set; }

        public DccUrl DccURL { get; private set; }

        public void SetDccUrl()
        {
            DccURL = DccUrl.Load(DccUrlJsonPath);
        }

        // TODO: 중복코드 정리, 초기화 안 된 경우 로직 정리
        public void Initialize(CommandLineOptions clo)
        {
            if (clo == null)
            {
                NcDebug.LogError($"[{nameof(ApiClients)}] CommandLineOptions is null.");
                return;
            }

            // NOTE: planetContext.CommandLineOptions and _commandLineOptions are same.
            // NOTE: Initialize several services after Agent initialized.
            WorldBossClient = new NineChroniclesAPIClient(clo.ApiServerHost);
            RpcGraphQlClient = string.IsNullOrEmpty(clo.RpcServerHost) ?
                new NineChroniclesAPIClient(string.Empty) :
                new NineChroniclesAPIClient($"http://{clo.RpcServerHost}/graphql");
            WorldBossQuery.SetUrl(clo.OnBoardingHost);
            MarketServiceClient = new MarketServiceClient(clo.MarketServiceHost);
            SeasonPassServiceManager = new SeasonPassServiceManager(clo.SeasonPassServiceHost);
            Arenaservicemanager = new ArenaServiceManager(clo.ArenaServiceHost);
            ApplySeasonPassMarketUrl(clo);

#if UNITY_IOS
            IAPServiceManager = new IAPServiceManager(clo.IAPServiceHost, InAppPurchaseServiceClient.Store.APPLE);
#else
            //pc has to find iap product for mail box system
            IAPServiceManager = new IAPServiceManager(clo.IAPServiceHost, InAppPurchaseServiceClient.Store.GOOGLE);
#endif
            IsInitialized = true;
        }

        private void ApplySeasonPassMarketUrl(CommandLineOptions clo)
        {
            if (!SeasonPassServiceManager.IsInitialized)
            {
                return;
            }

            if (!string.IsNullOrEmpty(clo.GoogleMarketUrl))
            {
                SeasonPassServiceManager.GoogleMarketURL = clo.GoogleMarketUrl;
            }

            if (!string.IsNullOrEmpty(clo.AppleMarketUrl))
            {
                SeasonPassServiceManager.AppleMarketURL = clo.AppleMarketUrl;
            }

            NcDebug.Log("[Game] Start()... SeasonPassServiceManager initialized." +
                $" host: {clo.SeasonPassServiceHost}" +
                $", google: {SeasonPassServiceManager.GoogleMarketURL}" +
                $", apple: {SeasonPassServiceManager.AppleMarketURL}");
        }
    }
}
