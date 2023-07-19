using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("cancel_product_registration")]
    public class CancelProductRegistration : GameAction
    {
        public const int CostAp = 5;
        public const int Capacity = 100;
        public Address AvatarAddress;
        public List<IProductInfo> ProductInfos;
        public bool ChargeAp;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IAccountStateDelta states = context.PreviousState;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!ProductInfos.Any())
            {
                throw new ListEmptyException("ProductInfos was empty.");
            }

            if (ProductInfos.Count > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(ProductInfos)} must be less than or equal {Capacity}.");
            }

            foreach (var productInfo in ProductInfos)
            {
                productInfo.ValidateType();
                if (productInfo.AvatarAddress != AvatarAddress ||
                    productInfo.AgentAddress != context.Signer)
                {
                    throw new InvalidAddressException();
                }
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

            avatarState.UseAp(CostAp, ChargeAp, states.GetSheet<MaterialItemSheet>(), context.BlockIndex, states.GetGameConfigState());
            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            ProductsState productsState;
            if (states.TryGetState(productsStateAddress, out List rawProductList))
            {
                productsState = new ProductsState(rawProductList);
            }
            else
            {
                // cancel order before product registered case.
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
                if (productInfo is ItemProductInfo {Legacy: true})
                {
                    var productType = productInfo.Type;
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

        public static IAccountStateDelta Cancel(ProductsState productsState, IProductInfo productInfo, IAccountStateDelta states,
            AvatarState avatarState, IActionContext context)
        {
            var productId = productInfo.ProductId;
            if (!productsState.ProductIds.Contains(productId))
            {
                throw new ProductNotFoundException($"can't find product {productId}");
            }

            productsState.ProductIds.Remove(productId);

            var productAddress = Product.DeriveAddress(productId);
            var product = ProductFactory.DeserializeProduct((List) states.GetState(productAddress));
            product.Validate(productInfo);

            switch (product)
            {
                case FavProduct favProduct:
                    states = states.TransferAsset(context, productAddress, avatarState.address,
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
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ProductInfos = plainValue["p"].ToList(s => ProductFactory.DeserializeProductInfo((List) s));
            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
