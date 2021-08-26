using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cocona;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog.Core;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Account
    {
        [Command(Description = "Query accounts' balances.")]
        public void Balance(
            [Option('v', Description = "Print more logs.")]
            bool verbose,
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
            [Option('M',
                Description = "Use legacy " + nameof(MonoRocksDBStore) + " instead of " +
                    nameof(RocksDBStore) + ".")]
            bool monorocksdb = false,
            [Option(
                'b',
                Description = "Optional block hash/index offset to query balances at.  " +
                    "Tip by default.")]
            string block = null,
            [Option('c', Description = "Optional chain ID.  Default is the canonical chain ID.")]
            Guid? chainId = null,
            [Argument(Description = "Account address.")]
            string address = null
        )
        {
            using Logger logger = Utils.ConfigureLogger(verbose);
            TextWriter stderr = Console.Error;
            (BlockChain<NCAction> chain, IStore store) =
                Utils.GetBlockChain(logger, storePath, monorocksdb, chainId);

            Block<NCAction> offset = Utils.ParseBlockOffset(chain, block);
            stderr.WriteLine("The offset block: #{0} {1}.", offset.Index, offset.Hash);

            Bencodex.Types.Dictionary goldCurrencyStateDict = (Bencodex.Types.Dictionary)
                chain.GetState(GoldCurrencyState.Address);
            GoldCurrencyState goldCurrencyState = new GoldCurrencyState(goldCurrencyStateDict);
            Currency gold = goldCurrencyState.Currency;

            if (address is {} addrStr)
            {
                Address addr = Utils.ParseAddress(addrStr);
                FungibleAssetValue balance = chain.GetBalance(addr, gold, offset.Hash);
                Console.WriteLine("{0}\t{1}", addr, balance);
                return;
            }

            var printed = new HashSet<Address>();
            foreach (BlockHash blockHash in chain.BlockHashes)
            {
                Block<NCAction> b = store.GetBlock<NCAction>(blockHash);
                stderr.WriteLine("Scanning block #{0} {1}...", b.Index, b.Hash);
                stderr.Flush();
                IEnumerable<Address> addrs = b.Transactions
                    .SelectMany(tx => tx.Actions
                        .Select(a => a.InnerAction)
                        .SelectMany(a => a is TransferAsset t
                            ? new[] { t.Sender, t.Recipient }
                            : a is InitializeStates i &&
                                i.GoldDistributions is Bencodex.Types.List l
                            ? l.OfType<Bencodex.Types.Dictionary>()
                                .Select(d => new GoldDistribution(d).Address)
                            : new Address[0]))
                    .Append(b.Miner);
                foreach (Address addr in addrs)
                {
                    if (!printed.Contains(addr))
                    {
                        FungibleAssetValue balance = chain.GetBalance(addr, gold, offset.Hash);
                        Console.WriteLine("{0}\t{1}", addr, balance);
                        printed.Add(addr);
                    }
                }
            }
        }
    }
}