using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bencodex;
using Cocona;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Core;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.DevExtensions
{
    public static class Utils
    {
        public static Logger ConfigureLogger(bool verbose)
        {
            var logConfig = new LoggerConfiguration();
            if (verbose)
            {
                logConfig = logConfig.WriteTo.Console();
            }
            return logConfig.CreateLogger();
        }

        public static (
            BlockChain<NCAction> Chain,
            IStore Store,
            IKeyValueStore StateKVStore,
            IStateStore StateStore
        ) GetBlockChain(
            ILogger logger,
            string storePath,
            Guid? chainId = null,
            IKeyValueStore stateKeyValueStore = null
        )
        {
            var policySource = new BlockPolicySource(logger);
            IBlockPolicy<NCAction> policy = policySource.GetPolicy();
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            IStore store = new RocksDBStore(storePath);
            if (stateKeyValueStore is null)
            {
                stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            }
            IStateStore stateStore = new TrieStateStore(stateKeyValueStore);
            Guid chainIdValue
                = chainId ??
                  store.GetCanonicalChainId() ??
                  throw new CommandExitedException(
                      "No canonical chain ID.  Available chain IDs:\n    " +
                      string.Join<Guid>("\n    ", store.ListChainIds()),
                      1);

            BlockHash genesisBlockHash;
            try
            {
                genesisBlockHash = store.IterateIndexes(chainIdValue).First();
            }
            catch (InvalidOperationException)
            {
                throw new CommandExitedException(
                    $"The chain {chainIdValue} seems empty; try with another chain ID:\n    " +
                        string.Join<Guid>("\n    ", store.ListChainIds()),
                    1
                );
            }
            Block<NCAction> genesis = store.GetBlock<NCAction>(
                genesisBlockHash
            );
            BlockChain<NCAction> chain = new BlockChain<NCAction>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis
            );
            return (chain, store, stateKeyValueStore, stateStore);
        }

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

        public static Block<NCAction> ParseBlockOffset(
            BlockChain<NCAction> chain,
            string blockHashOrIndex,
            long defaultIndex = -1)
        {
            if (!(blockHashOrIndex is string blockStr))
            {
                return chain[defaultIndex];
            }

            if (long.TryParse(blockStr, out long idx) ||
                blockStr.StartsWith("#") && long.TryParse(blockStr.Substring(1), out idx))
            {
                try
                {
                    return chain[idx];
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new CommandExitedException($"No such block index: {idx}.", 1);
                }
            }

            BlockHash blockHash = ParseBlockHash(blockStr);
            return chain[blockHash];
        }

        public static Dictionary<string, string> ImportSheets(string dir = null)
        {
            if (dir is null)
            {
                dir = Path
                    .GetFullPath($"..{Path.DirectorySeparatorChar}")
                    .Replace(
                        $".Lib9c.DevExtensions.Tests{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}",
                        $"Lib9c{Path.DirectorySeparatorChar}TableCSV{Path.DirectorySeparatorChar}");
            }
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
            var sheets = new Dictionary<string, string>();
            foreach (var filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".csv"))
                {
                    fileName = fileName.Replace(".csv", "");
                }

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

        public static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            Bencodex.Types.Dictionary dict = block.MarshalBlock();
            byte[] encoded = new Codec().Encode(dict);
            File.WriteAllBytes(path, encoded);
        }

        public static void ExportKeys(List<ActivationKey> keys, string path)
        {
            File.WriteAllLines(path, keys.Select(v => v.Encode()));
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
