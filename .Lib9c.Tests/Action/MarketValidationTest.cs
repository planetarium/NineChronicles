namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Helper;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Xunit;

    public class MarketValidationTest
    {
        private static readonly Address AgentAddress = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
        private static readonly Address AvatarAddress = new Address("47d082a115c63e7b58b1532d20e631538eafadde");
        private static readonly Currency Gold = Currency.Legacy("NCG", 2, minters: null);

        private readonly IAccountStateDelta _initialState;

        public MarketValidationTest()
        {
            _initialState = new MockStateDelta()
                .SetState(GoldCurrencyState.Address, new GoldCurrencyState(Gold).Serialize());
        }

        public static IEnumerable<object[]> RegisterInfosMemberData()
        {
            yield return new object[]
            {
                new RegisterInfosMember
                {
                    RegisterInfos = new List<IRegisterInfo>(),
                    Exc = typeof(ListEmptyException),
                },
                new RegisterInfosMember
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
                new RegisterInfosMember
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
                new RegisterInfosMember
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
                new RegisterInfosMember
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
                new RegisterInfosMember
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

        public static IEnumerable<object[]> ProductInfosMemberData()
        {
            yield return new object[]
            {
                new ProductInfosMember
                {
                    ProductInfos = new List<IProductInfo>(),
                    Exc = typeof(ListEmptyException),
                },
                new ProductInfosMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new FavProductInfo
                        {
                            Type = ProductType.NonFungible,
                        },
                        new FavProductInfo
                        {
                            Type = ProductType.Fungible,
                        },
                        new ItemProductInfo
                        {
                            Type = ProductType.FungibleAssetValue,
                        },
                    },
                    Exc = typeof(InvalidProductTypeException),
                },
            };
        }

        [Theory]
        [MemberData(nameof(RegisterInfosMemberData))]
        public void Validate_RegisterInfo(params RegisterInfosMember[] validateMembers)
        {
            var actionContext = new ActionContext
            {
                Signer = AgentAddress,
                PreviousState = _initialState,
                Random = new TestRandom(),
            };
            foreach (var validateMember in validateMembers)
            {
                foreach (var registerInfo in validateMember.RegisterInfos)
                {
                    var registerProduct = new RegisterProduct
                    {
                        AvatarAddress = AvatarAddress,
                        RegisterInfos = new[] { registerInfo },
                    };
                    Assert.Throws(validateMember.Exc, () => registerProduct.Execute(actionContext));

                    var reRegister = new ReRegisterProduct
                    {
                        AvatarAddress = AvatarAddress,
                        ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                        {
                            (new FavProductInfo(), registerInfo),
                        },
                    };
                    Assert.Throws(validateMember.Exc, () => reRegister.Execute(actionContext));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ProductInfosMemberData))]
        public void Validate_ProductInfo(params ProductInfosMember[] validateMembers)
        {
            var actionContext = new ActionContext
            {
                Signer = AgentAddress,
                PreviousState = _initialState,
                Random = new TestRandom(),
            };
            foreach (var validateMember in validateMembers)
            {
                foreach (var productInfo in validateMember.ProductInfos)
                {
                    var buyProduct = new BuyProduct
                    {
                        AvatarAddress = AvatarAddress,
                        ProductInfos = new[] { productInfo },
                    };

                    Assert.Throws(validateMember.Exc, () => buyProduct.Execute(actionContext));

                    var cancelRegister = new CancelProductRegistration
                    {
                        AvatarAddress = AvatarAddress,
                        ProductInfos = new List<IProductInfo>() { productInfo },
                    };

                    Assert.Throws(validateMember.Exc, () => cancelRegister.Execute(actionContext));

                    var reRegister = new ReRegisterProduct
                    {
                        AvatarAddress = AvatarAddress,
                        ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                        {
                            (
                                productInfo,
                                new AssetInfo
                                {
                                    AvatarAddress = AvatarAddress,
                                    Asset = 1 * RuneHelper.StakeRune,
                                    Price = 1 * Gold,
                                    Type = ProductType.FungibleAssetValue,
                                }
                            ),
                        },
                    };

                    Assert.Throws(validateMember.Exc, () => reRegister.Execute(actionContext));
                }
            }
        }

        public class RegisterInfosMember
        {
            public IEnumerable<IRegisterInfo> RegisterInfos { get; set; }

            public Type Exc { get; set; }
        }

        public class ProductInfosMember
        {
            public IEnumerable<IProductInfo> ProductInfos { get; set; }

            public Type Exc { get; set; }
        }
    }
}
