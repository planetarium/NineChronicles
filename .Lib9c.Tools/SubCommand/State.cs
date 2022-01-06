using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Cocona;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Serilog.Core;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class State
    {
        private static readonly Codec _codec = new Codec();

        [Command("Rebuild the entire states by executing the chain from the genesis.")]
        public void Rebuild(
            [Option('v', Description = "Print more logs.")]
            bool verbose,
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
            [Option('c', Description = "Optional chain ID.  Default is the canonical chain ID.")]
            Guid? chainId = null,
            [Option(
                't',
                Description = "Optional topmost block to execute last.  Tip by default.")]
            string topmost = null,
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
            TextWriter stderr = Console.Error;
            (
                BlockChain<NCAction> chain,
                IStore store,
                IKeyValueStore stateKvStore,
                IStateStore stateStore
            ) = Utils.GetBlockChain(
                logger,
                storePath,
                chainId,
                useMemoryKvStore is string p ? new MemoryKeyValueStore(p, stderr) : null
            );
            Block<NCAction> genesis = chain.Genesis;
            Block<NCAction> tip = Utils.ParseBlockOffset(chain, topmost);

            stderr.WriteLine("Clear the existing state store...");
            foreach (KeyBytes key in stateKvStore.ListKeys())
            {
                stateKvStore.Delete(key);
            }

            stderr.WriteLine("It will execute all actions (tx actions & block actions)");
            stderr.WriteLine(
                "  ...from the block #{0} {1}",
                "0".PadRight(tip.Index.ToString(CultureInfo.InvariantCulture).Length),
                genesis.Hash);
            stderr.WriteLine("    ...to the block #{0} {1}.", tip.Index, tip.Hash);

            IBlockPolicy<NCAction> policy = chain.Policy;
            (Block<NCAction>, string)? invalidStateRootHashBlock = null;
            long totalBlocks = tip.Index + 1L;
            long blocksExecuted = 0L;
            long txsExecuted = 0L;
            DateTimeOffset started = DateTimeOffset.Now;
            foreach (BlockHash blockHash in chain.BlockHashes)
            {
                Block<NCAction> block =
                    store.GetBlock<NCAction>(policy.GetHashAlgorithm, blockHash);
                var preEvalBlock = new PreEvaluationBlock<NCAction>(
                    block,
                    block.HashAlgorithm,
                    block.Nonce,
                    block.PreEvaluationHash
                );
                stderr.WriteLine(
                    "[{0}/{1}] Executing block #{2} {3}...",
                    block.Index,
                    tip.Index,
                    block.Index,
                    block.Hash
                );
                IImmutableDictionary<string, IValue> delta;
                HashDigest<SHA256> stateRootHash = block.Index < 1
                    ? preEvalBlock.DetermineStateRootHash(chain.Policy.BlockAction, stateStore, out delta)
                    : preEvalBlock.DetermineStateRootHash(
                        chain,
                        StateCompleterSet<NCAction>.Reject,
                        out delta);
                DateTimeOffset now = DateTimeOffset.Now;
                if (invalidStateRootHashBlock is null && !stateRootHash.Equals(block.StateRootHash))
                {
                    string blockDump = DumpBencodexToFile(
                        block.MarshalBlock(),
                        $"block_{block.Index}_{block.Hash}"
                    );
                    string deltaDump = DumpBencodexToFile(
                        new Dictionary(
                            delta.Select(kv =>
                                new KeyValuePair<IKey, IValue>(new Text(kv.Key), kv.Value))),
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

                if (blocksExecuted >= totalBlocks || block.Hash.Equals(tip.Hash))
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
                        Path.Join(subDir,$"{ByteUtil.Hex(pair.Key.ByteArray)}"), pair.Value);
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
    }
}
