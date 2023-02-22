using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("cancel_product_registration")]
    public class CancelProductRegistration : GameAction
    {
        public Address AvatarAddress;
        public List<ProductInfo> ProductInfos;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!ProductInfos.Any())
            {
                throw new ListEmptyException("ProductInfos was empty.");
            }

            // 주소 검증
            if (ProductInfos.Any(p => p.AvatarAddress != AvatarAddress) ||
                ProductInfos.Any(p => p.AgentAddress != context.Signer))
            {
                throw new InvalidAddressException();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("failed to load avatar state");
            }

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(AvatarAddress.ToHex(),
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
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
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            foreach (var productInfo in ProductInfos)
            {
                if (productInfo.Legacy)
                {
                    var productType = productInfo.Type;
                    var avatarAddress = avatarState.address;
                    if (productType == ProductType.FungibleAssetValue)
                    {
                        // 잘못된 타입
                        throw new InvalidProductTypeException($"Order not support {productType}");
                    }
                    var orderAddress = Order.DeriveAddress(productInfo.ProductId);
                    if (!states.TryGetState(orderAddress, out Dictionary rawOrder))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(Order)}({orderAddress}).");
                    }

                    var order = OrderFactory.Deserialize(rawOrder);
                    switch (order)
                    {
                        case FungibleOrder _:
                            if (productInfo.Type == ProductType.NonFungible)
                            {
                                throw new InvalidProductTypeException($"FungibleOrder not support {productType}");
                            }

                            break;
                        case NonFungibleOrder _:
                            if (productInfo.Type == ProductType.Fungible)
                            {
                                throw new InvalidProductTypeException($"NoneFungibleOrder not support {productType}");
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

                    states = SellCancellation.Cancel(context, states, avatarState, addressesHex,
                        order);
                }
                else
                {
                    states = Cancel(productsState, productInfo, states, avatarState, context);
                }
            }

            states = states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productsStateAddress, productsState.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize());
            }

            return states;
        }

        public static IAccountStateDelta Cancel(ProductsState productsState, ProductInfo productInfo, IAccountStateDelta states,
            AvatarState avatarState, IActionContext context)
        {
            var productId = productInfo.ProductId;
            if (!productsState.ProductIds.Contains(productId))
            {
                throw new ProductNotFoundException($"can't find product {productId}");
            }

            productsState.ProductIds.Remove(productId);

            var productAddress = Product.DeriveAddress(productId);
            var product = ProductFactory.Deserialize((List) states.GetState(productAddress));
            if (product.SellerAgentAddress != avatarState.agentAddress || product.SellerAvatarAddress != avatarState.address)
            {
                throw new InvalidAddressException();
            }

            switch (product)
            {
                case FavProduct favProduct:
                    states = states.TransferAsset(productAddress, avatarState.address,
                        favProduct.Asset);
                    break;
                case ItemProduct itemProduct:
                    switch (itemProduct.TradableItem)
                    {
                        case Costume costume:
                            avatarState.UpdateFromAddCostume(costume, true);
                            break;
                        case ItemUsable itemUsable:
                            avatarState.UpdateFromAddItem(itemUsable, true);
                            break;
                        case TradableMaterial tradableMaterial:
                        {
                            avatarState.UpdateFromAddItem(tradableMaterial, itemProduct.ItemCount, true);
                            break;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product));
            }

            var mail = new ProductCancelMail(context.BlockIndex, productId, context.BlockIndex, productId);
            avatarState.Update(mail);
            states = states.SetState(productAddress, Null.Value);
            return states;
        }


        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = new List(ProductInfos.Select(p => p.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ProductInfos = plainValue["p"].ToList(s => new ProductInfo((List) s));
        }
    }
}
