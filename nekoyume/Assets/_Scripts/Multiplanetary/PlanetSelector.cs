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
using Nekoyume.GraphQL.GraphTypes;
using Nekoyume.Multiplanetary.Extensions;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Nekoyume.Multiplanetary
{
    public static class PlanetSelector
    {
        /// <summary>
        /// Selected planet id in player prefs.
        /// format: <see cref="PlanetId.ToString()"/>
        /// </summary>
        private const string CachedPlanetIdStringKey = "CachedPlanetIdStringKey";

        private static PlanetId DefaultPlanetId => PlanetId.Heimdall;

        public static bool HasCachedPlanetIdString =>
            PlayerPrefs.HasKey(CachedPlanetIdStringKey);

        public static string CachedPlanetIdString =>
            PlayerPrefs.GetString(CachedPlanetIdStringKey);

        public static Subject<(PlanetContext planetContext, PlanetInfo? planetInfo)>
            SelectedPlanetInfoSubject
        { get; } = new();

        public static Subject<(PlanetContext planetContext, PlanetAccountInfo? planetAccountInfo)>
            SelectedPlanetAccountInfoSubject
        { get; } = new();

        public static async UniTask<PlanetContext> InitializePlanetRegistryAsync(
            PlanetContext context)
        {
            NcDebug.Log("[PlanetSelector] Initializing planet registry...");
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(context.CommandLineOptions.PlanetRegistryUrl))
            {
                NcDebug.Log("[PlanetSelector] Skip initializing Planets because" +
                          " PlanetRegistryUrl in CommandLineOptions is" +
                          " null or empty in editor.");
                context.IsSkipped = true;
                return context;
            }

#elif !UNITY_ANDROID && !UNITY_IOS
            Debug.Log("[PlanetSelector] Skip initializing PlanetRegistry because" +
                      " the platform not supported in non-editor." +
                      "(Only Android and iOS are supported)");
            context.IsSkipped = true;
            return context;
#endif

            var clo = context.CommandLineOptions;
            if (!clo.RpcClient)
            {
                NcDebug.Log("[PlanetSelector] Skip initializing PlanetRegistry because" +
                          " RpcClient is false.");
                context.IsSkipped = true;
                return context;
            }

            if (string.IsNullOrEmpty(clo.PlanetRegistryUrl))
            {
                NcDebug.LogError("[PlanetSelector] CommandLineOptions.PlanetRegisterUrl must" +
                               " not be null or empty when RpcClient is true.");
                context.SetError(PlanetContext.ErrorType.PlanetRegistryUrlIsEmpty);
                return context;
            }

            NcDebug.Log("[PlanetSelector] Initializing PlanetRegistry with" +
                      $" PlanetRegistryUrl: {clo.PlanetRegistryUrl}");
            context.PlanetRegistry = new PlanetRegistry(clo.PlanetRegistryUrl);

            for (var i = 0; i < 3; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                await context.PlanetRegistry.InitializeAsync();
                sw.Stop();
                if (context.PlanetRegistry.IsInitialized)
                {
                    NcDebug.Log($"[PlanetSelector] PlanetRegistry initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                    break;
                }

                NcDebug.LogError($"[PlanetSelector] Failed to initialize PlanetRegistry. Retry({i + 1})...");
            }

            if (!context.PlanetRegistry.IsInitialized)
            {
                NcDebug.LogError("[PlanetSelector] Failed to initialize PlanetRegistry.");
                context.SetNetworkConnectionError(PlanetContext.ErrorType.InitializePlanetRegistryFailed);
                return context;
            }

            NcDebug.Log("[PlanetSelector] PlanetRegistry initialized successfully.");
            return context;
        }

        #region PlanetInfo

        /// <summary>
        /// This method initializes <see cref="PlanetContext.SelectedPlanetInfo"/> and
        /// <see cref="PlanetContext.CanSkipPlanetSelection"/> if possible.
        ///
        /// Check planet selection in the following order:
        /// 1. <see cref="Nekoyume.Helper.CommandLineOptions.SelectedPlanetId"/> in context argument.
        /// 2. <see cref="PlanetSelector.CachedPlanetIdString"/>.
        /// 3. <see cref="Nekoyume.Helper.CommandLineOptions.DefaultPlanetId"/> in context argument.
        /// 4. <see cref="PlanetSelector.DefaultPlanetId"/>
        /// 5. finally, first planet info in <see cref="PlanetRegistry.PlanetInfos"/> in context argument.
        /// </summary>
        public static PlanetContext InitializeSelectedPlanetInfo(PlanetContext context)
        {
            NcDebug.Log("[PlanetSelector] Initializing CurrentPlanetInfo...");
            if (context.PlanetRegistry is null)
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry is null." +
                               " Use InitializeAsync() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetRegistryNotInitialized);
                return context;
            }

            if (!context.PlanetRegistry.PlanetInfos.Any())
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry.PlanetInfos is empty." +
                               " It cannot proceed without planet infos.");
                context.SetError(PlanetContext.ErrorType.NoPlanetInPlanetRegistry);
                return context;
            }

            PlanetInfo? planetInfo;
            // Check selected planet id in command line options.
            if (!string.IsNullOrEmpty(context.CommandLineOptions.SelectedPlanetId))
            {
                NcDebug.Log("[PlanetSelector] Use CommandLineOptions.SelectedPlanetId(" +
                          $"{context.CommandLineOptions.SelectedPlanetId}).");
                if (context.PlanetRegistry.TryGetPlanetInfoByIdString(
                        context.CommandLineOptions.SelectedPlanetId,
                        out planetInfo))
                {
                    context = SelectPlanetById(context, planetInfo.ID);
                    context.CanSkipPlanetSelection = !context.HasError;
                    if (context.CanSkipPlanetSelection.Value)
                    {
                        NcDebug.Log("[PlanetSelector] Can skip planet selection.");
                    }

                    return context;
                }

                NcDebug.LogWarning("[PlanetSelector] Cannot use CommandLineOptions.SelectedPlanetId(" +
                                 $"{context.CommandLineOptions.SelectedPlanetId})." +
                                 " PlanetRegistry does not have planet info for it." +
                                 " Try the following steps...");
            }

            // Check cached planet id in player prefs.
            if (HasCachedPlanetIdString)
            {
                NcDebug.Log($"[PlanetSelector] Use cached planet id in PlayerPrefs({CachedPlanetIdString}).");
                if (context.PlanetRegistry.TryGetPlanetInfoByIdString(
                        CachedPlanetIdString,
                        out planetInfo))
                {
                    context = SelectPlanetById(context, planetInfo.ID);
                    context.CanSkipPlanetSelection = !context.HasError;
                    if (context.CanSkipPlanetSelection.Value)
                    {
                        NcDebug.Log("[PlanetSelector] Can skip planet selection.");
                    }

                    return context;
                }

                NcDebug.LogWarning("[PlanetSelector] Cannot use cached planet id in PlayerPrefs(" +
                                 $"{CachedPlanetIdString})." +
                                 " PlanetRegistry does not have planet info for it." +
                                 " Try the following steps...");
            }

            if (!string.IsNullOrEmpty(context.CommandLineOptions.DefaultPlanetId))
            {
                // Use default planet id in command line options.
                NcDebug.Log("[PlanetSelector] Use CommandLineOptions.DefaultPlanetId(" +
                          $"{context.CommandLineOptions.DefaultPlanetId}).");
                if (context.PlanetRegistry.TryGetPlanetInfoByIdString(
                        context.CommandLineOptions.DefaultPlanetId,
                        out planetInfo))
                {
                    return SelectPlanetById(context, planetInfo.ID);
                }

                NcDebug.LogWarning("[PlanetSelector] Cannot use CommandLineOptions.DefaultPlanetId(" +
                                 $"{context.CommandLineOptions.DefaultPlanetId})." +
                                 " PlanetRegistry does not have planet info for it." +
                                 " Try the following steps...");
            }

            // Use default planet id in script.
            NcDebug.Log($"[PlanetSelector] Use PlanetSelector.DefaultPlanetId({DefaultPlanetId}).");
            if (context.PlanetRegistry.TryGetPlanetInfoById(
                    DefaultPlanetId,
                    out planetInfo))
            {
                return SelectPlanetById(context, planetInfo.ID);
            }

            NcDebug.LogWarning("[PlanetSelector] Cannot use PlanetSelector.DefaultPlanetId(" +
                             $"{DefaultPlanetId})." +
                             " PlanetRegistry does not have planet info for it." +
                             " Try the following steps...");

            // Use first planet info in planet registry.
            planetInfo = context.PlanetRegistry.PlanetInfos.First();
            NcDebug.Log("[PlanetSelector] Use PlanetRegistry.PlanetInfos.First(" +
                      $"{planetInfo.ID}).");
            return SelectPlanetById(context, planetInfo.ID);
        }

        public static PlanetContext SelectPlanetById(PlanetContext context, PlanetId planetId)
        {
            NcDebug.Log($"[PlanetSelector] Selecting planet by id({planetId})...");
            if (context.PlanetRegistry is null)
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry is null." +
                               " Use InitializeAsync() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetRegistryNotInitialized);
                return context;
            }

            if (context.SelectedPlanetInfo is not null &&
                context.SelectedPlanetInfo.ID.Equals(planetId))
            {
                return context;
            }

            if (!context.PlanetRegistry.TryGetPlanetInfoById(planetId, out var planetInfo))
            {
                NcDebug.LogError($"[PlanetSelector] There is no planet info for planet id({planetId}).");
                context.SetError(
                    PlanetContext.ErrorType.PlanetNotFoundInPlanetRegistry,
                    planetId.ToLocalizedPlanetName(containsPlanetId: false));
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        public static PlanetContext SelectPlanetByIdString(
            PlanetContext context,
            string planetIdString)
        {
            NcDebug.Log($"[PlanetSelector] Selecting planet by id string({planetIdString})...");
            if (context.PlanetRegistry is null)
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry is null." +
                               " Use InitializeAsync() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetRegistryNotInitialized);
                return context;
            }

            if (context.SelectedPlanetInfo is not null &&
                context.SelectedPlanetInfo.ID.ToString().Equals(planetIdString))
            {
                return context;
            }

            if (!context.PlanetRegistry.TryGetPlanetInfoByIdString(
                    planetIdString,
                    out var planetInfo))
            {
                NcDebug.LogError($"[PlanetSelector] There is no planet info for planet id({planetIdString}).");
                context.SetError(
                    PlanetContext.ErrorType.PlanetNotFoundInPlanetRegistry,
                    PlanetIdExtensions.ToLocalizedPlanetName(planetIdString, containsPlanetId: false));
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        private static PlanetContext SelectPlanetInternal(PlanetContext context, PlanetInfo planetInfo)
        {
            context.SelectedPlanetInfo = planetInfo;
            PlayerPrefs.SetString(CachedPlanetIdStringKey, context.SelectedPlanetInfo!.ID.ToString());
            context = UpdateCommandLineOptions(context);
            if (context.HasError)
            {
                NcDebug.LogError($"[PlanetSelector] Failed to select planet({planetInfo.ID}, {planetInfo.Name}).");
                return context;
            }

            NcDebug.Log($"[PlanetSelector] Planet({planetInfo.ID}, {planetInfo.Name}) selected successfully.");
            SelectedPlanetInfoSubject.OnNext((context, context.SelectedPlanetInfo));
            return UpdateSelectedPlanetAccountInfo(context);
        }

        #endregion

        #region PlanetAccountInfo

        public static async UniTask<PlanetContext> UpdatePlanetAccountInfosAsync(
            PlanetContext context,
            Address agentAddress,
            bool updateSelectedPlanetAccountInfo)
        {
            NcDebug.Log($"[PlanetSelector] Updating PlanetAccountInfos...");
            if (context.PlanetRegistry is null)
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry is null." +
                               " Use InitializeAsync() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetRegistryNotInitialized);
                return context;
            }

            if (!context.PlanetRegistry.PlanetInfos.Any())
            {
                NcDebug.LogError("[PlanetSelector] PlanetRegistry.PlanetInfos is empty." +
                               " It cannot proceed without planet infos.");
                context.SetError(PlanetContext.ErrorType.NoPlanetInPlanetRegistry);
                return context;
            }

            var sw = new Stopwatch();
            sw.Start();
            var planetAccountInfos = new List<PlanetAccountInfo>();
            var jsonSerializer = new NewtonsoftJsonSerializer();
            foreach (var planetInfo in context.PlanetRegistry.PlanetInfos)
            {
                if (planetInfo.RPCEndpoints.HeadlessGql.Count == 0)
                {
                    NcDebug.LogError($"[PlanetSelector] HeadlessGql endpoint of planet({planetInfo.ID}) is empty.");
                    context.SetError(
                        PlanetContext.ErrorType.NoHeadlessGqlEndpointInPlanet,
                        planetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false));
                    break;
                }

                var index = Random.Range(0, planetInfo.RPCEndpoints.HeadlessGql.Count);
                var endpoint = planetInfo.RPCEndpoints.HeadlessGql[index];
                if (string.IsNullOrEmpty(endpoint))
                {
                    NcDebug.LogError($"[PlanetSelector] endpoint(index: {index}) is null or empty" +
                                   $" for planet({planetInfo.ID}).");
                    context.SetError(
                        PlanetContext.ErrorType.PlanetHeadlessGqlEndpointIsEmpty,
                        planetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false),
                        index);
                    break;
                }

                NcDebug.Log($"[PlanetSelector] Querying agent and avatars for planet({planetInfo.ID})" +
                          $" with endpoint({endpoint})...");
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
                    NcDebug.LogError("[PlanetSelector] Querying agent and pledge canceled." +
                                   " Check the network connection.");
                    context.SetNetworkConnectionError(
                        PlanetContext.ErrorType.QueryPlanetAccountInfoFailed,
                        planetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false),
                        agentAddress.ToString());
                    break;
                }
                catch (HttpRequestException ex)
                {
                    NcDebug.LogException(ex);
                    NcDebug.LogError("[PlanetSelector] Querying agent and avatars failed." +
                                   " Check the endpoint url.");
                    context.SetNetworkConnectionError(
                        PlanetContext.ErrorType.QueryPlanetAccountInfoFailed,
                        planetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false),
                        agentAddress.ToString());
                    break;
                }
                catch (Exception ex)
                {
                    NcDebug.LogException(ex);
                    NcDebug.LogException(ex.InnerException);
                    NcDebug.LogError("[PlanetSelector] Querying agent and avatars failed." +
                                   " Unexpected exception occurred." +
                                   $"{ex.GetType().FullName}({ex.InnerException?.GetType().FullName})");
                    context.SetError(
                        PlanetContext.ErrorType.QueryPlanetAccountInfoFailed,
                        planetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false),
                        agentAddress.ToString());
                    break;
                }

                NcDebug.Log($"[PlanetSelector] {agentAndPledgeGraphType}");
                var info = new PlanetAccountInfo(
                    planetInfo.ID,
                    agentAndPledgeGraphType?.Agent?.Address,
                    agentAndPledgeGraphType?.Pledge.Approved,
                    agentAndPledgeGraphType?.Agent?.AvatarStates ?? Array.Empty<AvatarGraphType>());
                planetAccountInfos.Add(info);
            }

            sw.Stop();
            NcDebug.Log($"[PlanetSelector] PlanetAccountInfos({planetAccountInfos.Count})" +
                      $" updated in {sw.ElapsedMilliseconds}ms.(elapsed)");

            if (context.HasError)
            {
                return context;
            }

            context.PlanetAccountInfos = planetAccountInfos.ToArray();
            NcDebug.Log($"[PlanetSelector] PlanetAccountInfos({context.PlanetAccountInfos.Length})" +
                      " updated successfully.");
            return updateSelectedPlanetAccountInfo
                ? UpdateSelectedPlanetAccountInfo(context)
                : context;
        }

        public static PlanetContext SelectPlanetAccountInfo(PlanetContext context, PlanetId planetId)
        {
            NcDebug.Log($"[PlanetSelector] SelectPlanetAccountInfo() invoked. planet id({planetId})");
            if (context.PlanetAccountInfos is null)
            {
                NcDebug.LogError("[PlanetSelector] PlanetAccountInfos is null." +
                               " Use UpdatePlanetAccountInfosAsync() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetAccountInfosNotInitialized);
                return context;
            }

            if (context.SelectedPlanetAccountInfo is not null &&
                context.SelectedPlanetAccountInfo.PlanetId.Equals(planetId))
            {
                return context;
            }

            var info = context.PlanetAccountInfos.FirstOrDefault(e => e.PlanetId.Equals(planetId));
            if (info is null)
            {
                NcDebug.LogError($"[PlanetSelector] There is no planet account info for planet id({planetId}).");
                context.SetError(
                    PlanetContext.ErrorType.PlanetNotFoundInPlanetRegistry,
                    planetId.ToLocalizedPlanetName(containsPlanetId: false));
                return context;
            }

            context.SelectedPlanetAccountInfo = info;
            SelectedPlanetAccountInfoSubject.OnNext((context, context.SelectedPlanetAccountInfo));
            return context;
        }

        private static PlanetContext UpdateSelectedPlanetAccountInfo(PlanetContext context)
        {
            NcDebug.Log("[PlanetSelector] Updating SelectedPlanetAccountInfo...");
            if (context.SelectedPlanetInfo is null ||
                context.PlanetAccountInfos is null)
            {
                return context;
            }

            return SelectPlanetAccountInfo(context, context.SelectedPlanetInfo.ID);
        }

        #endregion

        private static PlanetContext UpdateCommandLineOptions(PlanetContext context)
        {
            var selectedPlanetInfo = context.SelectedPlanetInfo;
            if (selectedPlanetInfo is null)
            {
                NcDebug.LogError("[PlanetSelector] SelectedPlanetInfo is null." +
                               " Use SelectPlanet() before calling this method.");
                context.SetError(PlanetContext.ErrorType.PlanetNotSelected);
                return context;
            }

            var clo = context.CommandLineOptions;
            clo.SelectedPlanetId = selectedPlanetInfo.ID.ToString();
            clo.genesisBlockPath = selectedPlanetInfo.GenesisUri;
            var rpcEndpoints = selectedPlanetInfo.RPCEndpoints;
            if (rpcEndpoints.HeadlessGrpc.Count == 0)
            {
                NcDebug.LogError($"[PlanetSelector] HeadlessGql endpoint of planet({selectedPlanetInfo.ID}) is empty.");
                context.SetError(
                    PlanetContext.ErrorType.NoHeadlessGqlEndpointInPlanet,
                    selectedPlanetInfo.ID.ToLocalizedPlanetName(containsPlanetId: false));
                return context;
            }

            var uris = rpcEndpoints.HeadlessGrpc
                .Select(url => new Uri(url))
                .ToArray();
            clo.RpcServerHosts = uris.Select(uri => uri.Host);

            // FIXME: RpcServer is selected randomly for now.
            var uri = uris[Random.Range(0, uris.Length)];
            clo.RpcServerHost = uri.Host;
            clo.RpcServerPort = uri.Port;

            // FIXME: Other hosts are selected randomly for now.
            clo.ApiServerHost = rpcEndpoints.DataProviderGql.Count > 0
                ? rpcEndpoints.DataProviderGql[Random.Range(0, rpcEndpoints.DataProviderGql.Count)]
                : null;
            clo.MarketServiceHost = rpcEndpoints.MarketRest.Count > 0
                ? rpcEndpoints.MarketRest[Random.Range(0, rpcEndpoints.MarketRest.Count)]
                : null;
            clo.OnBoardingHost = rpcEndpoints.WorldBossRest.Count > 0
                ? rpcEndpoints.WorldBossRest[Random.Range(0, rpcEndpoints.WorldBossRest.Count)]
                : null;
            clo.PatrolRewardServiceHost = rpcEndpoints.PatrolRewardGql.Count > 0
                ? rpcEndpoints.PatrolRewardGql[Random.Range(0, rpcEndpoints.PatrolRewardGql.Count)]
                : null;

            clo.GuildServiceUrl = rpcEndpoints.GuildRest.Any()
                ? rpcEndpoints.GuildRest.First()
                : clo.GuildServiceUrl;

            clo.GuildIconBucket = string.IsNullOrEmpty(selectedPlanetInfo.GuildIconBucket)
                ? clo.GuildIconBucket
                : selectedPlanetInfo.GuildIconBucket;
            return context;
        }
    }
}
