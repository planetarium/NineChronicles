namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Xunit;

    public class RegisterProductTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private IAccountStateDelta _initialState;

        public RegisterProductTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _currency = Currency.Legacy("NCG", 2, minters: null);
            _initialState = new State()
                .SetState(GoldCurrencyState.Address, new GoldCurrencyState(_currency).Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Fact]
        public void Execute()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _avatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _avatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _avatarState.inventory.Items.Count);
            var asset = 3 * RuneHelper.DailyRewardRune;
            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .MintAsset(_avatarAddress, asset);
            var action = new RegisterProduct
            {
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _avatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _avatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Asset = asset,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);

            var marketState = new MarketState(nextState.GetState(Addresses.Market));
            Assert.Contains(_avatarAddress, marketState.AvatarAddresses);

            var productsState =
                new ProductsState((List)nextState.GetState(ProductsState.DeriveAddress(_avatarAddress)));
            var random = new TestRandom();
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();
                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.Deserialize((List)nextState.GetState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * _currency, product.Price);
                if (product is ItemProduct itemProduct)
                {
                    Assert.Equal(1, itemProduct.ItemCount);
                    Assert.NotNull(itemProduct.TradableItem);
                }

                if (product is FavProduct favProduct)
                {
                    Assert.Equal(asset, favProduct.Asset);
                }
            }

            Assert.Equal(0 * asset.Currency, nextState.GetBalance(_avatarAddress, asset.Currency));
        }

        [Theory]
        [InlineData(true, false, typeof(InvalidAddressException))]
        [InlineData(false, true, typeof(FailedLoadStateException))]
        public void Execute_Throw_Invalid_Avatar_Addresses(bool multipleAvatarAddress, bool invalidAvatarAddress, Type exc)
        {
            var registerInfoList = new List<IRegisterInfo>();
            if (multipleAvatarAddress)
            {
                for (int i = 0; i < 2; i++)
                {
                    var avatarAddress = _avatarAddress;
                    if (i == 0)
                    {
                        avatarAddress = new PrivateKey().ToAddress();
                    }

                    registerInfoList.Add(new AssetInfo
                    {
                        AvatarAddress = avatarAddress,
                        Asset = CrystalCalculator.CRYSTAL * 100,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                    });
                }
            }

            if (invalidAvatarAddress)
            {
                registerInfoList.Add(new AssetInfo
                {
                    AvatarAddress = new PrivateKey().ToAddress(),
                    Asset = CrystalCalculator.CRYSTAL * 100,
                    Price = 1 * _currency,
                    Type = ProductType.FungibleAssetValue,
                });
            }

            var action = new RegisterProduct
            {
                RegisterInfos = registerInfoList,
            };

            Assert.Throws(exc, () => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            }));
        }

        [Theory]
        // ticker is not ncg
        [InlineData(true, false, false, ProductType.NonFungible)]
        [InlineData(true, false, false, ProductType.FungibleAssetValue)]
        // 0.1 ncg
        [InlineData(false, false, true, ProductType.NonFungible)]
        // 0 ncg
        [InlineData(false, true, false, ProductType.Fungible)]
        [InlineData(false, true, false, ProductType.FungibleAssetValue)]
        public void Execute_Throw_InvalidPriceException(
            bool invalidTicker,
            bool invalidQuantity,
            bool isDouble,
            ProductType type
        )
        {
            var currency = invalidTicker ? CrystalCalculator.CRYSTAL : _currency;
            var quantity = invalidQuantity ? 0 : 2;
            var price = quantity * currency;
            if (isDouble)
            {
                price = price.DivRem(10, out _);
            }

            var registerInfos = new List<IRegisterInfo>
            {
                new RegisterInfo
                {
                    AvatarAddress = _avatarAddress,
                    ItemCount = 1,
                    Price = 1 * _currency,
                    TradableId = Guid.NewGuid(),
                    Type = ProductType.Fungible,
                },
                new RegisterInfo
                {
                    AvatarAddress = _avatarAddress,
                    ItemCount = 1,
                    Price = 1 * _currency,
                    TradableId = Guid.NewGuid(),
                    Type = ProductType.NonFungible,
                },
                new AssetInfo
                {
                    AvatarAddress = _avatarAddress,
                    Asset = 1 * RuneHelper.StakeRune,
                    Price = 1 * _currency,
                    Type = ProductType.FungibleAssetValue,
                },
            };

            if (type == ProductType.FungibleAssetValue)
            {
                registerInfos.Add(new AssetInfo
                {
                    AvatarAddress = _avatarAddress,
                    Asset = 1 * RuneHelper.DailyRewardRune,
                    Price = price,
                    Type = type,
                });
            }
            else
            {
                registerInfos.Add(new RegisterInfo
                {
                    AvatarAddress = _avatarAddress,
                    ItemCount = 1,
                    Price = price,
                    TradableId = Guid.NewGuid(),
                    Type = type,
                });
            }

            var action = new RegisterProduct
            {
                RegisterInfos = registerInfos,
            };

            Assert.Throws<InvalidPriceException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
            }));
        }

        [Theory]
        // not enough block index.
        [InlineData(ProductType.Fungible, 1, 2L, 1L, false)]
        // not enough inventory items.
        [InlineData(ProductType.Fungible, 2, 3L, 3L, false)]
        // inventory has locked.
        [InlineData(ProductType.Fungible, 1, 3L, 3L, true)]
        [InlineData(ProductType.NonFungible, 1, 3L, 3L, true)]
        public void Execute_Throw_ItemDoesNotExistException(ProductType type, int itemCount, long requiredBlockIndex, long blockIndex, bool lockInventory)
        {
            ITradableItem tradableItem = null;
            switch (type)
            {
                case ProductType.Fungible:
                {
                    var materialRow = _tableSheets.MaterialItemSheet.Values.First();
                    var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
                    tradableMaterial.RequiredBlockIndex = requiredBlockIndex;
                    tradableItem = tradableMaterial;
                    break;
                }

                case ProductType.NonFungible:
                {
                    var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
                    var id = Guid.NewGuid();
                    tradableItem = ItemFactory.CreateItemUsable(equipmentRow, id, requiredBlockIndex);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (lockInventory)
            {
                _avatarState.inventory.AddItem((ItemBase)tradableItem, iLock: new OrderLock(Guid.NewGuid()));
            }
            else
            {
                _avatarState.inventory.AddItem((ItemBase)tradableItem);
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());
            var action = new RegisterProduct
            {
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _avatarAddress,
                        ItemCount = itemCount,
                        Price = 1 * _currency,
                        TradableId = tradableItem.TradableId,
                        Type = type,
                    },
                },
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext
            {
                Signer = _agentAddress,
                BlockIndex = blockIndex,
                Random = new TestRandom(),
                PreviousStates = _initialState,
            }));
        }

        [Theory]
        [InlineData(ProductType.Fungible)]
        [InlineData(ProductType.NonFungible)]
        [InlineData(ProductType.FungibleAssetValue)]
        public void Execute_Throw_InvalidProductTypeException(ProductType type)
        {
            var registerInfoList = new List<IRegisterInfo>();
            switch (type)
            {
                case ProductType.Fungible:
                case ProductType.NonFungible:
                    registerInfoList.Add(new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Type = type,
                        Price = 1 * _currency,
                    });
                    break;
                case ProductType.FungibleAssetValue:
                    registerInfoList.Add(new RegisterInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Type = type,
                        Price = 1 * _currency,
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var action = new RegisterProduct
            {
                RegisterInfos = registerInfoList,
            };

            Assert.Throws<InvalidProductTypeException>(() => action.Execute(new ActionContext
            {
                Signer = _agentAddress,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                BlockIndex = 1L,
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidCurrencyException()
        {
            _initialState = _initialState.MintAsset(_avatarAddress, RuneHelper.StakeRune * 1);
            var action = new RegisterProduct
            {
                RegisterInfos = new List<IRegisterInfo>
                {
                    new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Asset = 1 * RuneHelper.StakeRune,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Asset = 1 * CrystalCalculator.CRYSTAL,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };

            Assert.Throws<InvalidCurrencyException>(() => action.Execute(new ActionContext
            {
                Signer = _agentAddress,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            }));
        }
    }
}
