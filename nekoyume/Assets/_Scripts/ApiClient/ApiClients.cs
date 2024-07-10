using Nekoyume.GraphQL;
using Nekoyume.Helper;
using NineChronicles.ExternalServices.IAPService.Runtime;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

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

        public NineChroniclesAPIClient WorldBossClient { get; private set; }

        public NineChroniclesAPIClient RpcGraphQlClient { get; private set; }

        public MarketServiceClient MarketServiceClient { get; private set; }

        public NineChroniclesAPIClient PatrolRewardServiceClient { get; private set; }
        
        public SeasonPassServiceManager SeasonPassServiceManager { get; private set; }
        
        // Game.IAPStoreManager와 기능 정리 가능할지도?
        public IAPServiceManager IAPServiceManager { get; private set; }
        
        public bool IsInitialized { get; private set; }
        
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
            // NOTE: Initialize api client.
            WorldBossClient = new NineChroniclesAPIClient(clo.ApiServerHost);
            
            // NOTE: Initialize graphql client which is targeting to RPC server.
            RpcGraphQlClient = new NineChroniclesAPIClient($"http://{clo.RpcServerHost}/graphql");
            
            // NOTE: Initialize world boss query.
            if (string.IsNullOrEmpty(clo.OnBoardingHost))
            {
                WorldBossQuery.SetUrl(string.Empty);
                NcDebug.Log($"[Game] Start()... WorldBossQuery initialized with empty host url" +
                            " because of no OnBoardingHost." +
                            $" url: {WorldBossQuery.Url}");
            }
            else
            {
                WorldBossQuery.SetUrl(clo.OnBoardingHost);
                NcDebug.Log("[Game] Start()... WorldBossQuery initialized." +
                            $" host: {clo.OnBoardingHost}" +
                            $" url: {WorldBossQuery.Url}");
            }
            
            // NOTE: Initialize market service.
            if (string.IsNullOrEmpty(clo.MarketServiceHost))
            {
                MarketServiceClient = new MarketServiceClient(string.Empty);
                NcDebug.Log("[Game] Start()... MarketServiceClient initialized with empty host url" +
                            " because of no MarketServiceHost");
            }
            else
            {
                MarketServiceClient = new MarketServiceClient(clo.MarketServiceHost);
                NcDebug.Log("[Game] Start()... MarketServiceClient initialized." +
                            $" host: {clo.MarketServiceHost}");
            }
            // NOTE: Initialize patrol reward service.
            PatrolRewardServiceClient = new NineChroniclesAPIClient(clo.PatrolRewardServiceHost);

            // NOTE: Initialize season pass service.
            SeasonPassServiceManager = new SeasonPassServiceManager(clo.SeasonPassServiceHost);
            if (SeasonPassServiceManager.IsInitialized)
            {
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
            
#if UNITY_IOS
            IAPServiceManager = new IAPServiceManager(clo.IAPServiceHost, Store.Apple);
#else
            //pc has to find iap product for mail box system
            IAPServiceManager = new IAPServiceManager(clo.IAPServiceHost, Store.Google);
#endif
            
            IsInitialized = true;
        }
    }
}
