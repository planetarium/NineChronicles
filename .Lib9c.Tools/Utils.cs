using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Lib9c.Tools
{
    public static class Utils
    {
        public static Dictionary<string, string> ImportSheets(string dir)
        {
            var sheets = new Dictionary<string, string>();
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                sheets[fileName] = File.ReadAllText(filePath);
            }

            return sheets;
        }

        public static void CreateActivationKey(
            out List<PendingActivationState> pendingActivationStates,
            out List<ActivationKey> activationKeys,
            uint countOfKeys)
        {
            pendingActivationStates = new List<PendingActivationState>();
            activationKeys = new List<ActivationKey>();

            for (uint i = 0; i < countOfKeys; i++)
            {
                var pendingKey = new PrivateKey();
                var nonce = pendingKey.PublicKey.ToAddress().ToByteArray();
                (ActivationKey ak, PendingActivationState s) =
                    ActivationKey.Create(pendingKey, nonce);
                pendingActivationStates.Add(s);
                activationKeys.Add(ak);
            }
        }

        public static AuthorizedMinersState GetAuthorizedMinersState(string configPath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AuthorizedMinersStateConfig>(json, options);
            return new AuthorizedMinersState(
                miners: config.Miners.Select(addr => new Address(addr)),
                interval: config.Interval,
                validUntil: config.ValidUntil);
        }

        [Serializable]
        public struct AuthorizedMinersStateConfig
        {
            public long Interval { get; set; }

            public List<string> Miners { get; set; }

            public long ValidUntil { get; set; }
        }
    }
}
