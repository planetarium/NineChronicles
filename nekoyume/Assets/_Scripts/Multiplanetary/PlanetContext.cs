#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet.Crypto;
using Nekoyume.Game.LiveAsset;
using Nekoyume.GraphQL.GraphTypes;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Multiplanetary.Extensions;

namespace Nekoyume.Multiplanetary
{
    public class PlanetContext
    {
        private const int InitializeRegistryRetryCount = 3;

        public enum ErrorType
        {
            PlanetRegistryUrlIsEmpty,
            InitializePlanetRegistryFailed,
            PlanetRegistryNotInitialized,
            NoPlanetInPlanetRegistry,
            PlanetNotFoundInPlanetRegistry,
            NoHeadlessGqlEndpointInPlanet,
            PlanetHeadlessGqlEndpointIsEmpty,
            QueryPlanetAccountInfoFailed,
            PlanetAccountInfosNotInitialized,
            PlanetNotSelected,
            UnsupportedCase01,
        }

        public readonly CommandLineOptions CommandLineOptions;

        public bool IsSkipped { get; }
        public PlanetRegistry? PlanetRegistry { get; set; }
        public PlanetInfo? SelectedPlanetInfo { get; set; }
        public PlanetAccountInfo[]? PlanetAccountInfos { get; set; }
        public PlanetAccountInfo? SelectedPlanetAccountInfo { get; set; }

        // NOTE: This is not kind of planet context, it is authentication context.
        //       But we have no idea where to put this yet.
        public bool? CanSkipPlanetSelection { get; set; }

        public string Error { get; private set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(Error);

        public bool IsInitialized { get; private set; }

        public bool HasPledgedAccount =>
            PlanetAccountInfos?.Any(e =>
                e.IsAgentPledged.HasValue &&
                e.IsAgentPledged.Value) ?? false;

        public bool IsSelectedPlanetAccountPledged => SelectedPlanetAccountInfo is { IsAgentPledged: true };

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;

            if (GameConfig.IsEditor || !commandLineOptions.RpcClient)
            {
                IsSkipped = true;
            }
        }

        /// <summary>
        /// IsSkipped 필드를 수동으로 설정할 수 있는 테스트용 생성자
        /// </summary>
        /// <param name="commandLineOptions">초기화 시킬 CommandLineOptions</param>
        /// <param name="isSkipped">InitializePlanetRegistryAsync()를 스킵할지 여부</param>
        public PlanetContext(CommandLineOptions commandLineOptions, bool isSkipped)
        {
            CommandLineOptions = commandLineOptions;
            IsSkipped = isSkipped;
        }

        public void SetError(ErrorType errorType, params object[] args)
        {
            Error = errorType switch
            {
                ErrorType.PlanetRegistryUrlIsEmpty =>
                    L10nManager.Localize("EDESC_PLANET_REGISTRY_URL_IS_EMPTY"),
                ErrorType.InitializePlanetRegistryFailed =>
                    L10nManager.Localize("EDESC_INITIALIZE_PLANET_REGISTRY_FAILED"),
                ErrorType.PlanetRegistryNotInitialized =>
                    L10nManager.Localize("EDESC_PLANET_REGISTRY_NOT_INITIALIZED"),
                ErrorType.NoPlanetInPlanetRegistry =>
                    L10nManager.Localize("EDESC_NO_PLANET_IN_PLANET_REGISTRY"),
                ErrorType.PlanetNotFoundInPlanetRegistry =>
                    L10nManager.Localize("EDESC_PLANET_NOT_FOUND_IN_PLANET_REGISTRY_FORMAT", args),
                ErrorType.NoHeadlessGqlEndpointInPlanet =>
                    L10nManager.Localize("EDESC_NO_HEADLESS_GQL_ENDPOINT_IN_PLANET_FORMAT", args),
                ErrorType.PlanetHeadlessGqlEndpointIsEmpty =>
                    L10nManager.Localize("EDESC_PLANET_HEADLESS_GQL_ENDPOINT_IS_EMPTY_FORMAT", args),
                ErrorType.QueryPlanetAccountInfoFailed =>
                    L10nManager.Localize("EDESC_QUERY_PLANET_ACCOUNT_INFO_FAILED_FORMAT", args),
                ErrorType.PlanetAccountInfosNotInitialized =>
                    L10nManager.Localize("EDESC_PLANET_ACCOUNT_INFOS_NOT_INITIALIZED"),
                ErrorType.PlanetNotSelected =>
                    L10nManager.Localize("EDESC_PLANET_NOT_SELECTED"),
                ErrorType.UnsupportedCase01 =>
                    L10nManager.Localize("EDESC_UNSUPPORTED_CASE_01", args),
                _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
            };
        }

