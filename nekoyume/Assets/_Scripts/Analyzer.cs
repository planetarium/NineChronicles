#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define ENABLE_FIREBASE
#endif
#nullable enable

using System.Collections.Generic;
using mixpanel;
using Nekoyume.State;

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

        public Analyzer(
            string? uniqueId = null,
            string? planetId = null,
            string? rpcServerHost = null,
            bool isTrackable = false)
        {
            _isTrackable = isTrackable;
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

            InitializeMixpanel(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost);
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

            Game.Event.OnRoomEnter.AddListener(_ => UpdateAvatarAddress());

            NcDebug.Log($"Analyzer initialized: {uniqueId}");
        }

        public static void SetAgentAddress(string? addressString)
        {
            if (addressString is not null)
            {
                Mixpanel.Identify(addressString);
#if ENABLE_FIREBASE
                FirebaseAnalytics.SetUserId(addressString);
#endif
            }

            addressString ??= "none";

            Mixpanel.Register("AgentAddress", addressString);
            Mixpanel.People.Set("AgentAddress", addressString);
            Mixpanel.People.Name = addressString;
            // SentrySdk.ConfigureScope(scope => { scope.User.Id = uniqueId; });
#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("AgentAddress", addressString);
#endif
        }

        public static void SetPlanetId(string? planetId)
        {
            NcDebug.Log($"[Analyzer] SetPlanetId() invoked with {planetId}");
            planetId ??= "no-planet-id";

            Mixpanel.Register("planet-id", planetId);
            // SentrySdk.ConfigureScope(scope => scope.SetTag("planet-id", planetId));
#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("planet_id", planetId);
#endif
        }

        private static void InitializeMixpanel(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost)
        {
            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Register("client-host", clientHost);
            Mixpanel.Register("client-hash", clientHash);
            Mixpanel.Register("target-network", targetNetwork);
            Mixpanel.Register("rpc-server-host", rpcServerHost);
            Mixpanel.Init();
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
                Mixpanel.Track(eventName);
#if ENABLE_FIREBASE
                FirebaseAnalytics.LogEvent(eventName.Replace("/", "_"));
#endif
                return;
            }

            var mixpanelValues = new Value();
#if ENABLE_FIREBASE
            var firebaseParameters = new Parameter[properties.Length];
#endif
            for (var i = 0; i < properties.Length; i++)
            {
                var (key, value) = properties[i];
                mixpanelValues[key] = value;
#if ENABLE_FIREBASE
                firebaseParameters[i] = new Parameter(key.Replace("/", "_"), value);
#endif
            }

            Mixpanel.Track(eventName, mixpanelValues);
#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(eventName.Replace("/", "_"), firebaseParameters);
#endif
        }

        public ITransaction? Track(
            string eventName,
            Dictionary<string, Value> valueDict,
            bool returnTrace = false)
        {
            if (!_isTrackable)
            {
                return null;
            }

            // var sentryTrace = CreateTrace(
            //     eventName,
            //     valueDict.ToDictionary(
            //         item => item.Key,
            //         item => item.Value.ToString()));

            var mixpanelValue = new Value(valueDict);
            Mixpanel.Track(eventName, mixpanelValue);

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

        public void Flush()
        {
            if (!_isTrackable)
            {
                return;
            }

            Mixpanel.Flush();
        }

        private static void UpdateAvatarAddress()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                Mixpanel.Register("AvatarAddress", string.Empty);
#if ENABLE_FIREBASE
                FirebaseAnalytics.SetUserProperty("AvatarAddress", string.Empty);
#endif
                return;
            }

            Mixpanel.Register("AvatarAddress", avatarState.address.ToHex());
#if ENABLE_FIREBASE
            FirebaseAnalytics.SetUserProperty("AvatarAddress", avatarState.address.ToHex());
#endif
        }

#if ENABLE_FIREBASE
        private static Parameter ValueToParameter(KeyValuePair<string, Value> item)
        {
            var (key, value) = item;
            var str = value.ToString();
            return new Parameter(key.Replace("/", "_"), str);
        }

        private static Parameter[] ValuesToParameters(Dictionary<string, Value> values)
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
