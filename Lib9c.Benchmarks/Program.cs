using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Nekoyume.BlockChain;
using Serilog;
using Serilog.Events;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Too few arguments.");
                Environment.Exit(1);
                return;
            }

            string storePath = args[0];
            int limit = int.Parse(args[1]);
            ILogger logger = new LoggerConfiguration().CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource(logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy =
                policySource.GetPolicy(BlockPolicySource.DifficultyBoundDivisor + 1, 0);
            var store = new RocksDBStore(storePath);
            if (!(store.GetCanonicalChainId() is Guid chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(store.IndexBlockHash(chainId, 0) is HashDigest<SHA256> gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            DateTimeOffset started = DateTimeOffset.UtcNow;
            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            var chain = new BlockChain<NCAction>(policy, store, store, genesis);
            long height = chain.Tip.Index;
            var blockHashes = limit < 0
                ? chain.BlockHashes.SkipWhile((_, i) => i < height + limit).ToArray()
                : chain.BlockHashes.Take(limit).ToArray();
            Console.Error.WriteLine(
                "Executing {0} blocks: {1}-{2} (inclusive).",
                blockHashes.Length,
                blockHashes[0],
                blockHashes.Last()
            );
            DateTimeOffset blocksLoaded = DateTimeOffset.UtcNow;
            long txs = 0;
            long actions = 0;
            foreach (HashDigest<SHA256> blockHash in blockHashes)
            {
                Block<NCAction> block = chain[blockHash];
                Console.Error.WriteLine(
                    "Block #{0} {1}; {2} txs",
                    block.Index,
                    blockHash,
                    block.Transactions.Count()
                );

                IEnumerable<ActionEvaluation> blockEvals;
                if (block.Index > 0)
                {
                    blockEvals = block.Evaluate(
                        DateTimeOffset.UtcNow,
                        address => chain.GetState(address, blockHash),
                        (address, currency) => chain.GetBalance(address, currency, blockHash)
                    );
                }
                else
                {
                    blockEvals = block.Evaluate(
                        DateTimeOffset.UtcNow,
                        _ => null,
                        ((_, currency) => new FungibleAssetValue(currency))
                    );
                }
                SetStates(chain.Id, store, block, blockEvals.ToArray(), buildStateReferences: true);
                txs += block.Transactions.LongCount();
                actions += block.Transactions.Sum(tx => tx.Actions.LongCount()) + 1;
            }

            DateTimeOffset ended = DateTimeOffset.UtcNow;
            Console.WriteLine("Loading blocks\t{0}", blocksLoaded - started);
            TimeSpan execActions = ended - blocksLoaded;
            Console.WriteLine("Executing actions\t{0}", execActions);
            Console.WriteLine("Average per block\t{0}", execActions / blockHashes.Length);
            Console.WriteLine("Average per tx\t{0}", execActions / txs);
            Console.WriteLine("Average per action\t{0}", execActions / actions);
            Console.WriteLine("Total elapsed\t{0}", ended - started);
        }

        // Copied from BlockChain<T>.SetStates().
        private static void SetStates(
            Guid chainId,
            IStateStore stateStore,
            Block<NCAction> block,
            IReadOnlyList<ActionEvaluation> actionEvaluations,
            bool buildStateReferences
        )
        {
            IImmutableSet<Address> stateUpdatedAddresses = actionEvaluations
                .SelectMany(a => a.OutputStates.StateUpdatedAddresses)
                .ToImmutableHashSet();
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = actionEvaluations
                .SelectMany(a => a.OutputStates.UpdatedFungibleAssets
                    .SelectMany(kv => kv.Value.Select(c => (kv.Key, c))))
                .ToImmutableHashSet();

            if (!stateStore.ContainsBlockStates(block.Hash))
            {
                var totalDelta = GetTotalDelta(actionEvaluations, ToStateKey, ToFungibleAssetKey);
                stateStore.SetStates(block, totalDelta);
            }

            if (buildStateReferences && stateStore is IBlockStatesStore blockStatesStore)
            {
                IImmutableSet<string> stateUpdatedKeys = stateUpdatedAddresses
                    .Select(ToStateKey)
                    .ToImmutableHashSet();
                IImmutableSet<string> assetUpdatedKeys = updatedFungibleAssets
                    .Select(ToFungibleAssetKey)
                    .ToImmutableHashSet();
                IImmutableSet<string> updatedKeys = stateUpdatedKeys.Union(assetUpdatedKeys);
                blockStatesStore.StoreStateReference(chainId, updatedKeys, block.Hash, block.Index);
            }
        }

        // Copied from ActionEvaluationsExtensions.GetTotalDelta().
        private static ImmutableDictionary<string, IValue> GetTotalDelta(
            IReadOnlyList<ActionEvaluation> actionEvaluations,
            Func<Address, string> toStateKey,
            Func<(Address, Currency), string> toFungibleAssetKey)
        {
            IImmutableSet<Address> stateUpdatedAddresses = actionEvaluations
                .SelectMany(a => a.OutputStates.StateUpdatedAddresses)
                .ToImmutableHashSet();
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = actionEvaluations
                .SelectMany(a => a.OutputStates.UpdatedFungibleAssets
                    .SelectMany(kv => kv.Value.Select(c => (kv.Key, c))))
                .ToImmutableHashSet();

            IAccountStateDelta lastStates = actionEvaluations.Count > 0
                ? actionEvaluations[actionEvaluations.Count - 1].OutputStates
                : null;
            ImmutableDictionary<string, IValue> totalDelta =
                stateUpdatedAddresses.ToImmutableDictionary(
                    toStateKey,
                    a => lastStates?.GetState(a)
                ).SetItems(
                    updatedFungibleAssets.Select(pair =>
                        new KeyValuePair<string, IValue>(
                            toFungibleAssetKey(pair),
                            new Bencodex.Types.Integer(
                                lastStates?.GetBalance(pair.Item1, pair.Item2).RawValue ?? 0
                            )
                        )
                    )
                );

            return totalDelta;
        }

        // Copied from KeyConverters.ToStateKey().
        private static string ToStateKey(Address address) => address.ToHex().ToLowerInvariant();

        // Copied from KeyConverters.ToFungibleAssetKey().
        private static string ToFungibleAssetKey(Address address, Currency currency) =>
            "_" + address.ToHex().ToLowerInvariant() +
            "_" + ByteUtil.Hex(currency.Hash.ByteArray).ToLowerInvariant();

        // Copied from KeyConverters.ToFungibleAssetKey().
        private static string ToFungibleAssetKey((Address, Currency) pair) =>
            ToFungibleAssetKey(pair.Item1, pair.Item2);
    }
}