        public void SetNetworkConnectionError(ErrorType errorType, params object[] args)
        {
            SetError(errorType, args);
            Error = $"{Error}\n{L10nManager.Localize("EDESC_NETWORK_CONNECTION_ERROR")}";
            NcDebug.LogError($"[{nameof(PlanetContext)}] {Error}");
        }

        public async UniTask InitializePlanetContextAsync()
        {
            NcDebug.Log($"[{nameof(PlanetContext)}] Initializing planet registry...");
            if (IsSkipped)
            {
                if (!CommandLineOptions.RpcClient)
                {
                    NcDebug.Log($"[{nameof(PlanetContext)}] Skip initializing PlanetRegistry because RpcClient is false.");
                    return;
                }

                NcDebug.Log($"[{nameof(PlanetContext)}] Skip initializing Planets because PlanetRegistryUrl in CommandLineOptions is null or empty in editor.");
                return;
            }

            var clo = CommandLineOptions;
            if (string.IsNullOrEmpty(clo.PlanetRegistryUrl))
            {
                NcDebug.LogError($"[{nameof(PlanetContext)}] CommandLineOptions.PlanetRegisterUrl must not be null or empty when RpcClient is true.");
                SetError(ErrorType.PlanetRegistryUrlIsEmpty);
                return;
            }

            NcDebug.Log($"[{nameof(PlanetContext)}] Initializing PlanetRegistry with PlanetRegistryUrl: {clo.PlanetRegistryUrl}");
            PlanetRegistry = new PlanetRegistry(clo.PlanetRegistryUrl);

            if (clo.DefaultPlanetId != null)
            {
                LiveAssetManager.instance.SetThorSchedule(new PlanetId(clo.DefaultPlanetId));
            }

            await InitializePlanetRegistryAsync(PlanetRegistry);

            if (!PlanetRegistry.IsInitialized)
            {
                NcDebug.LogError($"[{nameof(PlanetContext)}] Failed to initialize PlanetRegistry.");
                SetNetworkConnectionError(ErrorType.InitializePlanetRegistryFailed);
                return;
            }

            var planetCheckResult = await PlanetInfoCheck();
            if (!planetCheckResult)
            {
                return;
            }

            NcDebug.Log($"[{nameof(PlanetContext)}] PlanetRegistry initialized successfully.");
            IsInitialized = true;
        }

        private async UniTask InitializePlanetRegistryAsync(PlanetRegistry planetRegistry)
        {
            for (var i = 1; i <= InitializeRegistryRetryCount; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                await planetRegistry.InitializeAsync();
                sw.Stop();
                if (planetRegistry.IsInitialized)
                {
                    NcDebug.Log($"[{nameof(PlanetContext)}] PlanetRegistry initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                    break;
                }

                NcDebug.LogError($"[{nameof(PlanetContext)}] Failed to initialize PlanetRegistry. Retry({i})...");
            }
        }

        private async UniTask<bool> PlanetInfoCheck()
        {
            var planetRegistry = PlanetRegistry;
            if (planetRegistry == null)
            {
                NcDebug.LogError($"[{nameof(PlanetContext)}] PlanetRegistry is null.");
                SetError(ErrorType.PlanetRegistryNotInitialized);
                return false;
            }

            var jsonSerializer = new NewtonsoftJsonSerializer();
            var planetInfos = new List<PlanetInfo>();
            foreach (var planetInfo in planetRegistry.PlanetInfos)
            {
                if (planetInfo.RPCEndpoints.HeadlessGql.Count == 0)
                {
                    // ErrorType.NoHeadlessGqlEndpointInPlanet
                    NcDebug.LogError($"[{nameof(PlanetContext)}] HeadlessGql endpoint of planet({planetInfo.ID.ToLocalizedPlanetName(true)}) is empty.");
                    continue;
                }

                var index = UnityEngine.Random.Range(0, planetInfo.RPCEndpoints.HeadlessGql.Count);
                var endpoint = planetInfo.RPCEndpoints.HeadlessGql[index];
                if (string.IsNullOrEmpty(endpoint))
                {
                    // ErrorType.PlanetHeadlessGqlEndpointIsEmpty
                    NcDebug.LogError($"[{nameof(PlanetContext)}] endpoint(index: {index}) is null or empty for planet({planetInfo.ID.ToLocalizedPlanetName(true)}).");
                    continue;
                }

                using var client = new GraphQLHttpClient(endpoint, jsonSerializer);
                client.HttpClient.Timeout = TimeSpan.FromSeconds(10);
                bool hasError = false;
                try
                {
                    var (errors, nodeStatusGraphType) = await client.QueryNodeTipIndex();
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            NcDebug.LogError($"[{nameof(PlanetContext)}] {error.Message}");
                        }

                        hasError = true;
                    }
                }
                catch (Exception e)
                {
                    NcDebug.LogException(e);
                    hasError = true;
                }
                if (hasError)
                {
                    // ErrorType.QueryPlanetAccountInfoFailed
                    NcDebug.LogError($"[{nameof(PlanetContext)}] Querying failed. Check the endpoint url.");
                    continue;
                }
                planetInfos.Add(planetInfo);
            }

