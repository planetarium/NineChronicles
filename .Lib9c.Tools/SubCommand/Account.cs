using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cocona;
using Lib9c.DevExtensions;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog.Core;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Account
    {
        [Obsolete("This function is deprecated. Please use `NineChronicles.Headless.Executable account` command instead.")]
        [Command(Description = "Query accounts' balances.")]
        public void Balance(
            [Option('v', Description = "Print more logs.")]
            bool verbose,
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
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
            (BlockChain<NCAction> chain, IStore store, _, _) =
                Utils.GetBlockChain(logger, storePath, chainId);

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
                BlockDigest digest = GetBlockDigest(store, blockHash);
                stderr.WriteLine("Scanning block #{0} {1}...", digest.Index, digest.Hash);
                stderr.Flush();
                IEnumerable<Address> addrs = digest.TxIds
                    .Select(txId => store.GetTransaction<NCAction>(new TxId(txId.ToArray())))
                    .SelectMany(tx => tx.CustomActions is { } ca
                        ? ca.Select(a => a.InnerAction)
                            .SelectMany(a => a is TransferAsset t
                                ? new[] { t.Sender, t.Recipient }
                                : a is InitializeStates i &&
                                    i.GoldDistributions is Bencodex.Types.List l
                                ? l.OfType<Bencodex.Types.Dictionary>()
                                    .Select(d => new GoldDistribution(d).Address)
                                : new Address[0])
                        : Enumerable.Empty<Address>())
                    .Append(digest.Miner);
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

        private static BlockDigest GetBlockDigest(IStore store, BlockHash blockHash)
        {
            BlockDigest? digest = store.GetBlockDigest(blockHash);
            if (digest is { } d)
            {
                return d;
            }

            throw new InvalidOperationException($"Block #{blockHash} is not found in the store.");
        }
    }
}
