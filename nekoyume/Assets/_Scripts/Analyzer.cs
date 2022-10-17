using System.Collections.Generic;
using Sentry;
using UnityEngine;

namespace Nekoyume
{
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

            var clientHost = Resources.Load<TextAsset>("Sentry/ClientHost")?.text ?? "no-host";
            var clientHash = Resources.Load<TextAsset>("Sentry/ClientHash")?.text ?? "no-hash";
            var targetNetwork =
                Resources.Load<TextAsset>("Sentry/TargetNetwork")?.text ?? "no-target";
            var agentAddress = uniqueId;

            SentrySdk.ConfigureScope(scope =>
            {
                // Global scope: always tag for every transaction
                scope.User = new User()
                {
                    Id = uniqueId
                };
                scope.SetTag("client-host", clientHost);
                scope.SetTag("client-hash", clientHash);
                scope.SetTag("target-network", targetNetwork);
                scope.SetTag("rpc-server-host", rpcServerHost);
                scope.SetTag("AgentAddress", agentAddress);
            });

            Debug.Log($"Analyzer initialized: {uniqueId}");
        }

        public ITransaction CreateTrace(string eventName, Dictionary<string, string> properties)
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

        public void Trace(string eventName, Dictionary<string, string> properties)
        {
            if (!_isTrackable)
            {
                return;
            }
            var tx = CreateTrace(eventName, properties);
            FinishTrace(tx);
        }

        public void Trace(string eventName)
        {
            if (!_isTrackable)
            {
                return;
            }
            var tx = CreateTrace(eventName, new Dictionary<string, string>());
            FinishTrace(tx);
        }
    }
}
