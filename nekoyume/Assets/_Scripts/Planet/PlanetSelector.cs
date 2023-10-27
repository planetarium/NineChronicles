using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Planet
{
    public static class PlanetSelector
    {
        private const string CurrentPlanetIdHexKey = "CurrentPlanetIdHex";

        private static PlanetId DefaultPlanetId => PlanetId.Heimdall;

        public static Subject<PlanetInfo> CurrentPlanetInfoSubject { get; } = new();

        public static async UniTask<PlanetContext> InitializeAsync(
            PlanetContext context)
        {
            context = await InitializePlanetsAsync(context);
            if (context.IsSkipped ||
                context.HasError)
            {
                return context;
            }

            context = InitializeCurrentPlanetInfo(context);
            if (context.HasError)
            {
                return context;
            }

            return UpdateCommandLineOptions(context);
        }

        private static async UniTask<PlanetContext> InitializePlanetsAsync(
            PlanetContext context)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(context.CommandLineOptions.PlanetRegistryUrl))
            {
                Debug.Log("Skip initializing Planets because PlanetRegistryUrl in" +
                          " CommandLineOptions is null or empty in editor.");
                context.IsSkipped = true;
                return context;
            }
#elif !UNITY_ANDROID && !UNITY_IOS
            Debug.Log("Skip initializing Planets because `!UNITY_ANDROID && !UNITY_IOS`" +
                      " is true in non-editor.");
            context.IsSkipped = true;
            return context;
#endif

            var clo = context.CommandLineOptions;
            if (!clo.RpcClient)
            {
                Debug.Log("Skip initializing Planets because RpcClient is false.");
                context.IsSkipped = true;
                return context;
            }

            try
            {
                context.Planets = new Planets(clo.PlanetRegistryUrl);
            }
            catch (ArgumentException)
            {
                context.Error = "Failed to initialize Planets. PlanetRegisterUrl in" +
                                " CommandLineOptions must not be null or empty" +
                                " when RpcClient is true.";
                return context;
            }

            await context.Planets.InitializeAsync();
            if (!context.Planets.IsInitialized)
            {
                context.Error = "Failed to initialize Planets.";
                return context;
            }

            return context;
        }

        private static PlanetContext InitializeCurrentPlanetInfo(PlanetContext context)
        {
            if (context.CommandLineOptions.PlanetId.HasValue)
            {
                return SelectPlanet(context, context.CommandLineOptions.PlanetId.Value);
            }

            var defaultPlanetIdHex = DefaultPlanetId.ToHexString();
            var planetIdHex = PlayerPrefs.GetString(CurrentPlanetIdHexKey, defaultPlanetIdHex);
            var planetId = new PlanetId(planetIdHex);
            return SelectPlanet(context, planetId);
        }

        public static PlanetContext SelectPlanet(PlanetContext context, string planetName)
        {
            if (context.Planets is null)
            {
                context.Error = "Planets is null." +
                                "Use InitializeAsync() before calling this method.";
                return context;
            }

            if (context.CurrentPlanetInfo is not null &&
                context.CurrentPlanetInfo.Name == planetName)
            {
                return context;
            }

            if (!context.Planets.TryGetPlanetInfo(planetName, out var planetInfo))
            {
                context.Error = $"There is no planet info for planet name({planetName}).";
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        public static PlanetContext SelectPlanet(PlanetContext context, PlanetId planetId)
        {
            if (context.Planets is null)
            {
                context.Error = "Planets is null." +
                                "Use InitializeAsync() before calling this method.";
                return context;
            }

            if (context.CurrentPlanetInfo is not null &&
                context.CurrentPlanetInfo.ID.Equals(planetId))
            {
                return context;
            }

            if (!context.Planets.TryGetPlanetInfo(planetId, out var planetInfo))
            {
                context.Error = $"There is no planet info for planet id({planetId}).";
                return context;
            }

            return SelectPlanetInternal(context, planetInfo);
        }

        private static PlanetContext SelectPlanetInternal(PlanetContext context, PlanetInfo planetInfo)
        {
            context.CurrentPlanetInfo = planetInfo;
            PlayerPrefs.SetString(CurrentPlanetIdHexKey, context.CurrentPlanetInfo!.ID.ToHexString());
            context = UpdateCommandLineOptions(context);
            CurrentPlanetInfoSubject.OnNext(context.CurrentPlanetInfo);
            return context;
        }

        private static PlanetContext UpdateCommandLineOptions(PlanetContext context)
        {
            var currentPlanetInfo = context.CurrentPlanetInfo;
            if (currentPlanetInfo is null)
            {
                context.Error = "CurrentPlanetInfo is null.";
                return context;
            }

            if (currentPlanetInfo.RPCEndpoints.HeadlessGrpc.Count == 0)
            {
                context.Error = "RPCEndpoints.HeadlessGrpc is empty." +
                                "Check the planet registry url in command line options: " +
                                context.CommandLineOptions.PlanetRegistryUrl;
                return context;
            }

            var uris = currentPlanetInfo.RPCEndpoints.HeadlessGrpc
                .Select(url => new Uri(url))
                .ToArray();
            context.CommandLineOptions.RpcServerHosts = uris.Select(uri => uri.Host);

            // FIXME: It determines the RPC server host randomly for now.
            var uri = uris[Random.Range(0, uris.Length)];
            context.CommandLineOptions.RpcServerHost = uri.Host;
            context.CommandLineOptions.RpcServerPort = uri.Port;
            return context;
        }
    }
}
