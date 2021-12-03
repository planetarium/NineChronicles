using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
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
            foreach (byte[] key in stateKvStore.ListKeys())
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
                HashDigest<SHA256> stateRootHash = block.Index < 1
                    ? preEvalBlock.DetermineStateRootHash(chain.Policy.BlockAction, stateStore)
                    : preEvalBlock.DetermineStateRootHash(chain);
                DateTimeOffset now = DateTimeOffset.Now;
                if (invalidStateRootHashBlock is null && !stateRootHash.Equals(block.StateRootHash))
                {
                    string message =
                        $"Unexpected state root hash for block #{block.Index} {block.Hash}.\n" +
                        $"  Expected: {block.StateRootHash}\n  Actual:   {stateRootHash}";
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

        private class MemoryKeyValueStore : IKeyValueStore
        {
            private readonly ConcurrentDictionary<byte[], byte[]> _dictionary;
            private readonly string _dumpPath;
            private readonly TextWriter _messageWriter;
            private bool _disposed;

            public MemoryKeyValueStore(string dumpPath, TextWriter messageWriter)
            {
                _dictionary = new ConcurrentDictionary<byte[], byte[]>(
                    new ArrayEqualityComparer<byte>());
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
                foreach (KeyValuePair<byte[], byte[]> pair in _dictionary)
                {
                    string subDir = Path.Join(_dumpPath, pair.Key.LongLength.ToString());
                    Directory.CreateDirectory(subDir);
                    File.WriteAllBytes(Path.Join(subDir, $"{ByteUtil.Hex(pair.Key)}"), pair.Value);
                    if (i++ % 1000 == 0)
                    {
                        _messageWriter.Write('.');
                    }
                }
            }

            public byte[] Get(byte[] key) => _dictionary[key];

            public void Set(byte[] key, byte[] value) => _dictionary[key] = value;

            public void Set(IDictionary<byte[], byte[]> values)
            {
                foreach (KeyValuePair<byte[], byte[]> pair in values)
                {
                    _dictionary[pair.Key] = pair.Value;
                }
            }

            public void Delete(byte[] key) => _dictionary.TryRemove(key, out _);

            public bool Exists(byte[] key) => _dictionary.ContainsKey(key);

            public IEnumerable<byte[]> ListKeys() => _dictionary.Keys;
        }

        /// <summary>
        /// An <see cref="IEqualityComparer{T}"/> implementation to compare two arrays of the same
        /// element type.  This compares the elements in the order of the array.
        /// <para>The way to compare each element can be customized by specifying
        /// the <see cref="ElementComparer"/>.</para>
        /// </summary>
        /// <typeparam name="T">The element type of the array.</typeparam>
        private class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
            where T : IEquatable<T>
        {
            /// <summary>
            /// Creates a new instance of <see cref="ArrayEqualityComparer{T}"/>.
            /// </summary>
            /// <param name="elementComparer">Optionally customize the way to compare each element.
            /// </param>
            public ArrayEqualityComparer(IEqualityComparer<T> elementComparer = null)
            {
                ElementComparer = elementComparer;
            }

            /// <summary>
            /// Optionally customizes the way to compare each element.
            /// </summary>
            public IEqualityComparer<T> ElementComparer { get; }

            /// <inheritdoc cref="IEqualityComparer{T}.Equals(T, T)"/>
            public bool Equals(T[] x, T[] y)
            {
                if (x is null && y is null)
                {
                    return true;
                }
                else if (x is null || y is null)
                {
                    return false;
                }
                else if (x.Length != y.Length)
                {
                    return false;
                }

                if (ElementComparer is { } comparer)
                {
                    for (long i = 0L; i < x.LongLength; i++)
                    {
                        if (!comparer.Equals(x[i], y[i]))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    for (long i = 0L; i < x.LongLength; i++)
                    {
                        if (!x[i].Equals(y[i]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            /// <inheritdoc cref="IEqualityComparer{T}.GetHashCode(T)"/>
            public int GetHashCode(T[] obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                int hash = 17;
                if (ElementComparer is { } comparer)
                {
                    foreach (T el in obj)
                    {
                        hash = unchecked(hash * 31 + comparer.GetHashCode(el));
                    }
                }
                else
                {
                    foreach (T el in obj)
                    {
                        hash = unchecked(hash * 31 + el.GetHashCode());
                    }
                }

                return hash;
            }
        }
    }
}
