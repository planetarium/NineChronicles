#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Helper;
using Nekoyume.L10n;

namespace Nekoyume.Multiplanetary
{
    public class PlanetContext
    {
        private const int InitializeRetryCount = 3;

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
            UnsupportedCase01
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

        public async UniTask InitializePlanetRegistryAsync()
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

            for (var i = 1; i <= InitializeRetryCount; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                await PlanetRegistry.InitializeAsync();
                sw.Stop();
                if (PlanetRegistry.IsInitialized)
                {
                    NcDebug.Log($"[{nameof(PlanetContext)}] PlanetRegistry initialized in {sw.ElapsedMilliseconds}ms.(elapsed)");
                    break;
                }

                NcDebug.LogError($"[{nameof(PlanetContext)}] Failed to initialize PlanetRegistry. Retry({i})...");
            }

            if (!PlanetRegistry.IsInitialized)
            {
                NcDebug.LogError($"[{nameof(PlanetContext)}] Failed to initialize PlanetRegistry.");
                SetNetworkConnectionError(ErrorType.InitializePlanetRegistryFailed);
                return;
            }

            NcDebug.Log($"[{nameof(PlanetContext)}] PlanetRegistry initialized successfully.");
        }
    }
}
