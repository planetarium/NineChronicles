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
            SelectedPlanetInfoSubject { get; } = new();
        public static Subject<(PlanetContext planetContext, PlanetAccountInfo? planetAccountInfo)>
            SelectedPlanetAccountInfoSubject { get; } = new();

        public static async UniTask<PlanetContext> InitializePlanetRegistryAsync(
            PlanetContext context)
        {
            Debug.Log("[PlanetSelector] Initializing Planets...");
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(context.CommandLineOptions.PlanetRegistryUrl))
            {
                Debug.Log("[PlanetSelector] Skip initializing Planets because" +
                          " PlanetRegistryUrl in CommandLineOptions is" +
                          " null or empty in editor.");
                context.IsSkipped = true;
                return context;
            }

#elif !UNITY_ANDROID && !UNITY_IOS
            Debug.Log("[PlanetSelector] Skip initializing Planets because" +
                      " the platform not supported in non-editor." +
                      "(Only Android and iOS are supported)");
            context.IsSkipped = true;
            return context;
#endif

            var clo = context.CommandLineOptions;
            if (!clo.RpcClient)
            {
                Debug.Log("[PlanetSelector] Skip initializing Planets because" +
                          " RpcClient is false.");
                context.IsSkipped = true;
                return context;
            }

            try
            {
                Debug.Log("[PlanetSelector] Initializing Planets with" +
                          $" PlanetRegistryUrl: {clo.PlanetRegistryUrl}");
                context.PlanetRegistry = new PlanetRegistry(clo.PlanetRegistryUrl);
            }
            catch (ArgumentException)
            {
                context.Error = "[PlanetSelector] Failed to initialize Planets." +
                                " PlanetRegisterUrl in CommandLineOptions must" +
                                " not be null or empty when RpcClient is true.";
                Debug.LogError(context.Error);
                return context;
            }

            var sw = new Stopwatch();
            sw.Start();
            await context.PlanetRegistry.InitializeAsync();
            sw.Stop();
            Debug.Log($"[PlanetSelector] PlanetRegistry initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
            if (!context.PlanetRegistry.IsInitialized)
            {
                context.Error = "[PlanetSelector] Failed to initialize Planets.";
                Debug.LogError(context.Error);
                return context;
            }

            Debug.Log("[PlanetSelector] Planets initialized successfully.");
            return context;
        }

        #region PlanetInfo

        public static PlanetContext InitializeSelectedPlanetInfo(PlanetContext context)
        {
            Debug.Log("[PlanetSelector] Initializing CurrentPlanetInfo...");
            // Check selected planet id in command line options.
            if (!string.IsNullOrEmpty(context.CommandLineOptions.SelectedPlanetId))
            {
                Debug.Log("[PlanetSelector] Selected planet id found in command line options.");
                if (context.PlanetRegistry is null)
                {
                    context.Error = "[PlanetSelector] Planets is null." +
                                    " Use InitializeAsync() before calling this method.";
                    Debug.LogError(context.Error);
                    return context;
                }

                if (context.PlanetRegistry.TryGetPlanetInfoByIdString(
                        context.CommandLineOptions.SelectedPlanetId,
                        out var planetInfo))
                {
                    context = SelectPlanetById(context, planetInfo.ID);
                    context.CanSkipPlanetSelection = !context.HasError;
                    if (context.CanSkipPlanetSelection.Value)
                    {
                        Debug.Log("[PlanetSelector] Can skip planet selection.");
                    }

                    return context;
                }

                context.Error = "[PlanetSelector] There is no planet info for" +
                                $" planet id({CachedPlanetIdString})." +
                                " Check the CommandLineOptions.SelectedPlanetId. with" +
                                " CommandLineOptions.PlanetRegistryUrl.";
                Debug.LogError(context.Error);
                return context;
            }

            // Check cached planet id in player prefs.
            if (HasCachedPlanetIdString)
            {
                Debug.Log("[PlanetSelector] Cached planet id found in player prefs.");
                if (context.PlanetRegistry is null)
                {
                    context.Error = "[PlanetSelector] Planets is null." +
                                    " Use InitializeAsync() before calling this method.";
                    Debug.LogError(context.Error);
                    return context;
                }

                if (context.PlanetRegistry.TryGetPlanetInfoByIdString(
                        CachedPlanetIdString,
                        out var planetInfo))
                {
                    context = SelectPlanetById(context, planetInfo.ID);
                    context.CanSkipPlanetSelection = !context.HasError;
                    if (context.CanSkipPlanetSelection.Value)
                    {
                        Debug.Log("[PlanetSelector] Can skip planet selection.");
                    }

                    return context;
                }

                Debug.LogWarning("[PlanetSelector] There is no planet info for" +
                                 $" planet id({CachedPlanetIdString})." +
                                 " Resetting cached planet id...");
                PlayerPrefs.DeleteKey(CachedPlanetIdStringKey);
            }

            if (string.IsNullOrEmpty(context.CommandLineOptions.DefaultPlanetId))
            {
                // Use default planet id in script.
                Debug.Log($"[PlanetSelector] Using PlanetSelector.DefaultPlanetId({DefaultPlanetId}).");
                context = SelectPlanetById(context, DefaultPlanetId);
            }
            else
            {
                // Use default planet id in command line options.
                Debug.Log($"[PlanetSelector] Default planet id({context.CommandLineOptions.DefaultPlanetId})" +
                          $" found in command line options.");
                context = SelectPlanetByIdString(context, context.CommandLineOptions.DefaultPlanetId);
            }

            if (context.HasError)
            {
                return context;
            }

            Debug.Log("[PlanetSelector] CurrentPlanetInfo initialized successfully.");
            return context;
        }

        public static PlanetContext SelectPlanetById(PlanetContext context, PlanetId planetId)
        {
            Debug.Log($"[PlanetSelector] Selecting planet by id({planetId})...");
            if (context.PlanetRegistry is null)
            {
                context.Error = "[PlanetSelector] Planets is null." +
                                " Use InitializeAsync() before calling this method.";
                Debug.LogError(context.Error);
                return context;
            }

            if (context.SelectedPlanetInfo is not null &&
                context.SelectedPlanetInfo.ID.Equals(planetId))
            {
                return context;
            }

            if (!context.PlanetRegistry.TryGetPlanetInfoById(planetId, out var planetInfo))
            {
                context.Error = $"[PlanetSelector] There is no planet info for planet id({planetId}).";
                Debug.LogError(context.Error);
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        public static PlanetContext SelectPlanetByIdString(
            PlanetContext context,
            string planetIdString)
        {
            Debug.Log($"[PlanetSelector] Selecting planet by id string({planetIdString})...");
            if (context.PlanetRegistry is null)
            {
                context.Error = "[PlanetSelector] Planets is null." +
                                " Use InitializeAsync() before calling this method.";
                Debug.LogError(context.Error);
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
                context.Error = $"[PlanetSelector] There is no planet info for planet id({planetIdString}).";
                Debug.LogError(context.Error);
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        public static PlanetContext SelectPlanetByName(PlanetContext context, string planetName)
        {
            Debug.Log($"[PlanetSelector] Selecting planet by name({planetName})...");
            if (context.PlanetRegistry is null)
            {
                context.Error = "[PlanetSelector] Planets is null." +
                                " Use InitializeAsync() before calling this method.";
                Debug.LogError(context.Error);
                return context;
            }

            if (context.SelectedPlanetInfo is not null &&
                context.SelectedPlanetInfo.Name == planetName)
            {
                return context;
            }

            if (!context.PlanetRegistry.TryGetPlanetInfoByName(planetName, out var planetInfo))
            {
                context.Error = $"[PlanetSelector] There is no planet info for planet name({planetName}).";
                Debug.LogError(context.Error);
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
                Debug.LogError($"[PlanetSelector] Failed to select planet({planetInfo.ID}, {planetInfo.Name}).");
                return context;
            }

            Debug.Log($"[PlanetSelector] Planet({planetInfo.ID}, {planetInfo.Name}) selected successfully.");
            SelectedPlanetInfoSubject.OnNext((context, context.SelectedPlanetInfo));
            return UpdateSelectedPlanetAccountInfo(context);
        }

        #endregion

        #region PlanetAccountInfo

        public static async UniTask<PlanetContext> UpdatePlanetAccountInfosAsync(
            PlanetContext context,
            Address agentAddress)
        {
            Debug.Log($"[PlanetSelector] Updating PlanetAccountInfos...");
            if (context.PlanetRegistry is null)
            {
                context.Error = "[PlanetSelector] Planets is null." +
                                " Use InitializeAsync() before calling this method.";
                Debug.LogError(context.Error);
                return context;
            }

            if (!context.PlanetRegistry.PlanetInfos?.Any() ?? true)
            {
                context.Error = "[PlanetSelector] Planets.PlanetInfos is null or empty." +
                                " It cannot proceed without planet infos.";
                Debug.LogError(context.Error);
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
                    context.Error = $"[PlanetSelector] HeadlessGql is empty for planet({planetInfo.ID}).";
                    Debug.LogError(context.Error);
                    break;
                }

                var index = Random.Range(0, planetInfo.RPCEndpoints.HeadlessGql.Count);
                var endpoint = planetInfo.RPCEndpoints.HeadlessGql[index];
                if (string.IsNullOrEmpty(endpoint))
                {
                    context.Error = $"[PlanetSelector] endpoint(index: {index}) is null or empty" +
                                    $" for planet({planetInfo.ID}).";
                    Debug.LogError(context.Error);
                    break;
                }

                Debug.Log($"[PlanetSelector] Querying agent and avatars for planet({planetInfo.ID})" +
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
                    Debug.LogException(ex);
                    const string message = "[PlanetSelector] Querying agent and avatars canceled." +
                                           " Check the network connection.";
                    context.Error = message;
                    Debug.LogError(message);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    Debug.LogException(ex);
                    const string message = "[PlanetSelector] Querying agent and avatars failed." +
                                           " Check the endpoint url.";
                    context.Error = message;
                    Debug.LogError(message);
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogException(ex.InnerException);
                    var message = "[PlanetSelector] Querying agent and avatars failed." +
                                  " Unexpected exception occurred." +
                                  $"{ex.GetType().FullName}({ex.InnerException?.GetType().FullName})";
                    context.Error = message;
                    Debug.LogError(message);
                    break;
                }

                Debug.Log($"[PlanetSelector] {agentAndPledgeGraphType}");
                var info = new PlanetAccountInfo(
                    planetInfo.ID,
                    agentAndPledgeGraphType?.Agent?.Address,
                    agentAndPledgeGraphType?.Pledge.Approved,
                    agentAndPledgeGraphType?.Agent?.AvatarStates ?? Array.Empty<AvatarGraphType>());
                planetAccountInfos.Add(info);
            }

            sw.Stop();
            Debug.Log($"[PlanetSelector] PlanetAccountInfos({planetAccountInfos.Count})" +
                      $" updated in {sw.ElapsedMilliseconds}ms.(elapsed)");

            if (context.HasError)
            {
                return context;
            }

            context.PlanetAccountInfos = planetAccountInfos.ToArray();
            Debug.Log($"[PlanetSelector] PlanetAccountInfos({context.PlanetAccountInfos.Length})" +
                      " updated successfully.");
            return UpdateSelectedPlanetAccountInfo(context);
        }

        public static PlanetContext SelectPlanetAccountInfo(PlanetContext context, PlanetId planetId)
        {
            Debug.Log($"[PlanetSelector] SelectPlanetAccountInfo() invoked. planet id({planetId})");
            if (context.PlanetAccountInfos is null)
            {
                context.Error = "[PlanetSelector] PlanetAccountInfos is null." +
                                " Use UpdatePlanetAccountInfosAsync() before calling this method.";
                Debug.LogError(context.Error);
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
                context.Error = $"[PlanetSelector] There is no planet account info for planet id({planetId}).";
                Debug.LogError(context.Error);
                return context;
            }

            context.SelectedPlanetAccountInfo = info;
            SelectedPlanetAccountInfoSubject.OnNext((context, context.SelectedPlanetAccountInfo));
            return context;
        }

        private static PlanetContext UpdateSelectedPlanetAccountInfo(PlanetContext context)
        {
            Debug.Log("[PlanetSelector] Updating SelectedPlanetAccountInfo...");
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
            var currentPlanetInfo = context.SelectedPlanetInfo;
            if (currentPlanetInfo is null)
            {
                context.Error = "[PlanetSelector] CurrentPlanetInfo is null." +
                                " Use SelectPlanet() before calling this method.";
                Debug.LogError(context.Error);
                return context;
            }

            var clo = context.CommandLineOptions;
            clo.SelectedPlanetId = currentPlanetInfo.ID.ToString();
            clo.genesisBlockPath = currentPlanetInfo.GenesisUri;
            var rpcEndpoints = currentPlanetInfo.RPCEndpoints;
            if (rpcEndpoints.HeadlessGrpc.Count == 0)
            {
                context.Error = "[PlanetSelector] RPCEndpoints.HeadlessGrpc is empty." +
                                " Check the planet registry url in command line options: " +
                                clo.PlanetRegistryUrl;
                Debug.LogError(context.Error);
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

            return context;
        }
    }
}
