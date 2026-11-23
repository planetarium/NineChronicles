using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Telemetry
{
    public class TelemetryEvent
    {
        public string Name;
        public long Ts;
        public string Rpc;
        public string AvatarAddress;
        public Dictionary<string, string> Props = new Dictionary<string, string>();
    }

    public static class TelemetryEvents
    {
        public const string UnityError = "Unity/Error";
        public const string RpcDisconnected = "Unity/RPC Disconnected";
        public const string AgentInitializeFailed = "Unity/Intro/Start/AgentInitializeFailed";
        public const string PurchaseClickedError = "Unity/Shop/IAP/OnPurchaseClicked/Error";
        public const string ProcessPurchaseError = "Unity/Shop/IAP/ProcessPurchase/Error";
        public const string StateWaitTimeout = "Unity/Shop/IAP/ProcessPurchase/StateWaitTimeout";

        public static readonly IReadOnlyList<string> Tracked = new[]
        {
            UnityError,
            RpcDisconnected,
            AgentInitializeFailed,
            PurchaseClickedError,
            ProcessPurchaseError,
            StateWaitTimeout,
        };

        public static readonly IReadOnlyList<string> Unsigned = new[] { AgentInitializeFailed };

        private static readonly Dictionary<string, string[]> AllowedProps =
            new Dictionary<string, string[]>
            {
                { UnityError, new[] { "code", "key" } },
                { PurchaseClickedError, new[] { "productId", "error" } },
                { ProcessPurchaseError, new[] { "productId", "error" } },
                { StateWaitTimeout, new[] { "productId" } },
            };

        public static Dictionary<string, string> FilterProps(
            string name,
            IDictionary<string, string> props)
        {
            var result = new Dictionary<string, string>();
            if (props == null || !AllowedProps.TryGetValue(name ?? string.Empty, out var allowed))
            {
                return result;
            }

            foreach (var kv in props.Where(kv => allowed.Contains(kv.Key)))
            {
                result[kv.Key] = kv.Value;
            }

            return result;
        }
    }
}
