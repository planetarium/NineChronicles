using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("re_register_product")]
    public class ReRegisterProduct : GameAction
    {
        public Address AvatarAddress;
        public List<(ProductInfo, IRegisterInfo)> ReRegisterInfos;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!ReRegisterInfos.Any())
            {
                throw new ListEmptyException($"ReRegisterInfos was empty.");
            }
            if (ReRegisterInfos.Any(tuple =>
                    tuple.Item1.AvatarAddress != AvatarAddress ||
                    tuple.Item2.AvatarAddress != AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("failed to load avatar state");
            }

            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            ProductsState productsState;
            if (states.TryGetState(productsStateAddress, out List rawProductList))
            {
                productsState = new ProductsState(rawProductList);
            }
            else
            {
                var marketState = states.TryGetState(Addresses.Market, out List rawMarketList)
                    ? new MarketState(rawMarketList)
                    : new MarketState();
                productsState = new ProductsState();
                marketState.AvatarAddresses.Add(AvatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }
            foreach (var (productInfo, info) in ReRegisterInfos.OrderBy(tuple => tuple.Item2.Type).ThenBy(tuple => tuple.Item2.Price))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                if (productInfo.Legacy)
                {
                    // if product is order. it move to products state from sharded shop state.
                    var productType = productInfo.Type;
                    var avatarAddress = avatarState.address;
                    if (productType == ProductType.FungibleAssetValue)
                    {
                        // 잘못된 타입
                        throw new InvalidProductTypeException(
                            $"Order not support {productType}");
                    }

                    var digestListAddress =
                        OrderDigestListState.DeriveAddress(avatarAddress);
                    if (!states.TryGetState(digestListAddress, out Dictionary rawList))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(OrderDigest)}({digestListAddress}).");
                    }

                    var digestList = new OrderDigestListState(rawList);
                    var orderAddress = Order.DeriveAddress(productInfo.ProductId);
                    if (!states.TryGetState(orderAddress, out Dictionary rawOrder))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(Order)}({orderAddress}).");
                    }

                    var order = OrderFactory.Deserialize(rawOrder);
                    var itemCount = 1;
                    switch (order)
                    {
                        case FungibleOrder fungibleOrder:
                            itemCount = fungibleOrder.ItemCount;
                            if (productInfo.Type == ProductType.NonFungible)
                            {
                                throw new InvalidProductTypeException(
                                    $"FungibleOrder not support {productType}");
                            }

                            break;
                        case NonFungibleOrder _:
                            if (productInfo.Type == ProductType.Fungible)
                            {
                                throw new InvalidProductTypeException(
                                    $"NoneFungibleOrder not support {productType}");
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(order));
                    }

                    if (order.SellerAvatarAddress != avatarAddress ||
                        order.SellerAgentAddress != context.Signer)
                    {
                        throw new InvalidAddressException();
                    }

                    if (!order.Price.Equals(productInfo.Price))
                    {
                        throw new InvalidPriceException($"order price does not match information. expected: {order.Price} actual: {productInfo.Price}");
                    }

                    var updateSellInfo = new UpdateSellInfo(productInfo.ProductId,
                        productInfo.ProductId, order.TradableId,
                        order.ItemSubType, productInfo.Price, itemCount);
                    states = UpdateSell.Cancel(states, updateSellInfo, addressesHex,
                        avatarState, digestList, context,
                        avatarState.address);
                }
                else
                {
                    states = CancelProductRegistration.Cancel(productsState, productInfo,
                        states, avatarState, context);
                }
                states = RegisterProduct.Register(context, info, avatarState, productsState, states);
            }

            states = states
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productsStateAddress, productsState.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize());
            }
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = new List(ReRegisterInfos.Select(tuple =>
                    List.Empty.Add(tuple.Item1.Serialize()).Add(tuple.Item2.Serialize()))),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ReRegisterInfos = new List<(ProductInfo, IRegisterInfo)>();
            var serialized = (List) plainValue["r"];
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                var registerList = (List) innerList[1];
                IRegisterInfo info =
                    registerList[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue
                        ? (IRegisterInfo) new AssetInfo(registerList)
                        : new RegisterInfo(registerList);
                ReRegisterInfos.Add((new ProductInfo((List)innerList[0]), info));
            }
        }
    }
}
