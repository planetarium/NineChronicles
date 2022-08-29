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
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
#pragma warning restore CS0618
                true,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 18, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, new PrivateKey().ToAddress())),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("ETH", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("BTC", 18, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("BTC", 2, new Address("47d082a115c63e7b58b1532d20e631538eafadde"))),
                false,
            };
            #pragma warning restore CS0618
        }

        public static IEnumerable<object[]> IsPreviewNetTestcases()
        {
            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
#pragma warning restore CS0618
                true,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 18, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, new PrivateKey().ToAddress())),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("ETH", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("BTC", 18, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
#pragma warning restore CS0618
                false,
            };

            yield return new object[]
            {
                new GoldCurrencyState(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("BTC", 2, new Address("340f110b91d0577a9ae0ea69ce15269436f217da"))),
#pragma warning restore CS0618
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
