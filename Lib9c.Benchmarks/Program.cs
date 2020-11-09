using System;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
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
                if (block.Index > 0)
                {
                    block.Evaluate(
                        DateTimeOffset.UtcNow,
                        address => chain.GetState(address, blockHash),
                        (address, currency) => chain.GetBalance(address, currency, blockHash)
                    );
                }
                else
                {
                    block.Evaluate(
                        DateTimeOffset.UtcNow,
                        _ => null,
                        ((_, currency) => new FungibleAssetValue(currency))
                    );
                }
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
    }
}
