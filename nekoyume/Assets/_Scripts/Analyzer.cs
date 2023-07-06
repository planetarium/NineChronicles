using System.Collections.Generic;
using mixpanel;
using Nekoyume.State;
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
            string uniqueId = "none",
            string rpcServerHost = "no-rpc-host",
            bool isTrackable = false)
        {
            _isTrackable = isTrackable;
            if (!_isTrackable)
            {
                Debug.Log($"Analyzer does not track: {nameof(isTrackable)} is false");
                return;
            }

            var clientHost = Resources.Load<TextAsset>("ClientHost")?.text ?? "no-host";
            var clientHash = Resources.Load<TextAsset>("ClientHash")?.text ?? "no-hash";
            var targetNetwork = Resources.Load<TextAsset>("TargetNetwork")?.text ?? "no-target";

            InitializeMixpanel(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost,
                uniqueId);
            InitializeSentry(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost,
                uniqueId);
            UpdateAvatarAddress();

            Game.Event.OnRoomEnter.AddListener(_ => UpdateAvatarAddress());

            Debug.Log($"Analyzer initialized: {uniqueId}");
        }

        private void InitializeMixpanel(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost,
            string uniqueId)
        {
            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Identify(uniqueId);
            Mixpanel.Register("client-host", clientHost);
            Mixpanel.Register("client-hash", clientHash);
            Mixpanel.Register("target-network", targetNetwork);
            Mixpanel.Register("rpc-server-host", rpcServerHost);
            Mixpanel.Register("AgentAddress", uniqueId);
            Mixpanel.People.Set("AgentAddress", uniqueId);
            Mixpanel.People.Name = uniqueId;
            Mixpanel.Init();
        }

        private void InitializeSentry(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost,
            string uniqueId)
        {
            // SentrySdk.ConfigureScope(scope =>
            // {
            //     scope.User = new User()
            //     {
            //         Id = uniqueId
            //     };
            //     scope.SetTag("client-host", clientHost);
            //     scope.SetTag("client-hash", clientHash);
            //     scope.SetTag("target-network", targetNetwork);
            //     scope.SetTag("rpc-server-host", rpcServerHost);
            // });
        }

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
                return;
            }

            var result = new Value();
            foreach (var (key, value) in properties)
            {
                result[key] = value;
            }

            Mixpanel.Track(eventName, result);
        }

        public ITransaction Track(
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

            var value = new Value(valueDict);
            Mixpanel.Track(eventName, value);

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
                Mixpanel.Unregister("AvatarAddress");
                return;
            }

            Mixpanel.Register("AgentAddress", avatarState.address.ToHex());
        }
    }
}
