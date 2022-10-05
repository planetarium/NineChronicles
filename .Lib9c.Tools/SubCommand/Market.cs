using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bencodex.Types;
using Cocona;
using Lib9c.DevExtensions;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Serilog.Core;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Market
    {
        [Obsolete("This function is deprecated. Please use `NineChronicles.Headless.Executable market` command instead.")]
        [Command(Description = "Query market transactions.")]
        public void Query(
            [Option('v', Description = "Print more logs.")]
            bool verbose,
            [Option('s', Description = "Path to the chain store.")]
            string storePath,
            [Option(
                'f',
                Description = "Optional bottom block hash/index to search.  Genesis by default.")]
            string from = null,
            [Option(
                't',
                Description = "Optional topmost block hash/index to search.  Tip by default.")]
            string to = null,
            [Option('F', Description = "Include failed transactions too.")]
            bool includeFails = false,
            [Option(
                'T',
                Description = "Filter by item type.  This implicitly filters out transactions " +
                    "made with " + nameof(Buy) + " action version prior to " + nameof(Buy5) +
                    ".  This can be applied multiple times (meaning: match any of them).  " +
                    "The list of available types can be found in " + nameof(ItemSubType) +
                    " enum declared in Lib9c/Model/Item/ItemType.cs file.")]
            string[] itemType = null,
            [Option('c', Description = "Optional chain ID.  Default is the canonical chain ID.")]
            Guid? chainId = null
        )
        {
            using Logger logger = Utils.ConfigureLogger(verbose);
            TextWriter stderr = Console.Error;
            (BlockChain<NCAction> chain, IStore store, _, _) =
                Utils.GetBlockChain(logger, storePath, chainId);

            HashSet<ItemSubType> itemTypes = null;
            if (itemType is {} t)
            {
                try
                {
                    itemTypes =
                        t.Select(s => ItemSubType.Parse<ItemSubType>(s, ignoreCase: true))
                        .ToHashSet();
                }
                catch (ArgumentException e)
                {
                    throw new CommandExitedException("-T/--item-type: " + e.Message, 1);
                }
            }

            Block<NCAction> start = Utils.ParseBlockOffset(chain, from, defaultIndex: 0);
            stderr.WriteLine("The bottom block to search: #{0} {1}.", start.Index, start.Hash);
            Block<NCAction> end = Utils.ParseBlockOffset(chain, to);
            stderr.WriteLine("The topmost block to search: #{0} {1}.", end.Index, end.Hash);

            Block<NCAction> block = end;
            int indexWidth = block.Index.ToString().Length + 1;
            Console.WriteLine(
                "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                $"#IDX".PadRight(indexWidth),
                "HASH".PadRight(BlockHash.Size * 2),
                "TIME".PadRight(DateTimeOffset.Now.ToString("o").Length),
                "TXID".PadRight(TxId.Size * 2),
                "BUYER".PadRight(Address.Size * 2),
                "BUYER AVATER".PadRight(Address.Size * 2),
                "SELLER".PadRight(Address.Size * 2),
                "SELLER AVATER".PadRight(Address.Size * 2),
                "QUANTITY",
                "TOTAL PRICE"
            );

            while (true)
            {
                stderr.WriteLine("Scanning block #{0} {1}...", block.Index, block.Hash);
                stderr.Flush();

                IEnumerable<(Transaction<NCAction>, NCAction)> actions = block.Transactions
                    .Reverse()
                    .Where(tx => includeFails ||
                        !(chain.GetTxExecution(block.Hash, tx.Id) is { } e) ||
                        e is TxSuccess)
                    .SelectMany(tx => tx.CustomActions is { } ca
                        ? ca.Reverse().Select(a => (tx, a))
                        : Enumerable.Empty<(Transaction<NCAction>, NCAction)>());

                foreach (var (tx, act) in actions)
                {
                    ActionBase a = act.InnerAction;
                    IEnumerable<Order> orders = act.InnerAction switch
                    {
                        IBuy0 b0 => new Order[]
                        {
                            new Order
                            {
                                BuyerAvatar = b0.buyerAvatarAddress,
                                Seller = b0.sellerAgentAddress,
                                SellerAvatar = b0.sellerAvatarAddress,
                            },
                        },
                        IBuy5 b => b.purchaseInfos.Reverse().Select(p => new Order
                        {
                            BuyerAvatar = b.buyerAvatarAddress,
                            Seller = p.SellerAgentAddress,
                            SellerAvatar = p.SellerAvatarAddress,
                            Price = p.Price,
                            ItemSubType = p.ItemSubType,
                            Quantity = p.OrderId is {} oid
                                ? chain.GetState(GetOrderAddress(oid)) is Dictionary rawOrder
                                    ? OrderFactory.Deserialize(rawOrder) is FungibleOrder fo
                                        ? fo.ItemCount
                                        : 1
                                    : (int?)null
                                : (int?)null,
                        }),
                        _ => new Order[0],
                    };

                    if (itemTypes is {} types)
                    {
                        orders = orders.Where(o => o.ItemSubType is {} t && types.Contains(t));
                    }

                    foreach (Order order in orders)
                    {
                        Console.WriteLine(
                            "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                            $"#{block.Index}".PadLeft(indexWidth),
                            block.Hash,
                            tx.Timestamp.ToString("o"),
                            tx.Id,
                            tx.Signer.ToHex(),
                            order.BuyerAvatar.ToHex(),
                            order.Seller.ToHex(),
                            order.SellerAvatar.ToHex(),
                            order.Quantity?.ToString().PadLeft(8) ?? "",
                            order.Price?.ToString() ?? "(N/A)"
                        );
                    }
                }

                if (block.Hash.Equals(start.Hash) || !(block.PreviousHash is {} prevHash))
                {
                    break;
                }

                try
                {
                    block = chain[prevHash];
                }
                catch (KeyNotFoundException)
                {
                    stderr.WriteLine(
                        "The block #{0} {1} cannot be found.", block.Index - 1, prevHash);
                    for (long i = block.Index - 1; i >= 0; i--)
                    {
                        try
                        {
                            block = chain[i];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            continue;
                        }

                        break;
                    }
                }
            }
        }

        private static Address GetOrderAddress(Guid orderId) =>
            Model.Order.Order.DeriveAddress(orderId);

        struct Order
        {
            public Address BuyerAvatar;
            public Address Seller;
            public Address SellerAvatar;
            public FungibleAssetValue? Price;
            public ItemSubType? ItemSubType;
            public int? Quantity;
        }
    }
}
