using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Bencodex;
using Bencodex.Types;
using Cocona;
using Lib9c.DevExtensions;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Nekoyume.Action;
using Serilog.Core;

namespace Lib9c.Tools.SubCommand
{
    public class State
    {
        private static readonly Codec _codec = new Codec();

        [Obsolete("This function is deprecated. Please use `NineChronicles.Headless.Executable state rebuild` command instead.")]
        [Command(Description = "Rebuild entire states by executing the chain from the genesis.")]
        public void Rebuild(
            [Option('v', Description = "Print more logs.")]
            bool verbose,
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
            [Option('c', Description = "Optional chain ID.  Default is the canonical chain ID.")]
            Guid? chainId = null,
            [Option(
                't',
                Description = "Optional topmost block to execute last.  Can be either a block " +
                    "hash or block index.  Tip by default.")]
            string topmost = null,
            [Option(
                new char[] { 'f', 'B' },
                Description = "Optional bottommost block to execute first.  Can be either a " +
                    "block hash or block index.  Genesis by default.")]
            string bottommost = null,
            [Option('b', Description = "Bypass the state root hash check.")]
            bool bypassStateRootHashCheck = false,
            [Option(
                'm',
                Description = "Use the in-memory key-value state store and dump it to " +
                    "the specified directory path in the end.",
                ValueName = "DIR")]
            string useMemoryKvStore = null
        )
        {
            using Logger logger = Utils.ConfigureLogger(verbose);
            CancellationToken cancellationToken = GetInterruptSignalCancellationToken();
            TextWriter stderr = Console.Error;
            (
                BlockChain chain,
                IStore store,
                IKeyValueStore stateKvStore,
                IStateStore stateStore
            ) = Utils.GetBlockChain(
                logger,
                storePath,
                chainId,
                useMemoryKvStore is string p ? new MemoryKeyValueStore(p, stderr) : null
            );
            Block bottom = Utils.ParseBlockOffset(chain, bottommost, 0);
            Block top = Utils.ParseBlockOffset(chain, topmost);

            stderr.WriteLine("It will execute all actions (tx actions & block actions)");
            stderr.WriteLine(
                "  ...from the block #{0} {1}",
                bottom.Index.ToString(CultureInfo.InvariantCulture).PadRight(
                    top.Index.ToString(CultureInfo.InvariantCulture).Length),
                bottom.Hash);
            stderr.WriteLine("    ...to the block #{0} {1}.", top.Index, top.Hash);

            IBlockPolicy policy = chain.Policy;
            (Block, string)? invalidStateRootHashBlock = null;
            long totalBlocks = top.Index - bottom.Index + 1;
            long blocksExecuted = 0L;
            long txsExecuted = 0L;
            DateTimeOffset started = DateTimeOffset.Now;
            IEnumerable<BlockHash> blockHashes = store.IterateIndexes(
                chain.Id,
                (int)bottom.Index,
                (int)(top.Index - bottom.Index + 1L)
            );
            foreach (BlockHash blockHash in blockHashes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new CommandExitedException(1);
                }

                Block block =
                    store.GetBlock(blockHash);
                var preEvalBlock = new PreEvaluationBlock(
                    block, block.Transactions
                );
                stderr.WriteLine(
                    "[{0}/{1}] Executing block #{2} {3}...",
                    block.Index - bottom.Index + 1L,
                    top.Index - bottom.Index + 1L,
                    block.Index,
                    block.Hash
                );
                IReadOnlyList<IActionEvaluation> delta;
                var actionLoader = TypedActionLoader.Create(
                    typeof(ActionBase).Assembly, typeof(ActionBase));
                var actionEvaluator = new ActionEvaluator(
                    _ => policy.BlockAction,
                    new BlockChainStates(store, stateStore),
                    actionLoader);
                HashDigest<SHA256> stateRootHash = block.Index < 1
                    ? BlockChain.DetermineGenesisStateRootHash(
                        actionEvaluator,
                        preEvalBlock,
                        out delta)
                    : chain.DetermineBlockStateRootHash(
                        preEvalBlock,
                        out delta);
                DateTimeOffset now = DateTimeOffset.Now;
                if (invalidStateRootHashBlock is null && !stateRootHash.Equals(block.StateRootHash))
                {
                    string blockDump = DumpBencodexToFile(
                        block.MarshalBlock(),
                        $"block_{block.Index}_{block.Hash}"
                    );
                    string deltaDump = DumpBencodexToFile(
                        new Dictionary(GetTotalDelta(
                            delta,
                            ToStateKey,
                            ToFungibleAssetKey,
                            ToTotalSupplyKey,
                            ValidatorSetKey)),
                        $"delta_{block.Index}_{block.Hash}"
                    );
                    string message =
                        $"Unexpected state root hash for block #{block.Index} {block.Hash}.\n" +
                        $"  Expected: {block.StateRootHash}\n  Actual:   {stateRootHash}\n" +
                        $"  Block file: {blockDump}\n  Evaluated delta file: {deltaDump}\n";
                    if (!bypassStateRootHashCheck)
                    {
                        throw new CommandExitedException(message, 1);
                    }

                    stderr.WriteLine(message);
                    invalidStateRootHashBlock = (block, message);
                }

                blocksExecuted++;
                txsExecuted += block.Transactions.Count;
                TimeSpan elapsed = now - started;

                if (blocksExecuted >= totalBlocks || block.Hash.Equals(top.Hash))
                {
                    stderr.WriteLine("Elapsed: {0:c}.", elapsed);
                    break;
                }
                else
                {
                    TimeSpan estimatedRemaining =
                        elapsed / blocksExecuted * (totalBlocks - blocksExecuted);
                    stderr.WriteLine(
                        "Elapsed: {0:c}, estimated remaining: {1:c}.",
                        elapsed,
                        estimatedRemaining
                    );
                }
            }

