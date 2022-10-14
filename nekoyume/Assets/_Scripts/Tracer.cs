using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

using Sentry;


namespace Nekoyume
{
    public class Tracer
    {
        public static Tracer Instance => Game.Game.instance.Tracer;

        private readonly bool _isTrackable;

        public Tracer(string uniqueId = "none", bool isTrackable = false)
        {
            _isTrackable = isTrackable;
            if (!isTrackable)
            {
                Debug.Log($"Sentry Tracer does not trace: {nameof(isTrackable)} is false");
                return;
            }
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new User()
                {
                    Id = uniqueId
                };
            });

            Debug.Log($"Sentry Tracer Initialized: {uniqueId}");
        }

        public ITransaction Create(string eventName, Dictionary<string, string> properties)
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

        public void Finish(ITransaction transaction)
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
            var tx = Create(eventName, properties);
            Finish(tx);
        }

        public void Trace(string eventName)
        {
            if (!_isTrackable)
            {
                return;
            }
            var tx = Create(eventName, new Dictionary<string, string>());
            Finish(tx);
        }
    }

}
