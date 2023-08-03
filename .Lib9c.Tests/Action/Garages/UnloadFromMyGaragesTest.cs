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
    using Nekoyume.Model.Mail;
    using Xunit;

    public class UnloadFromMyGaragesTest
    {
        private static readonly Address AgentAddr = new PrivateKey().ToAddress();
        private static readonly int AvatarIndex = 0;

        private static readonly Address AvatarAddr =
            Addresses.GetAvatarAddress(AgentAddr, AvatarIndex);

        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;
        private readonly Address _recipientAvatarAddr;
        private readonly (Address balanceAddr, FungibleAssetValue value)[] _fungibleAssetValues;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly IFungibleItem[] _fungibleItems;
        private readonly IAccountStateDelta _previousStates;

        public UnloadFromMyGaragesTest()
        {
            // NOTE: Garage actions does not consider the avatar state v1.
            (
                _tableSheets,
                _,
                _,
                _,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates(
                agentAddr: AgentAddr,
                avatarIndex: AvatarIndex);
            _ncg = _initialStatesWithAvatarStateV2.GetGoldCurrency();
            (
                _recipientAvatarAddr,
                _fungibleAssetValues,
                _fungibleIdAndCounts,
                _fungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var avatarAddr = Addresses.GetAvatarAddress(AgentAddr, AvatarIndex);
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddr, avatarAddr);

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
                avatarAddr,
                fungibleAssetValues,
                fungibleIdAndCounts,
                "memo",
            };
        }

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            Address recipientAvatarAddr,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            var actions = new[]
            {
                new UnloadFromMyGarages(),
                new UnloadFromMyGarages(
                    recipientAvatarAddr,
                    fungibleAssetValues,
                    fungibleIdAndCounts,
                    memo),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new UnloadFromMyGarages();
                des.LoadPlainValue(ser);
                Assert.Equal(action.RecipientAvatarAddr, des.RecipientAvatarAddr);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                            des.FungibleAssetValues is null);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                            des.FungibleIdAndCounts is null);
                Assert.Equal(action.Memo, des.Memo);

                Assert.Equal(ser, des.PlainValue);

                var actionInter = (IUnloadFromMyGaragesV1)action;
                var desInter = (IUnloadFromMyGaragesV1)des;
                Assert.True(
                    actionInter.FungibleAssetValues?.SequenceEqual(desInter.FungibleAssetValues!) ??
                    desInter.FungibleAssetValues is null);
                Assert.Equal(actionInter.RecipientAvatarAddr, desInter.RecipientAvatarAddr);
                Assert.True(
                    actionInter.FungibleIdAndCounts?.SequenceEqual(desInter.FungibleIdAndCounts!) ??
                    desInter.FungibleIdAndCounts is null);
                Assert.Equal(actionInter.Memo, desInter.Memo);
            }
        }

        [Fact]
        public void Execute_Success()
        {
            const long blockIndex = 0L;
            const string memo = "memo";
            var (action, nextStates) = Execute(
                AgentAddr,
                blockIndex,
                _previousStates,
                new TestRandom(),
                _recipientAvatarAddr,
                _fungibleAssetValues,
                _fungibleIdAndCounts,
                memo);
            var garageBalanceAddr =
                Addresses.GetGarageBalanceAddress(AgentAddr);
            if (action.FungibleAssetValues is { })
            {
                foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                {
                    Assert.Equal(
                        value,
                        nextStates.GetBalance(balanceAddr, value.Currency));
                    Assert.Equal(
                        value.Currency * 0,
                        nextStates.GetBalance(garageBalanceAddr, value.Currency));
                }
            }

            if (action.FungibleIdAndCounts is { })
            {
                var inventoryAddr = _recipientAvatarAddr.Derive(SerializeKeys.LegacyInventoryKey);
                var inventory = nextStates.GetInventory(inventoryAddr);
                foreach (var (fungibleId, count) in action.FungibleIdAndCounts)
                {
                    var garageAddr = Addresses.GetGarageAddress(
                        AgentAddr,
                        fungibleId);
                    Assert.Equal(0, new FungibleItemGarage(nextStates.GetState(garageAddr)).Count);
                    Assert.True(inventory.HasFungibleItem(
                        fungibleId,
                        blockIndex: 0,
                        count));
                }
            }

            var avatarDict = (Dictionary)nextStates.GetState(_recipientAvatarAddr)!;
            var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey]);
            Assert.Single(mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(mailBox.First());
            Assert.Equal(blockIndex, mail.blockIndex);
            Assert.Equal(blockIndex, mail.requiredBlockIndex);
            Assert.True(action.FungibleAssetValues?.SequenceEqual(mail.FungibleAssetValues!) ??
                        mail.FungibleAssetValues is null);
            Assert.True(action.FungibleIdAndCounts?.SequenceEqual(mail.FungibleIdAndCounts!) ??
                        mail.FungibleIdAndCounts is null);
            Assert.Equal(action.Memo, mail.Memo);
        }

        [Fact]
        public void Execute_Throws_InvalidActionFieldException()
        {
            // FungibleAssetValues and FungibleIdAndCounts are null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAvatarAddr,
                null,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(tuple =>
                (tuple.balanceAddr, tuple.value * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAvatarAddr,
                negativeFungibleAssetValues,
                null));

            // Count of fungible id is negative.
            var negativeFungibleIdAndCounts = _fungibleIdAndCounts.Select(tuple => (
                tuple.fungibleId,
                tuple.count * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _recipientAvatarAddr,
                null,
                negativeFungibleIdAndCounts));
        }

        [Fact]
        public void Execute_Throws_Exception()
        {
            // Agent's FungibleAssetValue garages does not have enough balance.
            var previousStatesWithEmptyBalances = _previousStates;
            var actionContext = new ActionContext { Signer = AgentAddr };
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddr);
            foreach (var (_, value) in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances.BurnAsset(
                    actionContext,
                    garageBalanceAddress,
                    value);
            }

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithEmptyBalances,
                new TestRandom(),
                _recipientAvatarAddr,
                _fungibleAssetValues,
                null));

            // Inventory state is null.
            var inventoryAddr = _recipientAvatarAddr.Derive(SerializeKeys.LegacyInventoryKey);
            var previousStatesWithNullInventoryState =
                _previousStates.SetState(inventoryAddr, Null.Value);
            Assert.Throws<StateNullException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNullInventoryState,
                new TestRandom(),
                _recipientAvatarAddr,
                null,
                _fungibleIdAndCounts));

            // The state in InventoryAddr is not Inventory.
            foreach (var invalidInventoryState in new IValue[]
                     {
                         new Integer(0),
                         Dictionary.Empty,
                     })
            {
                var previousStatesWithInvalidInventoryState =
                    _previousStates.SetState(inventoryAddr, invalidInventoryState);
                Assert.Throws<InvalidCastException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidInventoryState,
                    new TestRandom(),
                    _recipientAvatarAddr,
                    null,
                    _fungibleIdAndCounts));
            }

            // Agent's fungible item garage state is null.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    AgentAddr,
                    fungibleId);
                var previousStatesWithNullGarageState =
                    _previousStates.SetState(garageAddr, Null.Value);
                Assert.Throws<StateNullException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithNullGarageState,
                    new TestRandom(),
                    _recipientAvatarAddr,
                    null,
                    _fungibleIdAndCounts));
            }

            // Agent's fungible item garage does not contain enough items.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    AgentAddr,
                    fungibleId);
                var garageState = _previousStates.GetState(garageAddr);
                var garage = new FungibleItemGarage(garageState);
                garage.Unload(1);
                var previousStatesWithNotEnoughCountOfGarageState =
                    _previousStates.SetState(garageAddr, garage.Serialize());

                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithNotEnoughCountOfGarageState,
                    new TestRandom(),
                    _recipientAvatarAddr,
                    null,
                    _fungibleIdAndCounts));
            }
        }

        private static (UnloadFromMyGarages action, IAccountStateDelta nextStates) Execute(
            Address signer,
            long blockIndex,
            IAccountStateDelta previousState,
            IRandom random,
            Address recipientAvatarAddr,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo = null)
        {
            var action = new UnloadFromMyGarages(
                recipientAvatarAddr,
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

        private static (Address balanceAddr, FungibleAssetValue value)[]
            GetFungibleAssetValues(
                Address agentAddr,
                Address avatarAddr)
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav => fav.Sign > 0)
                .Select(fav =>
                {
                    if (Currencies.IsRuneTicker(fav.Currency.Ticker) ||
                        Currencies.IsSoulstoneTicker(fav.Currency.Ticker))
                    {
                        return (avatarAddr, fav);
                    }

                    return (agentAddr, fav);
                })
                .ToArray();
        }

        private (
            Address recipientAvatarAddr,
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            IFungibleItem[] _fungibleItems,
            IAccountStateDelta previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var actionContext = new ActionContext { Signer = Addresses.Admin };
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddr);
            var fungibleAssetValues = GetFungibleAssetValues(
                AgentAddr,
                AvatarAddr);
            foreach (var (_, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        actionContext,
                        Addresses.Admin,
                        garageBalanceAddress,
                        value);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    actionContext,
                    garageBalanceAddress,
                    value);
            }

            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateMaterial)
                .Select((material, index) =>
                {
                    var garageAddr = Addresses.GetGarageAddress(
                        AgentAddr,
                        material.FungibleId);
                    var count = index + 1;
                    var garage = new FungibleItemGarage(material, count);
                    previousStates = previousStates.SetState(
                        garageAddr,
                        garage.Serialize());

                    return (fungibleItem: (IFungibleItem)material, count);
                }).ToArray();
            return (
                recipientAvatarAddr: AvatarAddr,
                fungibleAssetValues,
                fungibleItemAndCounts
                    .Select(tuple => (tuple.fungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                fungibleItemAndCounts.Select(tuple => tuple.fungibleItem).ToArray(),
                previousStates
            );
        }
    }
}
