namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RegisterProductTest
    {
        private static readonly Address AvatarAddress =
            new Address("47d082a115c63e7b58b1532d20e631538eafadde");

        private static readonly Currency Gold = Currency.Legacy("NCG", 2, minters: null);

        private readonly Address _agentAddress;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private readonly GameConfigState _gameConfigState;
        private IAccountStateDelta _initialState;

        public RegisterProductTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            var rankingMapAddress = new PrivateKey().ToAddress();
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            _avatarState = new AvatarState(
                AvatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = AvatarAddress;

            _initialState = new MockStateDelta()
                .SetState(GoldCurrencyState.Address, new GoldCurrencyState(Gold).Serialize())
                .SetState(Addresses.GetSheetAddress<MaterialItemSheet>(), _tableSheets.MaterialItemSheet.Serialize())
                .SetState(Addresses.GameConfig, _gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(AvatarAddress, _avatarState.Serialize());
        }

        public static IEnumerable<object[]> Execute_Validate_MemberData()
        {
            yield return new object[]
            {
                new ValidateMember
                {
                    RegisterInfos = new List<IRegisterInfo>(),
                    Exc = typeof(ListEmptyException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = new PrivateKey().ToAddress(),
                        },
                        new AssetInfo
                        {
                            AvatarAddress = new PrivateKey().ToAddress(),
                        },
                    },
                    Exc = typeof(InvalidAddressException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * Gold,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * CrystalCalculator.CRYSTAL,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = (10 * Gold).DivRem(3, out _),
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * Gold,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * CrystalCalculator.CRYSTAL,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = (10 * Gold).DivRem(3, out _),
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Type = ProductType.FungibleAssetValue,
                            Price = 1 * Gold,
                            Asset = 0 * RuneHelper.StakeRune,
                        },
                    },
                    Exc = typeof(InvalidPriceException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.Fungible,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                        },
                    },
                    Exc = typeof(InvalidProductTypeException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                            ItemCount = 2,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.Fungible,
                            ItemCount = 0,
                        },
                    },
                    Exc = typeof(InvalidItemCountException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                            Asset = 1 * CrystalCalculator.CRYSTAL,
                        },
                    },
                    Exc = typeof(InvalidCurrencyException),
                },
            };
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
            var context = new ActionContext();
            _initialState = _initialState
                .SetState(AvatarAddress, _avatarState.Serialize())
                .MintAsset(context, AvatarAddress, asset);
            var action = new RegisterProduct
            {
                AvatarAddress = AvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = 1,
                        Price = 1 * Gold,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = 1,
                        Price = 1 * Gold,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = AvatarAddress,
                        Asset = asset,
                        Price = 1 * Gold,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(AvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(_gameConfigState.ActionPointMax - RegisterProduct.CostAp, nextAvatarState.actionPoint);

            var marketState = new MarketState(nextState.GetState(Addresses.Market));
            Assert.Contains(AvatarAddress, marketState.AvatarAddresses);

            var productsState =
                new ProductsState((List)nextState.GetState(ProductsState.DeriveAddress(AvatarAddress)));
            var random = new TestRandom();
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();
                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * Gold, product.Price);
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

            Assert.Equal(0 * asset.Currency, nextState.GetBalance(AvatarAddress, asset.Currency));
        }

        [Theory]
        [MemberData(nameof(Execute_Validate_MemberData))]
        public void Execute_Validate_RegisterInfos(params ValidateMember[] validateMembers)
        {
            foreach (var validateMember in validateMembers)
            {
                foreach (var registerInfo in validateMember.RegisterInfos)
                {
                    var action = new RegisterProduct
                    {
                        AvatarAddress = AvatarAddress,
                        RegisterInfos = new[] { registerInfo },
                    };
                    Assert.Throws(validateMember.Exc, () => action.Execute(new ActionContext
                    {
                        PreviousState = _initialState,
                        Random = new TestRandom(),
                        Signer = _agentAddress,
                    }));
                }
            }
        }

        [Theory]
        // not enough block index.
        [InlineData(ProductType.Fungible, 1, 2L, 1L, false)]
        [InlineData(ProductType.NonFungible, 1, 4L, 3L, false)]
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

            _initialState = _initialState.SetState(AvatarAddress, _avatarState.Serialize());
            var action = new RegisterProduct
            {
                AvatarAddress = AvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = itemCount,
                        Price = 1 * Gold,
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
                PreviousState = _initialState,
            }));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var registerInfos = new List<RegisterInfo>();
            for (int i = 0; i < RegisterProduct.Capacity + 1; i++)
            {
                registerInfos.Add(new RegisterInfo());
            }

            var action = new RegisterProduct
            {
                AvatarAddress = _avatarState.address,
                RegisterInfos = registerInfos,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(new ActionContext()));
        }

        public class ValidateMember
        {
            public IEnumerable<IRegisterInfo> RegisterInfos { get; set; }

            public Type Exc { get; set; }
        }
    }
}
