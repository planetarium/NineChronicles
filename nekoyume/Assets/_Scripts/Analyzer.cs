#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define ENABLE_FIREBASE
#endif
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.State;
using Nekoyume.Telemetry;

#if ENABLE_FIREBASE
using Firebase.Analytics;
#endif

//using Sentry;
/*
 * [ISSUE TRACKER]
 * https://issuetracker.unity3d.com/issues/il2cpp-build-fails-when-using-an-assembly-renamed-via-sentrys-assembly-alias-tool
 * Sentry will cause error in Android il2cpp build when use current unity version.
 * DON'T use this package, or UPDATE unity to any version above "2022.1.12f1, 2022.2.0b4, 2023.1.0a5".
 */
using UnityEngine;

namespace Nekoyume
{
    public interface ITransaction
    {
        // Dumb stub
    }

    public class Analyzer
    {
        public static Analyzer Instance => Game.Game.instance.Analyzer;

        private readonly bool _isTrackable;

        private readonly TelemetrySender? _telemetry;

        public Analyzer(
            string? uniqueId = null,
            string? planetId = null,
            string? rpcServerHost = null,
            bool isTrackable = false,
            TelemetrySender? telemetry = null)
        {
            _isTrackable = isTrackable;
            _telemetry = telemetry;
            if (!_isTrackable)
            {
                NcDebug.Log($"Analyzer does not track: {nameof(isTrackable)} is false");
                return;
            }

            planetId ??= "no-planet-id";
            rpcServerHost ??= "no-rpc-host";

            // ReSharper disable Unity.UnknownResource
            var clientHost = Resources.Load<TextAsset>("ClientHost")?.text ?? "no-host";
            var clientHash = Resources.Load<TextAsset>("ClientHash")?.text ?? "no-hash";
            var targetNetwork = Resources.Load<TextAsset>("TargetNetwork")?.text ?? "no-target";
            // ReSharper restore Unity.UnknownResource

            InitializeSentry(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost);
#if ENABLE_FIREBASE
            InitializeFirebaseAnalytics(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost);
#endif
            SetAgentAddress(uniqueId);
            SetPlanetId(planetId);
            UpdateAvatarAddress();

            Game.Lobby.OnLobbyEnterEvent += UpdateAvatarAddress;

            NcDebug.Log($"Analyzer initialized: {uniqueId}");
        }

        public static void SetAgentAddress(string? addressString)
        {
#if ENABLE_FIREBASE
            if (addressString is not null)
            {
                FirebaseAnalytics.SetUserId(addressString);
            }
#endif

            addressString ??= "none";

            // SentrySdk.ConfigureScope(scope => { scope.User.Id = uniqueId; });
#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("AgentAddress", addressString);
#endif
        }

        public static void SetPlanetId(string? planetId)
        {
            NcDebug.Log($"[Analyzer] SetPlanetId() invoked with {planetId}");
            planetId ??= "no-planet-id";

            // SentrySdk.ConfigureScope(scope => scope.SetTag("planet-id", planetId));
#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("planet_id", planetId);
#endif
        }

        private static void InitializeSentry(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost)
        {
            // SentrySdk.ConfigureScope(scope =>
            // {
            //     scope.SetTag("client-host", clientHost);
            //     scope.SetTag("client-hash", clientHash);
            //     scope.SetTag("target-network", targetNetwork);
            //     scope.SetTag("rpc-server-host", rpcServerHost);
            // });
        }

#if ENABLE_FIREBASE
        private static void InitializeFirebaseAnalytics(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost)
        {
            FirebaseAnalytics.SetUserProperty("client_host", clientHost);
            FirebaseAnalytics.SetUserProperty("client_hash", clientHash);
            FirebaseAnalytics.SetUserProperty("target_network", targetNetwork);
            FirebaseAnalytics.SetUserProperty("rpc_server_host", rpcServerHost);
        }
#endif

        //private ITransaction CreateTrace(string eventName, Dictionary<string, string> properties)
        //{
        //    if (!_isTrackable)
        //    {
        //        return null;
        //    }
        //    var transaction = SentrySdk.StartTransaction(eventName, eventName);
        //    foreach (var (key, val) in properties)
        //    {
        //        transaction.SetTag(key, val);
        //    }
        //    return transaction;
        //}

