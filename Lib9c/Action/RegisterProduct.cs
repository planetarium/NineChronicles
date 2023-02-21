using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_product")]
    public class RegisterProduct : GameAction
    {
        public IEnumerable<IRegisterInfo> RegisterInfos;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (RegisterInfos.Select(r => r.AvatarAddress).Distinct().Count() != 1)
            {
                throw new InvalidAddressException();
            }

            var ncg = states.GetGoldCurrency();
            if (RegisterInfos.Any(r => !r.Price.Currency.Equals(ncg) ||
                !r.Price.MinorUnit.IsZero ||
                r.Price < 1 * ncg))
            {
                throw new InvalidPriceException(
                    $"product price must be greater than zero.");
            }

            var avatarAddress = RegisterInfos.First().AvatarAddress;
            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            if (!avatarState.worldInformation.IsStageCleared(
                    GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    avatarAddress.ToHex(),
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }

            var productsStateAddress = ProductsState.DeriveAddress(avatarAddress);
            ProductsState productsState;
            if (states.TryGetState(productsStateAddress, out List rawProducts))
            {
                productsState = new ProductsState(rawProducts);
            }
            else
            {
                productsState = new ProductsState();
                var marketState = states.TryGetState(Addresses.Market, out List rawMarketList)
                    ? new MarketState(rawMarketList)
                    : new MarketState();
                marketState.AvatarAddresses.Add(avatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }
            foreach (var info in RegisterInfos.OrderBy(r => r.Type).ThenBy(r => r.Price))
            {
                states = Register(context, info, avatarState, productsState, states);
            }

            states = states
                .SetState(avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productsStateAddress, productsState.Serialize());
            if (migrationRequired)
            {
                states = states
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize());
            }

            return states;
        }

        public static IAccountStateDelta Register(IActionContext context, IRegisterInfo info, AvatarState avatarState,
            ProductsState productsState, IAccountStateDelta states)
        {
            switch (info)
            {
                case RegisterInfo registerInfo:
                    switch (info.Type)
                    {
                        case ProductType.Fungible:
                        case ProductType.NonFungible:
                        {
                            var tradableId = registerInfo.TradableId;
                            var itemCount = registerInfo.ItemCount;
                            var type = registerInfo.Type;
                            ITradableItem tradableItem = null;
                            switch (type)
                            {
                                case ProductType.Fungible:
                                {
                                    if (avatarState.inventory.TryGetTradableItems(tradableId,
                                            context.BlockIndex, itemCount, out var items))
                                    {
                                        int totalCount = itemCount;
                                        tradableItem = (ITradableItem) items.First().item;
                                        foreach (var inventoryItem in items)
                                        {
                                            int removeCount = Math.Min(totalCount,
                                                inventoryItem.count);
                                            ITradableFungibleItem tradableFungibleItem =
                                                (ITradableFungibleItem) inventoryItem.item;
                                            if (!avatarState.inventory.RemoveTradableItem(
                                                    tradableId,
                                                    tradableFungibleItem.RequiredBlockIndex,
                                                    removeCount))
                                            {
                                                throw new ItemDoesNotExistException(
                                                    $"failed to remove tradable material {tradableId}/{itemCount}");
                                            }

                                            totalCount -= removeCount;
                                            if (totalCount < 1)
                                            {
                                                break;
                                            }
                                        }

                                        if (totalCount != 0)
                                        {
                                            throw new InvalidItemCountException();
                                        }
                                    }

                                    break;
                                }
                                case ProductType.NonFungible:
                                {
                                    if (avatarState.inventory.TryGetNonFungibleItem(tradableId,
                                            out var item) &&
                                        avatarState.inventory.RemoveNonFungibleItem(tradableId))
                                    {
                                        tradableItem = (ITradableItem) item.item;
                                    }

                                    break;
                                }
                            }

                            if (tradableItem is null)
                            {
                                throw new ItemDoesNotExistException($"can't find item: {tradableId}");
                            }

                            Guid productId = context.Random.GenerateRandomGuid();
                            var product = new ItemProduct
                            {
                                ProductId = productId,
                                Price = registerInfo.Price,
                                TradableItem = tradableItem,
                                ItemCount = itemCount,
                                RegisteredBlockIndex = context.BlockIndex,
                                Type = registerInfo.Type,
                                SellerAgentAddress = context.Signer,
                                SellerAvatarAddress = registerInfo.AvatarAddress,
                            };
                            productsState.ProductIds.Add(productId);
                            states = states.SetState(Product.DeriveAddress(productId),
                                product.Serialize());
                            break;
                        }
                        case ProductType.FungibleAssetValue:
                        default:
                            throw new InvalidProductTypeException($"register item does not support {ProductType.FungibleAssetValue}");
                    }

                    break;
                case AssetInfo assetInfo:
                {
                    if (assetInfo.Type == ProductType.FungibleAssetValue)
                    {
                        if (assetInfo.Asset.Currency.Equals(CrystalCalculator.CRYSTAL))
                        {
                            throw new InvalidCurrencyException($"{CrystalCalculator.CRYSTAL} does not allow register.");
                        }

                        Guid productId = context.Random.GenerateRandomGuid();
                        Address productAddress = Product.DeriveAddress(productId);
                        FungibleAssetValue asset = assetInfo.Asset;
                        var product = new FavProduct
                        {
                            ProductId = productId,
                            Price = assetInfo.Price,
                            Asset = asset,
                            RegisteredBlockIndex = context.BlockIndex,
                            Type = assetInfo.Type,
                            SellerAgentAddress = context.Signer,
                            SellerAvatarAddress = assetInfo.AvatarAddress,
                        };
                        states = states
                            .TransferAsset(avatarState.address, productAddress, asset)
                            .SetState(productAddress, product.Serialize());
                        productsState.ProductIds.Add(productId);
                        break;
                    }
                    throw new InvalidProductTypeException($"register asset does not support {assetInfo.Type}");
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RegisterInfos.Select(r => r.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = (List) plainValue["r"];
            RegisterInfos = serialized.Cast<List>()
                .Select(registerList =>
                    registerList[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue
                        ? (IRegisterInfo) new AssetInfo(registerList)
                        : new RegisterInfo(registerList)).ToList();
        }
    }
}
