using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Planet
{
    public class Planets
    {
        private readonly string _planetRegistryUrl;
        private List<PlanetInfo> _planetInfos;

        public bool IsInitialized { get; private set; }
        public IEnumerable<PlanetInfo> PlanetInfos => _planetInfos;

        public Planets(string planetRegistryUrl)
        {
            if (string.IsNullOrEmpty(planetRegistryUrl))
            {
                throw new ArgumentException(
                    $"{nameof(planetRegistryUrl)} must not be null or empty.",
                    nameof(planetRegistryUrl));
            }

            _planetRegistryUrl = planetRegistryUrl;
            _planetInfos = new List<PlanetInfo>();
        }

        public async UniTask<bool> InitializeAsync(float timeout = 10f)
        {
            if (IsInitialized)
            {
                return true;
            }

            Debug.Log("Planets] start initialization");
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
                    Debug.LogError($"Planets] initialize failed due to timeout({timeout})");
                    return false;
                }
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Planets] initialize failed: {req.error}");
                return false;
            }

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PlanetIdJsonConverter());
            var planetInfos = JsonSerializer.Deserialize<List<PlanetInfo>>(req.downloadHandler.text, options);
            if (planetInfos is not null)
            {
                if (planetInfos.Count == 0)
                {
                    Debug.LogError("Planets] initialize failed: count of planet infos is 0.");
                    return false;
                }

                _planetInfos = planetInfos;
                IsInitialized = true;
            }

            Debug.Log($"Planets] finish initialization: {IsInitialized}");
            return IsInitialized;
        }

        public bool TryGetPlanetInfo(PlanetId planetId, out PlanetInfo planetInfo)
        {
            planetInfo = _planetInfos.FirstOrDefault(e => e.ID.Equals(planetId));
            return planetInfo is not null;
        }
        
        public bool TryGetPlanetInfo(string planetName, out PlanetInfo planetInfo)
        {
            planetName = planetName.ToLower();
            planetInfo = _planetInfos.FirstOrDefault(e => e.Name.ToLower().Equals(planetName));
            return planetInfo is not null;
        }
    }
}
