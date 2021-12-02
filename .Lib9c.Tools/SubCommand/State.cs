using System;
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
            bool bypassStateRootHashCheck = false
        )
        {
            using Logger logger = Utils.ConfigureLogger(verbose);
            TextWriter stderr = Console.Error;
            (
                BlockChain<NCAction> chain,
                IStore store,
                IKeyValueStore stateKvStore,
                IStateStore stateStore
            ) = Utils.GetBlockChain(logger, storePath, chainId);
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
        }
    }
}
