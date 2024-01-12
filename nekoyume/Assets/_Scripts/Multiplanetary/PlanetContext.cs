#nullable enable

using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;

namespace Nekoyume.Multiplanetary
{
    public class PlanetContext
    {
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
        public bool IsSkipped;
        public PlanetRegistry? PlanetRegistry;
        public PlanetInfo? SelectedPlanetInfo;
        public PlanetAccountInfo[]? PlanetAccountInfos;
        public PlanetAccountInfo? SelectedPlanetAccountInfo;

        // NOTE: This is not kind of planet context, it is authentication context.
        //       But we have no idea where to put this yet.
        public bool? CanSkipPlanetSelection;

        public string Error { get; private set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(Error);

        public bool HasPledgedAccount => PlanetAccountInfos?.Any(e =>
            e.IsAgentPledged.HasValue &&
            e.IsAgentPledged.Value) ?? false;

        public bool IsSelectedPlanetAccountPledged =>
            SelectedPlanetAccountInfo is { IsAgentPledged: true };

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;
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
        }
    }
}