        public void FinishTrace(ITransaction transaction)
        {
            //if (transaction is not null)
            //{
            //    transaction.Finish();
            //}
        }

        public void Track(string eventName, params (string key, string value)[] properties)
        {
            SendTelemetry(eventName, () =>
            {
                var props = new Dictionary<string, string>();
                foreach (var (key, value) in properties)
                {
                    if (key is not null && value is not null)
                    {
                        props[key] = value;
                    }
                }

                return props;
            });

            if (!_isTrackable)
            {
                return;
            }

            //ITransaction sentryTrace = CreateTrace(
            //    eventName,
            //    properties.ToDictionary(
            //        prop => prop.key,
            //        prop => prop.value));
            //Instance.FinishTrace(sentryTrace);

            if (properties.Length == 0)
            {
#if ENABLE_FIREBASE
                FirebaseAnalytics.LogEvent(eventName.Replace("/", "_"));
#endif
                return;
            }

#if ENABLE_FIREBASE
            var firebaseParameters = new Parameter[properties.Length];
#endif
            for (var i = 0; i < properties.Length; i++)
            {
                var (key, value) = properties[i];
#if ENABLE_FIREBASE
                firebaseParameters[i] = new Parameter(key.Replace("/", "_"), value);
#endif
            }

#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(eventName.Replace("/", "_"), firebaseParameters);
#endif
        }

        public ITransaction? Track(
            string eventName,
            Dictionary<string, AnalyticsValue> valueDict,
            bool returnTrace = false)
        {
            SendTelemetry(eventName, () => valueDict?.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToString()));

            if (!_isTrackable)
            {
                return null;
            }

            // var sentryTrace = CreateTrace(
            //     eventName,
            //     valueDict.ToDictionary(
            //         item => item.Key,
            //         item => item.Value.ToString()));

#if ENABLE_FIREBASE
            var firebaseParameters = ValuesToParameters(valueDict);
            FirebaseAnalytics.LogEvent(eventName.Replace("/", "_"), firebaseParameters);
#endif

            // if (returnTrace)
            // {
            //     return sentryTrace;
            // }
            //
            // Instance.FinishTrace(sentryTrace);
            return null;
        }

        /// <summary>
        /// Hands tracked failure events to the telemetry sender with the event-time RPC host and
        /// avatar. Telemetry must never break the caller, so every failure is swallowed here.
        /// </summary>
        private void SendTelemetry(string eventName, Func<Dictionary<string, string>?> props)
        {
            if (_telemetry is null || !TelemetryEvents.Tracked.Contains(eventName))
            {
                return;
            }

            try
            {
                var avatarAddress = States.Instance?.CurrentAvatarState?.address;
                _telemetry.Enqueue(
                    eventName,
                    props(),
                    Game.Game.instance.CurrentRpcHost,
                    avatarAddress is null
                        ? null
                        : "0x" + avatarAddress.Value.ToHex().ToLowerInvariant());
            }
            catch (Exception e)
            {
                NcDebug.LogWarning($"[Analyzer] telemetry enqueue failed: {e.GetType().Name}");
            }
        }

        public void Flush()
        {
            if (!_isTrackable)
            {
                return;
            }

        }

        private static void UpdateAvatarAddress()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
#if ENABLE_FIREBASE
                FirebaseAnalytics.SetUserProperty("AvatarAddress", string.Empty);
#endif
                return;
            }

#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("AvatarAddress", avatarState.address.ToHex());
#endif
        }

#if ENABLE_FIREBASE
        private static Parameter ValueToParameter(KeyValuePair<string, AnalyticsValue> item)
        {
            var (key, value) = item;
            var str = value.ToString();
            return new Parameter(key.Replace("/", "_"), str);
        }

        private static Parameter[] ValuesToParameters(Dictionary<string, AnalyticsValue> values)
        {
            var parameters = new Parameter[values.Count];
            var i = 0;
            foreach (var pair in values)
            {
                parameters[i++] = ValueToParameter(pair);
            }

            return parameters;
        }
#endif
    }
}