            if (invalidStateRootHashBlock is { } b)
            {
                stderr.WriteLine(
                    "Note that the state root hash check is bypassed, " +
                    "but there was an invalid state root hash for block #{0} {1}.  {2}",
                    b.Item1.Index,
                    b.Item1.Hash,
                    b.Item2
                );
            }

            TimeSpan totalElapsed = DateTimeOffset.Now - started;
            stderr.WriteLine("Total elapsed: {0:c}", totalElapsed);
            stderr.WriteLine("Avg block execution time: {0:c}", totalElapsed / totalBlocks);
            stderr.WriteLine("Avg tx execution time: {0:c}", totalElapsed / txsExecuted);
            stateKvStore.Dispose();
            stateStore.Dispose();
        }

        [Obsolete("This function is deprecated. Please use `NineChronicles.Headless.Executable state check` command instead.")]
        [Command(Description = "Check if states for the specified block are available " +
            "in the state store.")]
        public void Check(
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
            [Argument(
                Description = "A block to check.  Can be either a block hash or block index.  " +
                    "Tip by default.")]
            string block = null,
            [Option('c', Description = "Optional chain ID.  Default is the canonical chain ID.")]
            Guid? chainId = null,
            [Option('v', Description = "Print more logs.")]
            bool verbose = false
        )
        {
            using Logger logger = Utils.ConfigureLogger(verbose);
            CancellationToken cancellationToken = GetInterruptSignalCancellationToken();
            TextWriter stderr = Console.Error;
            (
                BlockChain chain,
                IStore store,
                IKeyValueStore stateKvStore,
                IStateStore stateStore
            ) = Utils.GetBlockChain(
                logger,
                storePath,
                chainId
            );
            Block checkBlock = Utils.ParseBlockOffset(chain, block);
            HashDigest<SHA256> stateRootHash = checkBlock.StateRootHash;
            ITrie stateRoot = stateStore.GetStateRoot(stateRootHash);
            bool exist = stateRoot.Recorded;
            Console.WriteLine(
                exist
                    ? "Block #{0} {1} has states in the state store."
                    : "Block #{0} {1} does not have states in the state store.",
                checkBlock.Index,
                checkBlock.Hash
            );
            Console.WriteLine("State root hash: {0}", stateRootHash);
            if (exist)
            {
                return;
            }

            logger.Information("Finding the latest ancestor block having its states...");

            bool WillGoFurther(Block b)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new CommandExitedException(1);
                }
                else if (stateStore.GetStateRoot(b.StateRootHash).Recorded)
                {
                    logger.Information("#{0} {1} has states.", b.Index, b.Hash);
                    return false;
                }

