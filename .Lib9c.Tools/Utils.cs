using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cocona;
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
        public static Address ParseAddress(string address)
        {
            if (address.StartsWith("0x") || address.StartsWith("0X"))
            {
                address = address.Substring(2);
            }

            try
            {
                return new Address(address);
            }
            catch (ArgumentException e)
            {
                throw new CommandExitedException($"{address}: {e.Message}", 1);
            }
        }

        public static BlockHash ParseBlockHash(string blockHash)
        {
            try
            {
                return BlockHash.FromString(blockHash);
            }
            catch (Exception e) when (e is ArgumentOutOfRangeException || e is FormatException)
            {
                throw new CommandExitedException($"{blockHash}: {e.Message}", 1);
            }
        }

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
            var ps = new ConcurrentBag<PendingActivationState>();
            var ks = new ConcurrentBag<ActivationKey>();
            Parallel.For(0, countOfKeys, _ =>
            {
                var pendingKey = new PrivateKey();
                var nonce = pendingKey.PublicKey.ToAddress().ToByteArray();
                (ActivationKey ak, PendingActivationState s) =
                    ActivationKey.Create(pendingKey, nonce);
                ps.Add(s);
                ks.Add(ak);
            });

            pendingActivationStates = ps.ToList();
            activationKeys = ks.ToList();
        }

        public static AuthorizedMinersState GetAuthorizedMinersState(string configPath)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AuthorizedMinersStateConfig>(json, options);
            return new AuthorizedMinersState(
                miners: config.Miners.Select(addr => new Address(addr)),
                interval: config.Interval,
                validUntil: config.ValidUntil);
        }

        public static AdminState GetAdminState(string configPath)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AdminStateConfig>(json, options);
            return new AdminState(
                adminAddress: new Address(config.AdminAddress),
                validUntil: config.ValidUntil);
        }

        public static ImmutableHashSet<Address> GetActivatedAccounts(string listPath)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            string json = File.ReadAllText(listPath);
            ActivatedAccounts activatedAccounts = JsonSerializer.Deserialize<ActivatedAccounts>(json, options);

            return activatedAccounts.Accounts
                .Select(account => new Address(account))
                .ToImmutableHashSet();
        }

        [Serializable]
        private struct AuthorizedMinersStateConfig
        {
            public long Interval { get; set; }

            public List<string> Miners { get; set; }

            public long ValidUntil { get; set; }
        }

        [Serializable]
        private struct AdminStateConfig
        {
            public string AdminAddress { get; set; }

            public long ValidUntil { get; set; }
        }

        [Serializable]
        private struct ActivatedAccounts
        {
            public List<string> Accounts { get; set; }
        }
    }
}
