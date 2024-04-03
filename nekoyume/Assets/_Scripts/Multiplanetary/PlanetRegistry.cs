using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Multiplanetary
{
    public class PlanetRegistry
    {
        private readonly string _planetRegistryUrl;
        private PlanetInfo[] _planetInfos;

        public bool IsInitialized { get; private set; }
        public IEnumerable<PlanetInfo> PlanetInfos => _planetInfos;

        public PlanetRegistry(string planetRegistryUrl)
        {
            if (string.IsNullOrEmpty(planetRegistryUrl))
            {
                throw new ArgumentException(
                    $"{nameof(planetRegistryUrl)} must not be null or empty.",
                    nameof(planetRegistryUrl));
            }

            _planetRegistryUrl = planetRegistryUrl;
            _planetInfos = Array.Empty<PlanetInfo>();
        }

        public async UniTask<bool> InitializeAsync(
            float timeout = 10f,
            bool shuffleOrderOfPlanetInfos = true)
        {
            if (IsInitialized)
            {
                return true;
            }

            NcDebug.Log("[PlanetRegistry] start initialization");
            using var req = UnityWebRequest.Get(_planetRegistryUrl);
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            try
            {
                await req.SendWebRequest().WithCancellation(cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    NcDebug.LogError($"[PlanetRegistry] initialize failed due to timeout({timeout})");
                    return false;
                }
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                NcDebug.LogError($"[PlanetRegistry] initialize failed: {req.error}");
                return false;
            }

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PlanetIdJsonConverter());
            var planetInfos = JsonSerializer.Deserialize<List<PlanetInfo>>(req.downloadHandler.text, options);
            if (planetInfos is not null)
            {
                if (planetInfos.Count == 0)
                {
                    NcDebug.LogError("[PlanetRegistry] initialize failed: count of planet infos is 0.");
                    return false;
                }

                if (shuffleOrderOfPlanetInfos)
                {
                    _planetInfos = planetInfos.OrderByDescending(e =>
                            e.ID.Equals(PlanetId.Odin) ||
                            e.ID.Equals(PlanetId.OdinInternal)
                                ? default
                                : Guid.NewGuid())
                        .ToArray();
                }
                else
                {
                    _planetInfos = planetInfos.ToArray();
                }

                var text = string.Join(", ", _planetInfos.Select(e =>
                    $"{e.ID.ToString()}({e.Name})"));
                NcDebug.Log($"[PlanetRegistry] initialize succeeded: [{text}]");

                IsInitialized = true;
            }

            NcDebug.Log($"[PlanetRegistry] finish initialization: {IsInitialized}");
            return IsInitialized;
        }

        public bool TryGetPlanetInfoById(PlanetId planetId, out PlanetInfo planetInfo)
        {
            planetInfo = _planetInfos.FirstOrDefault(e => e.ID.Equals(planetId));
            return planetInfo is not null;
        }

        public bool TryGetPlanetInfoByIdString(
            string planetIdString,
            out PlanetInfo planetInfo)
        {
            planetInfo = _planetInfos.FirstOrDefault(e =>
                e.ID.ToString().Equals(planetIdString) ||
                e.ID.ToHexString().Equals(planetIdString));
            return planetInfo is not null;
        }

        public bool TryGetPlanetInfoByName(string planetName, out PlanetInfo planetInfo)
        {
            planetName = planetName.ToLower();
            planetInfo = _planetInfos.FirstOrDefault(e => e.Name.ToLower().Equals(planetName));
            return planetInfo is not null;
        }

        public bool TryGetPlanetInfoByHeadlessGrpc(string headlessGrpc, out PlanetInfo planetInfo)
        {
            planetInfo = null;
            foreach (var pInfo in _planetInfos)
            {
                foreach (var grpc in pInfo.RPCEndpoints.HeadlessGrpc)
                {
                    if (grpc.Contains(headlessGrpc))
                    {
                        planetInfo = pInfo;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
