using System.Collections.Generic;
using System.Linq;
using mixpanel;
using PackageExtensions.Mixpanel;
using Sentry;
using UnityEngine;

namespace Nekoyume
{
    public class Analyzer
    {
        public static Analyzer Instance => Game.Game.instance.Analyzer;

        private readonly bool _isTrackable;

        private readonly MixpanelValueFactory _mixpanelValueFactory;

        public Analyzer(
            string uniqueId = "none",
            string rpcServerHost = null,
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

            InitializeSentry(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost,
                uniqueId,
                _isTrackable);

            _mixpanelValueFactory = new MixpanelValueFactory(
                clientHost,
                clientHash,
                targetNetwork,
                rpcServerHost,
                uniqueId);

            Mixpanel.SetToken("80a1e14b57d050536185c7459d45195a");
            Mixpanel.Identify(uniqueId);
            Mixpanel.Register("AgentAddress", uniqueId);
            Mixpanel.People.Set("AgentAddress", uniqueId);
            Mixpanel.People.Name = uniqueId;
            Mixpanel.Init();

            Debug.Log($"Analyzer initialized: {uniqueId}");
        }

        private void InitializeSentry(
            string clientHost,
            string clientHash,
            string targetNetwork,
            string rpcServerHost = "no-rpc-host",
            string uniqueId = "none",
            bool isTrackable = false)
        {
            if (!isTrackable)
            {
                return;
            }

            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new User()
                {
                    Id = uniqueId
                };
                scope.SetTag("client-host", clientHost);
                scope.SetTag("client-hash", clientHash);
                scope.SetTag("target-network", targetNetwork);
                scope.SetTag("rpc-server-host", rpcServerHost);
            });
        }

        private ITransaction CreateTrace(string eventName, Dictionary<string, string> properties)
        {
            if (!_isTrackable)
            {
                return null;
            }
            var transaction = SentrySdk.StartTransaction(eventName, eventName);
            foreach (var (key, val) in properties)
            {
                transaction.SetTag(key, val);
            }
            return transaction;
        }

        public void FinishTrace(ITransaction transaction)
        {
            if (transaction is not null)
            {
                transaction.Finish();
            }
        }

        public void Track(string eventName, params (string key, string value)[] properties)
        {
            if (!_isTrackable)
            {
                return;
            }

            ITransaction sentryTrace = CreateTrace(
                eventName,
                properties.ToDictionary(
                    prop => prop.key,
                    prop => prop.value));
            Instance.FinishTrace(sentryTrace);

            if (properties.Length == 0)
            {
                Mixpanel.Track(eventName);
                return;
            }

            var value = _mixpanelValueFactory.GetValue(properties);
            Mixpanel.Track(eventName, value);
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

            var sentryTrace =  CreateTrace(
                eventName,
                valueDict.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToString()));

            Value value = new Value(valueDict);

            value = _mixpanelValueFactory.UpdateValue(value);
            Mixpanel.Track(eventName, value);


            if (returnTrace)
            {
                return sentryTrace;
            }
            Instance.FinishTrace(sentryTrace);
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
    }
}
