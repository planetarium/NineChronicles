namespace Lib9c.Tests.Action.Garages
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Abstractions;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Garages;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Xunit;

    public class DeliverToOthersGaragesTest
    {
        private const int AvatarIndex = 0;
        private static readonly Address SenderAgentAddr = Addresses.Admin;

        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;
        private readonly Address _recipientAgentAddr;
        private readonly FungibleAssetValue[] _fungibleAssetValues;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly ITradableFungibleItem[] _tradableFungibleItems;
        private readonly IAccountStateDelta _previousStates;

        public DeliverToOthersGaragesTest()
        {
            // NOTE: Garage actions does not consider the avatar state v1.
            (
                _tableSheets,
                _,
                _,
                _,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates(
                agentAddr: SenderAgentAddr,
                avatarIndex: AvatarIndex);
            _ncg = _initialStatesWithAvatarStateV2.GetGoldCurrency();
            (
                _recipientAgentAddr,
                _fungibleAssetValues,
                _fungibleIdAndCounts,
                _tradableFungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var recipientAddr = new PrivateKey().ToAddress();
            var fungibleAssetValues = GetFungibleAssetValues();
            var hex = string.Join(
                string.Empty,
                Enumerable.Range(0, 64).Select(i => (i % 10).ToString()));
            var fungibleIdAndCounts = new[]
            {
                (HashDigest<SHA256>.FromString(hex), 1),
                (HashDigest<SHA256>.FromString(hex), int.MaxValue),
            };

            yield return new object[]
            {
                recipientAddr,
                fungibleAssetValues,
                fungibleIdAndCounts,
                "memo",
            };
        }

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            Address recipientAddr,
            FungibleAssetValue[] fungibleAssetValues,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            string? memo)
        {
            var actions = new[]
            {
                new DeliverToOthersGarages(),
                new DeliverToOthersGarages(
                    recipientAddr,
                    fungibleAssetValues,
                    fungibleIdAndCounts,
                    memo),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new DeliverToOthersGarages();
                des.LoadPlainValue(ser);
                Assert.Equal(action.RecipientAgentAddr, des.RecipientAgentAddr);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                            des.FungibleAssetValues is null);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                            des.FungibleIdAndCounts is null);
                Assert.Equal(action.Memo, des.Memo);
                Assert.Equal(ser, des.PlainValue);

                var actionInter = (IDeliverToOthersGaragesV1)action;
                var desInter = (IDeliverToOthersGaragesV1)des;
                Assert.Equal(actionInter.RecipientAgentAddr, desInter.RecipientAgentAddr);
                Assert.True(
                    actionInter.FungibleAssetValues?.SequenceEqual(desInter.FungibleAssetValues!) ??
                    desInter.FungibleAssetValues is null);
                Assert.True(
                    actionInter.FungibleIdAndCounts?.SequenceEqual(desInter.FungibleIdAndCounts!) ??
                    desInter.FungibleIdAndCounts is null);
                Assert.Equal(actionInter.Memo, desInter.Memo);
            }
        }

        [Fact]
        public void Execute_Success()
        {
            var (action, nextStates) = Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAgentAddr,
                _fungibleAssetValues,
                _fungibleIdAndCounts,
                "memo");
            if (action.FungibleAssetValues is { })
            {
                var senderGarageBalanceAddr =
                    Addresses.GetGarageBalanceAddress(SenderAgentAddr);
                var recipientGarageBalanceAddr =
                    Addresses.GetGarageBalanceAddress(_recipientAgentAddr);
                foreach (var fav in action.FungibleAssetValues)
                {
                    Assert.Equal(
                        fav.Currency * 0,
                        nextStates.GetBalance(senderGarageBalanceAddr, fav.Currency));
                    Assert.Equal(
                        fav,
                        nextStates.GetBalance(recipientGarageBalanceAddr, fav.Currency));
                }
            }

            if (action.FungibleIdAndCounts is null)
            {
                return;
            }

            foreach (var (fungibleId, count) in action.FungibleIdAndCounts)
            {
                var senderGarageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                Assert.Equal(
                    0,
                    new FungibleItemGarage(nextStates.GetState(senderGarageAddr)).Count
                );
                var recipientGarageAddr = Addresses.GetGarageAddress(
                    _recipientAgentAddr,
                    fungibleId);
                Assert.Equal(
                    count,
                    new FungibleItemGarage(nextStates.GetState(recipientGarageAddr)).Count);
            }
        }

        [Fact]
        public void Execute_Throws_InvalidActionFieldException()
        {
            // FungibleAssetValues and FungibleIdAndCounts are null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAgentAddr,
                null,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(fav => fav * -1);
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAgentAddr,
                negativeFungibleAssetValues,
                null));

            // Count of fungible id is negative.
            var negativeFungibleIdAndCounts = _fungibleIdAndCounts.Select(tuple => (
                tuple.fungibleId,
                tuple.count * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAgentAddr,
                null,
                negativeFungibleIdAndCounts));
        }

        [Fact]
        public void Execute_Throws_Exception()
        {
            // Sender's FungibleAssetValue Garage does not have enough balance.
            var previousStatesWithEmptyBalances = _previousStates;
            var actionContext = new ActionContext { Signer = SenderAgentAddr };
            var senderFungibleAssetValueGarageAddr =
                Addresses.GetGarageBalanceAddress(SenderAgentAddr);
            foreach (var vaf in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances.BurnAsset(
                    actionContext,
                    senderFungibleAssetValueGarageAddr,
                    vaf);
            }

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                SenderAgentAddr,
                0,
                previousStatesWithEmptyBalances,
                new TestRandom(),
                _recipientAgentAddr,
                _fungibleAssetValues,
                null));

            // Sender's fungible item Garage state is null.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                var previousStatesWithNullGarageState =
                    _previousStates.SetState(garageAddr, Null.Value);
                Assert.Throws<StateNullException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithNullGarageState,
                    new TestRandom(),
                    _recipientAgentAddr,
                    null,
                    _fungibleIdAndCounts));
            }

            // Mismatch fungible id between sender's and recipient's fungible item Garage.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(_recipientAgentAddr, fungibleId);
                var nextIndex = (i + 1) % _fungibleIdAndCounts.Length;
                var garage = new FungibleItemGarage(_tradableFungibleItems[nextIndex], 1);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(addr, garage.Serialize());
                Assert.Throws<ArgumentException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    _recipientAgentAddr,
                    null,
                    _fungibleIdAndCounts));
            }

            // Sender's fungible item Garage does not contain enough items.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                var garageState = _previousStates.GetState(garageAddr);
                var garage = new FungibleItemGarage(garageState);
                garage.Unload(1);
                var previousStatesWithNotEnoughCountOfGarageState =
                    _previousStates.SetState(garageAddr, garage.Serialize());

                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithNotEnoughCountOfGarageState,
                    new TestRandom(),
                    _recipientAgentAddr,
                    null,
                    _fungibleIdAndCounts));
            }

            // Recipient's fungible item Garages can be overflowed.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(_recipientAgentAddr, fungibleId);
                var garage = new FungibleItemGarage(_tradableFungibleItems[i], int.MaxValue);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(addr, garage.Serialize());
                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    _recipientAgentAddr,
                    null,
                    _fungibleIdAndCounts));
            }
        }

        private static (DeliverToOthersGarages action, IAccountStateDelta nextStates) Execute(
            Address signer,
            long blockIndex,
            IAccountStateDelta previousState,
            IRandom random,
            Address recipientAgentAddr,
            IEnumerable<FungibleAssetValue>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo = null)
        {
            var action = new DeliverToOthersGarages(
                recipientAgentAddr,
                fungibleAssetValues,
                fungibleIdAndCounts,
                memo);
            return (
                action,
                action.Execute(new ActionContext
                {
                    Signer = signer,
                    BlockIndex = blockIndex,
                    Rehearsal = false,
                    PreviousState = previousState,
                    Random = random,
                }));
        }

        private static FungibleAssetValue[] GetFungibleAssetValues()
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav => fav.Sign > 0)
                .ToArray();
        }

        private (
            Address recipientAddr,
            FungibleAssetValue[] fungibleAssetValues,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            ITradableFungibleItem[] _tradableFungibleItems,
            IAccountStateDelta previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var actionContext = new ActionContext { Signer = Addresses.Admin };
            var senderFavGarageBalanceAddr =
                Addresses.GetGarageBalanceAddress(SenderAgentAddr);
            var fungibleAssetValues = GetFungibleAssetValues();
            foreach (var fav in fungibleAssetValues)
            {
                if (fav.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        actionContext,
                        Addresses.Admin,
                        senderFavGarageBalanceAddr,
                        fav);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    actionContext,
                    senderFavGarageBalanceAddr,
                    fav);
            }

            var fungibleIdAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateTradableMaterial)
                .Select((tradableMaterial, index) =>
                {
                    var senderGarageAddr = Addresses.GetGarageAddress(
                        SenderAgentAddr,
                        tradableMaterial.FungibleId);
                    var garageState = previousStates.GetState(senderGarageAddr);
                    var garage = garageState is null
                        ? new FungibleItemGarage(tradableMaterial, 0)
                        : new FungibleItemGarage(garageState);
                    garage.Load(index + 1);
                    previousStates = previousStates
                        .SetState(senderGarageAddr, garage.Serialize());

                    return (
                        tradableFungibleItem: (ITradableFungibleItem)tradableMaterial,
                        count: index + 1);
                }).ToArray();
            return (
                new PrivateKey().ToAddress(),
                fungibleAssetValues,
                fungibleIdAndCounts
                    .Select(tuple => (tuple.tradableFungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                fungibleIdAndCounts.Select(tuple => tuple.tradableFungibleItem).ToArray(),
                previousStates);
        }
    }
}
