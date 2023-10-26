namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class MintAssetsTest
    {
        private readonly PrivateKey _ncgMinter;
        private readonly Currency _ncgCurrency;
        private readonly IAccount _prevState;

        public MintAssetsTest()
        {
            _ncgMinter = new PrivateKey();
            _ncgCurrency = Currency.Legacy(
                "NCG",
                2,
                _ncgMinter.ToAddress()
            );
            _prevState = new Account(
                MockState.Empty.SetState(AdminState.Address, new AdminState(_ncgMinter.ToAddress(), 100).Serialize())
            );
        }

        [Fact]
        public void PlainValue()
        {
            var r = new List<(Address recipient, FungibleAssetValue amount)>()
            {
                (default, _ncgCurrency * 100),
                (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000),
            };
            var act = new MintAssets(r);
            var expected = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize()))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize())));
            Assert.Equal(
                expected,
                act.PlainValue
            );
        }

        [Fact]
        public void LoadPlainValue()
        {
            var pv = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize()))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize())));
            var act = new MintAssets();
            act.LoadPlainValue(pv);

            var expected = new List<(Address recipient, FungibleAssetValue amount)>()
            {
                (default, _ncgCurrency * 100),
                (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000),
            };
            Assert.Equal(
                expected,
                act.FungibleAssetValues
            );
        }

        [Fact]
        public void Execute()
        {
            var action = new MintAssets(
                new List<(Address recipient, FungibleAssetValue amount)>()
                {
                    (default, _ncgCurrency * 100),
                    (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000),
                }
            );
            IAccount nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _ncgMinter.ToAddress(),
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(
                _ncgCurrency * 100,
                nextState.GetBalance(default, _ncgCurrency)
            );
            Assert.Equal(
                _ncgCurrency * 1000,
                nextState.GetBalance(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency)
            );
        }

        [Fact]
        public void Execute_Throws_PermissionDeniedException()
        {
            var action = new MintAssets(
                new List<(Address recipient, FungibleAssetValue amount)>()
                {
                    (default, _ncgCurrency * 100),
                    (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000),
                }
            );
            Assert.Throws<PermissionDeniedException>(() => action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = default,
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            ));
        }
    }
}
