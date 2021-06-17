using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    public class RewardGold : ActionBase
    {
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            states = GenesisGoldDistribution(context, states);
            states = WeeklyArenaRankingBoard(context, states);
            // FIXME Separate action before new shop release
            // Log.Debug("Start Migration");
            // states = MigrateOrder(context, states);
            // Log.Debug("Complete Migration");
            return MinerReward(context, states);
        }

        public IAccountStateDelta GenesisGoldDistribution(IActionContext ctx, IAccountStateDelta states)
        {
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;
            foreach(GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                if (amount <= 0) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
                FungibleAssetValue fav = goldCurrency * amount;
                var testAddresses = new HashSet<Address>(
                    new []
                    {
                        new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                        new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                    }
                );
                if (!testAddresses.Contains(distribution.Address))
                {
                    fav = fav.DivRem(100, out FungibleAssetValue _);
                }
                states = states.TransferAsset(
                    fund,
                    distribution.Address,
                    fav
                );
            }
            return states;
        }

        public IAccountStateDelta WeeklyArenaRankingBoard(IActionContext ctx, IAccountStateDelta states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = states.GetWeeklyArenaState(index - 1);
                if (!prevWeekly.Ended)
                {
                    prevWeekly.End();
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    states = states.SetState(prevWeekly.address, prevWeekly.Serialize());
                    states = states.SetState(weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weekly.address, weekly.Serialize());
            }
            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int)Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
            FungibleAssetValue miningReward =
                defaultMiningReward.DivRem(countOfHalfLife, out FungibleAssetValue _);

            if (miningReward >= FungibleAssetValue.Parse(currency, "1.25"))
            {
                states = states.TransferAsset(
                    GoldCurrencyState.Address,
                    ctx.Miner,
                    miningReward
                );
            }

            return states;
        }

        public IAccountStateDelta MigrateOrder(IActionContext ctx, IAccountStateDelta states)
        {
            var types = new List<ItemSubType>()
            {
                ItemSubType.Weapon,
                ItemSubType.Armor,
                ItemSubType.Belt,
                ItemSubType.Necklace,
                ItemSubType.Ring,
                ItemSubType.Food,
                ItemSubType.Hourglass,
                ItemSubType.ApStone,
                ItemSubType.FullCostume,
                ItemSubType.HairCostume,
                ItemSubType.EarCostume,
                ItemSubType.EyeCostume,
                ItemSubType.TailCostume,
                ItemSubType.Title,
            };

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            foreach (var type in types)
            {
                Log.Debug($"Type: {type}");
                foreach (var key in ShardedShopState.AddressKeys)
                {
                    Log.Debug($"AddressKey: {key}");
                    var address = ShardedShopState.DeriveAddress(type, key);
                    if (states.GetState(address) is Dictionary dictionary)
                    {
                        var shardedShopState = new ShardedShopState(dictionary);
                        foreach (var shopItem in shardedShopState.Products.Values)
                        {
                            if (shopItem.ExpiredBlockIndex < ctx.BlockIndex)
                            {
                                continue;
                            }
                            var order = OrderFactory.Create(shopItem);
                            var orderAddress = Order.DeriveAddress(order.OrderId);
                            var orderDigest = order.Digest(shopItem, costumeStatSheet);
                            var itemAddress = Addresses.GetItemAddress(order.TradableId);
                            var v2Address = ShardedShopStateV2.DeriveAddress(order.ItemSubType, order.OrderId);
                            var shardedShopStateV2 = states.TryGetState(v2Address, out Dictionary dict)
                                ? new ShardedShopStateV2(dict)
                                : new ShardedShopStateV2(v2Address);
                            if (shardedShopStateV2.OrderDigestList.Any(o => o.OrderId.Equals(order.OrderId)))
                            {
                                continue;
                            }

                            var digestListAddress = OrderDigestListState.DeriveAddress(order.SellerAvatarAddress);
                            var digestList = states.TryGetState(digestListAddress, out Dictionary receiptDict)
                                ? new OrderDigestListState(receiptDict)
                                : new OrderDigestListState(digestListAddress);
                            digestList.Add(orderDigest);

                            shardedShopStateV2.Add(orderDigest, ctx.BlockIndex);

                            if (!(shopItem.ItemUsable is null))
                            {
                                states = states.SetState(itemAddress, shopItem.ItemUsable.Serialize());
                            }
                            if (!(shopItem.Costume is null))
                            {
                                states = states.SetState(itemAddress, shopItem.Costume.Serialize());
                            }
                            if (!(shopItem.TradableFungibleItem is null))
                            {
                                states = states.SetState(itemAddress, shopItem.TradableFungibleItem.Serialize());
                            }
                            states = states.SetState(orderAddress, order.Serialize());
                            states = states.SetState(digestListAddress, digestList.Serialize());
                            states = states.SetState(v2Address, shardedShopStateV2.Serialize());
                        }
                    }
                }
            }
            return states;
        }
    }
}
