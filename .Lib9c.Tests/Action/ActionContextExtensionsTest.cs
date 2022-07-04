namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class ActionContextExtensionsTest
    {
        public static IEnumerable<object[]> IsMainNetTestcases()
        {
            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                true,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 18, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 2, new PrivateKey().ToAddress())),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("ETH", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("BTC", 18, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("BTC", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                false,
            };
        }

        public static IEnumerable<object[]> IsPreviewNetTestcases()
        {
            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
                true,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 18, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("NCG", 2, new PrivateKey().ToAddress())),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("ETH", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("BTC", 18, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
                    new Currency("BTC", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
                false,
            };
        }

        [Theory]
        [MemberData(nameof(IsMainNetTestcases))]
        public void IsMainNet(GoldCurrencyState goldCurrencyState, bool expected)
        {
            var state = new State().SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            IActionContext context = new ActionContext
            {
                PreviousStates = state,
            };

            Assert.Equal(expected, context.IsMainNet());
        }

        [Fact]
        public void Since()
        {
            Assert.True(new ActionContext
            {
                BlockIndex = 1001,
            }.Since(1000));

            Assert.True(new ActionContext
            {
                BlockIndex = 0,
            }.Since(0));

            Assert.False(new ActionContext
            {
                BlockIndex = 0,
            }.Since(1));
        }
    }
}