            planetRegistry.SetPlanetInfos(planetInfos);
            return true;
        }

        // TODO: PlanetInfoCheck와 PlanetAccountInfo 정보 구성을 분리하는 것이 좋을 것 같습니다.
        public async UniTask<PlanetAccountInfo[]?> SetPlanetAccountInfoAsync(Address agentAddress)
        {
            var planetRegistry = PlanetRegistry;
            if (planetRegistry == null)
            {
                NcDebug.LogError($"[{nameof(PlanetContext)}] PlanetRegistry is null.");
                SetError(ErrorType.PlanetRegistryNotInitialized);
                return null;
            }

            var sw = new Stopwatch();
            sw.Start();
            var planetAccountInfos = new List<PlanetAccountInfo>();
            var jsonSerializer = new NewtonsoftJsonSerializer();

            foreach (var planetInfo in planetRegistry.PlanetInfos)
            {
                var index = UnityEngine.Random.Range(0, planetInfo.RPCEndpoints.HeadlessGql.Count);
                var endpoint = planetInfo.RPCEndpoints.HeadlessGql[index];
                if (string.IsNullOrEmpty(endpoint))
                {
                    // ErrorType.PlanetHeadlessGqlEndpointIsEmpty
                    NcDebug.LogError($"[{nameof(PlanetContext)}] endpoint(index: {index}) is null or empty for planet({planetInfo.ID.ToLocalizedPlanetName(true)}).");
                    continue;
                }

                NcDebug.Log($"[{nameof(PlanetContext)}] Querying agent and avatars for planet({planetInfo.ID.ToLocalizedPlanetName(true)}) with endpoint({endpoint})...");
                AgentAndPledgeGraphType? agentAndPledgeGraphType;
                using var client = new GraphQLHttpClient(endpoint, jsonSerializer);
                client.HttpClient.Timeout = TimeSpan.FromSeconds(10);
                try
                {
                    (_, agentAndPledgeGraphType) = await client.QueryAgentAndPledgeAsync(agentAddress);
                }
                catch (OperationCanceledException ex)
                {
                    NcDebug.LogException(ex);
                    // ErrorType.QueryPlanetAccountInfoFailed
                    NcDebug.LogError($"[{nameof(PlanetContext)}] Querying agent and pledge canceled. Check the network connection.");
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    NcDebug.LogException(ex);
                    // ErrorType.QueryPlanetAccountInfoFailed
                    NcDebug.LogError($"[{nameof(PlanetContext)}] Querying agent and avatars failed. Check the endpoint url.");
                    continue;
                }
                catch (Exception ex)
                {
                    NcDebug.LogException(ex);
                    NcDebug.LogException(ex.InnerException);
                    // ErrorType.QueryPlanetAccountInfoFailed
                    NcDebug.LogError($"[{nameof(PlanetContext)}] Querying agent and avatars failed. Unexpected exception occurred.{ex.GetType().FullName}({ex.InnerException?.GetType().FullName})");
                    continue;
                }

                NcDebug.Log($"[{nameof(PlanetContext)}] {agentAndPledgeGraphType}");
                var info = new PlanetAccountInfo(
                    planetInfo.ID,
                    agentAndPledgeGraphType?.Agent?.Address,
                    agentAndPledgeGraphType?.Pledge.Approved,
                    agentAndPledgeGraphType?.Agent?.AvatarStates ?? Array.Empty<AvatarGraphType>());
                planetAccountInfos.Add(info);
            }

            sw.Stop();
            NcDebug.Log($"[PlanetSelector] PlanetAccountInfos({planetAccountInfos.Count}) updated in {sw.ElapsedMilliseconds}ms.(elapsed)");
            PlanetAccountInfos = planetAccountInfos.ToArray();
            return PlanetAccountInfos;
        }
    }
}