                logger.Information("#{0} {1} has no states.", b.Index, b.Hash);
                return true;
            }

            if (BisectBlocks(chain, checkBlock.Index, 0, WillGoFurther) is { } upper)
            {
                Console.WriteLine(
                    "\nThe latest ancestor block that has states in the state store is:\n" +
                    "Block #{0} {1}\nState root hash: {2}",
                    upper.Index,
                    upper.Hash,
                    stateRootHash
                );
            }

            if (BisectBlocks(chain, checkBlock.Index, chain.Tip.Index, WillGoFurther) is { } lower)
            {
                Console.WriteLine(
                    "\nThe earliest descendant block that has states in the state store is:\n" +
                    "Block #{0} {1}\nState root hash: {2}",
                    lower.Index,
                    lower.Hash,
                    stateRootHash
                );
            }

            throw new CommandExitedException(1);
        }

        private static Block BisectBlocks(
            BlockChain chain,
            long start,
            long end,
            Predicate<Block> willGoFurther
        )
        {
            long tip = chain.Tip.Index;
            while (start != end)
            {
                long upper = Math.Max(start, end);
                long lower = Math.Min(start, end);
                long dir = (end > start ? 1L : -1L);
                long idx = lower + (upper - lower) / 2L;
                idx = Math.Min(upper, idx);
                idx = Math.Max(lower, idx);

                Block b = chain[idx];
                if (willGoFurther(b))
                {
                    long nextStart = idx == end ? idx + dir : idx;
                    nextStart = Math.Min(nextStart, tip);
                    nextStart = Math.Max(nextStart, 0L);
                    start = nextStart + (nextStart == start ? dir : 0L);
                }
                else
                {
                    long nextEnd = idx == start ? idx + dir : idx;
                    nextEnd = Math.Min(nextEnd, tip);
                    nextEnd = Math.Max(nextEnd, 0L);
                    end = nextEnd - (nextEnd == end ? dir : 0L);
                }
            }

            return willGoFurther(chain[start]) ? null : chain[start];
        }

        private static CancellationToken GetInterruptSignalCancellationToken()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            return cts.Token;
        }

        private static string DumpBencodexToFile(IValue value, string name)
        {
            string path = Path.Join(Path.GetTempPath(), $"{name}.dat");
            using FileStream stream = File.OpenWrite(path);
            _codec.Encode(value, stream);
            return path;
        }

        private class MemoryKeyValueStore : IKeyValueStore
        {
            private readonly ConcurrentDictionary<KeyBytes, byte[]> _dictionary;
            private readonly string _dumpPath;
            private readonly TextWriter _messageWriter;
            private bool _disposed;

            public MemoryKeyValueStore(string dumpPath, TextWriter messageWriter)
            {
                _dictionary = new ConcurrentDictionary<KeyBytes, byte[]>();
                _dumpPath = dumpPath;
                _messageWriter = messageWriter;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _messageWriter.WriteLine("Dump in-memory key-value store to {0}...", _dumpPath);
                Directory.CreateDirectory(_dumpPath);
                _messageWriter.WriteLine("Total keys: {0}", _dictionary.Count);
                long i = 0;
                foreach (KeyValuePair<KeyBytes, byte[]> pair in _dictionary)
                {
                    string subDir = Path.Join(
                        _dumpPath,
                        pair.Key.Length.ToString(),
                        pair.Key.Length > 0 ? $"{pair.Key.ByteArray[0]:x2}" : "_"
                    );
                    Directory.CreateDirectory(subDir);
                    File.WriteAllBytes(
                        Path.Join(subDir, pair.Key.Hex), pair.Value);
                    if (i++ % 1000 == 0)
                    {
                        _messageWriter.Write('.');
                    }
                }
            }

            public byte[] Get(in KeyBytes key) => _dictionary[key];

            public IReadOnlyDictionary<KeyBytes, byte[]> Get(IEnumerable<KeyBytes> keys)
            {
                var dictBuilder = ImmutableDictionary.CreateBuilder<KeyBytes, byte[]>();
                foreach (KeyBytes key in keys)
                {
                    if (_dictionary.TryGetValue(key, out var value) && value is { } v)
                    {
                        dictBuilder[key] = v;
                    }
                }

                return dictBuilder.ToImmutable();
            }

            public void Set(in KeyBytes key, byte[] value) =>
                _dictionary[key] = value;

            public void Set(IDictionary<KeyBytes, byte[]> values)
            {
                foreach (KeyValuePair<KeyBytes, byte[]> kv in values)
                {
                    _dictionary[kv.Key] = kv.Value;
                }
            }

            public void Delete(in KeyBytes key) =>
                _dictionary.TryRemove(key, out _);

            public void Delete(IEnumerable<KeyBytes> keys)
            {
                foreach (KeyBytes key in keys)
                {
                    _dictionary.TryRemove(key, out _);
                }
            }

            public bool Exists(in KeyBytes key) =>
                _dictionary.ContainsKey(key);

            IEnumerable<KeyBytes> IKeyValueStore.ListKeys() =>
                _dictionary.Keys;
        }

        private static ImmutableDictionary<string, IValue> GetTotalDelta(
            IReadOnlyList<IActionEvaluation> actionEvaluations,
            Func<Address, string> toStateKey,
            Func<(Address, Currency), string> toFungibleAssetKey,
            Func<Currency, string> toTotalSupplyKey,
            string validatorSetKey)
        {
            IImmutableSet<Address> stateUpdatedAddresses = actionEvaluations
                .SelectMany(a => a.OutputState.Delta.StateUpdatedAddresses)
                .ToImmutableHashSet();
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = actionEvaluations
                .SelectMany(a => a.OutputState.Delta.UpdatedFungibleAssets)
                .ToImmutableHashSet();
            IImmutableSet<Currency> updatedTotalSupplies = actionEvaluations
                .SelectMany(a => a.OutputState.Delta.UpdatedTotalSupplyCurrencies)
                .ToImmutableHashSet();

            IAccountStateDelta lastStates = actionEvaluations.Count > 0
                ? actionEvaluations[actionEvaluations.Count - 1].OutputState
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

            foreach (var currency in updatedTotalSupplies)
            {
                if (lastStates?.GetTotalSupply(currency).RawValue is { } rawValue)
                {
                    totalDelta = totalDelta.SetItem(
                        toTotalSupplyKey(currency),
                        new Bencodex.Types.Integer(rawValue)
                    );
                }
            }

            if (lastStates?.GetValidatorSet() is { } validatorSet && validatorSet.Validators.Any())
            {
                totalDelta = totalDelta.SetItem(
                    validatorSetKey,
                    validatorSet.Bencoded
                );
            }

            return totalDelta;
        }

        private const string ValidatorSetKey = "___";

        private static string ToStateKey(Address address) => ByteUtil.Hex(address.ByteArray);

        private static string ToFungibleAssetKey(Address address, Currency currency) =>
            "_" + ByteUtil.Hex(address.ByteArray) +
            "_" + ByteUtil.Hex(currency.Hash.ByteArray);

        private static string ToFungibleAssetKey((Address, Currency) pair) =>
            ToFungibleAssetKey(pair.Item1, pair.Item2);

        private static string ToTotalSupplyKey(Currency currency) =>
            "__" + ByteUtil.Hex(currency.Hash.ByteArray);
    }
}
