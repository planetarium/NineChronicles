using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nekoyume.Telemetry
{
    public static class TelemetryPayload
    {
        public static string Serialize(
            string batchId,
            long ts,
            string agentAddress,
            string planet,
            string platform,
            string version,
            IReadOnlyList<TelemetryEvent> events)
        {
            var arr = new JArray();
            foreach (var e in events)
            {
                arr.Add(new JObject
                {
                    ["name"] = e.Name,
                    ["ts"] = e.Ts,
                    ["rpc"] = e.Rpc,
                    ["avatarAddress"] = e.AvatarAddress,
                    ["props"] = JObject.FromObject(e.Props ?? new Dictionary<string, string>()),
                });
            }

            var o = new JObject
            {
                ["v"] = 1,
                ["batchId"] = batchId,
                ["ts"] = ts,
                ["agentAddress"] = agentAddress,
                ["client"] = new JObject
                {
                    ["planet"] = planet,
                    ["platform"] = platform,
                    ["version"] = version,
                },
                ["events"] = arr,
            };
            return o.ToString(Formatting.None);
        }
    }
}
