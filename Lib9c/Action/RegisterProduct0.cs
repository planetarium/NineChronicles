using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_product")]
    public class RegisterProduct0 : GameAction
    {
        public const int CostAp = 5;
        public const int Capacity = 100;
        public Address AvatarAddress;
        public IEnumerable<IRegisterInfo> RegisterInfos;
        public bool ChargeAp;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!RegisterInfos.Any())
            {
                throw new ListEmptyException("RegisterInfos was empty");
            }

            if (RegisterInfos.Count() > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(RegisterInfos)} must be less than or equal {Capacity}.");
            }

            var ncg = states.GetGoldCurrency();
            foreach (var registerInfo in RegisterInfos)
            {
                registerInfo.ValidateAddress(AvatarAddress);
                registerInfo.ValidatePrice(ncg);
                registerInfo.Validate();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("failed to load avatar state.");
            }

            if (!avatarState.worldInformation.IsStageCleared(
                    GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    AvatarAddress.ToHex(),
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }

            avatarState.UseAp(CostAp, ChargeAp, states.GetSheet<MaterialItemSheet>(), context.BlockIndex, states.GetGameConfigState());
            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
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
                marketState.AvatarAddresses.Add(AvatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }
            foreach (var info in RegisterInfos.OrderBy(r => r.Type).ThenBy(r => r.Price))
            {
                states = Register(context, info, avatarState, productsState, states);
            }

            states = states
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(productsStateAddress, productsState.Serialize());
            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize());
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
                    }

                    break;
                case AssetInfo assetInfo:
                {
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
                        .TransferAsset(context, avatarState.address, productAddress, asset)
                        .SetState(productAddress, product.Serialize());
                    productsState.ProductIds.Add(productId);
                    break;
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RegisterInfos.Select(r => r.Serialize())),
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = (List) plainValue["r"];
            RegisterInfos = serialized.Cast<List>()
                .Select(ProductFactory.DeserializeRegisterInfo).ToList();
            AvatarAddress = plainValue["a"].ToAddress();
            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
