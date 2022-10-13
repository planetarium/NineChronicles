using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

using Sentry;


namespace Nekoyume
{
    public class Tracer
    {

        public Tracer(string uniqueId = "none")
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new User()
                {
                    Id = uniqueId
                };
            });

            Debug.Log($"Sentry Tracer Initialized: {uniqueId}");
        }

        public static ITransaction Create(string eventName, Dictionary<string, string> properties)
        {
            var transaction = SentrySdk.StartTransaction(eventName, "process action");
            foreach (var (key, val) in properties)
            {
                transaction.SetTag(key, val);
            }
            return transaction;
        }

        public static void Finish(ITransaction transaction)
        {
            transaction.Finish();
        }

        public static void Trace(string eventName, Dictionary<string, string> properties)
        {
            var tx = Create(eventName, properties);
            Finish(tx);
        }

        public static void Trace(string eventName)
        {
            var tx = Create(eventName, new Dictionary<string, string>());
            Finish(tx);
        }
    }

}
