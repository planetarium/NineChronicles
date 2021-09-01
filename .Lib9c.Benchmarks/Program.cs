using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource(Log.Logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy =
                policySource.GetPolicy(BlockPolicySource.DifficultyBoundDivisor + 1, 10000);
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            var store = new RocksDBStore(storePath);
            if (!(store.GetCanonicalChainId() is Guid chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(store.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            DateTimeOffset started = DateTimeOffset.UtcNow;
            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            IKeyValueStore stateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "state_hashes")),
                stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            var stateStore = new TrieStateStore(stateKeyValueStore, stateRootKeyValueStore);
            var chain = new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis);
            long height = chain.Tip.Index;
            BlockHash[] blockHashes = limit < 0
                ? chain.BlockHashes.SkipWhile((_, i) => i < height + limit).ToArray()
                : chain.BlockHashes.Take(limit).ToArray();
            Console.Error.WriteLine(
                "Executing {0} blocks: {1}-{2} (inclusive).",
                blockHashes.Length,
                blockHashes[0],
                blockHashes.Last()
            );
            Block<NCAction>[] blocks = blockHashes.Select(h => chain[h]).ToArray();
            DateTimeOffset blocksLoaded = DateTimeOffset.UtcNow;
            long txs = 0;
            long actions = 0;
            var txId = new TxId(ByteUtil.ParseHex("73042779a14e38d3c4965217079bceb421c14cd6de201170fb40ab3831c0802f"));
            var block = chain[2219377];
            if (block.Transactions.Any(t => t.Id.Equals(txId)))
            {
                Log.Information(block.Index.ToString());
                var tx = block.Transactions.First(t => t.Id.Equals(txId));
                var action = (Buy)tx.Actions.First().InnerAction;
                var purchaseInfo = action.purchaseInfos.First();
                var orderAddress = Order.DeriveAddress(purchaseInfo.OrderId);
                Log.Information(orderAddress.ToHex());
                var order = (FungibleOrder)OrderFactory.Deserialize((Dictionary)chain.GetState(orderAddress));
                Log.Information($"{order.Price}, {order.ItemSubType}, {order.StartedBlockIndex}, {order.ItemCount}, {order.OrderId}");
                var inventoryAddress = purchaseInfo.SellerAvatarAddress.Derive("inventory");
                var inventory = new Inventory((List)chain.GetState(inventoryAddress));
                var sellerAvatarState = new AvatarState((Dictionary)chain.GetState(purchaseInfo.SellerAvatarAddress));

                Address digestListAddress = OrderDigestListState.DeriveAddress(purchaseInfo.SellerAvatarAddress);
                var digestList = new OrderDigestListState((Dictionary)chain.GetState(digestListAddress));
                sellerAvatarState.inventory.ReconfigureFungibleItem(digestList, purchaseInfo.TradableId);

                sellerAvatarState.inventory = inventory;
                int errorCode;
                if (inventory.TryGetLockedItem(new OrderLock(order.OrderId), out var _))
                {
                    Log.Information("Locked");
                    errorCode = order.ValidateTransfer(sellerAvatarState, purchaseInfo.TradableId, purchaseInfo.Price,
                        block.Index);
                }
                else
                {
                    errorCode = order.ValidateTransfer2(sellerAvatarState, purchaseInfo.TradableId, purchaseInfo.Price,
                        block.Index);
                }

                Log.Information(errorCode.ToString());
                foreach (var item in inventory.Items.Where(i => i.Locked))
                {
                    var orderLock = (OrderLock)item.Lock;
                    Log.Information(orderLock.OrderId.ToString());
                    var lockedAddress = Order.DeriveAddress(orderLock.OrderId);
                    Log.Information(lockedAddress.ToHex());
                    var locked = (FungibleOrder)OrderFactory.Deserialize((Dictionary)chain.GetState(lockedAddress));
                    Log.Information($"{locked.Price}, {locked.ItemSubType}, {locked.StartedBlockIndex}, {locked.ItemCount}, {locked.OrderId}");
                }

                foreach (var item in inventory.Items.Where(i => i.item.ItemSubType == ItemSubType.ApStone && i.item is TradableMaterial))
                {
                    Log.Information($"{item.count}");
                }


                var selledBlock = chain[2208001];
                var sellCount = 0;
                foreach (var trx in selledBlock.Transactions)
                {
                    if (trx.Actions.First().InnerAction is Sell sell && trx.Signer.Equals(sellerAvatarState.agentAddress))
                    {
                        Log.Information($"{sell.orderId}, {sell.itemSubType}, {sell.sellerAvatarAddress}, {sell.count}");
                        sellCount += sell.count;
                    }
                }
                Log.Information(sellCount.ToString());

                var digestListState =
                    new OrderDigestListState((Dictionary)chain.GetState(OrderDigestListState.DeriveAddress(sellerAvatarState.address)));
                var count = 0;
                foreach (var digest in digestListState.OrderDigestList)
                {
                    if (digest.ItemId == 500000)
                    {
                        count += digest.ItemCount;
                    }
                    Log.Information(digest.OrderId.ToString());
                }
                Log.Information(count.ToString());
            }
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
