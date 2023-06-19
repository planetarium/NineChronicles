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
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Libplanet.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Garages;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
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
        private readonly (Address balanceAddr, FungibleAssetValue value)[] _fungibleAssetValues;
        private readonly Address? _inventoryAddr;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly ITradableFungibleItem[] _tradableFungibleItems;
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
                _fungibleAssetValues,
                _inventoryAddr,
                _fungibleIdAndCounts,
                _tradableFungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue() =>
            LoadIntoMyGaragesTest.Get_Sample_PlainValue();

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            var actions = new[]
            {
                new UnloadFromMyGarages(),
                new UnloadFromMyGarages(
                    fungibleAssetValues,
                    inventoryAddr,
                    fungibleIdAndCounts,
                    memo),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new UnloadFromMyGarages();
                des.LoadPlainValue(ser);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                            des.FungibleAssetValues is null);
                Assert.Equal(action.InventoryAddr, des.InventoryAddr);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                            des.FungibleIdAndCounts is null);
                Assert.Equal(action.Memo, des.Memo);

                Assert.Equal(ser, des.PlainValue);

                var actionInter = (IUnloadFromMyGarages)action;
                var desInter = (IUnloadFromMyGarages)des;
                Assert.True(
                    actionInter.FungibleAssetValues?.SequenceEqual(desInter.FungibleAssetValues!) ??
                    desInter.FungibleAssetValues is null);
                Assert.Equal(actionInter.InventoryAddr, desInter.InventoryAddr);
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
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _fungibleAssetValues,
                _inventoryAddr,
                _fungibleIdAndCounts,
                "memo");
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

            if (action.InventoryAddr is null ||
                action.FungibleIdAndCounts is null)
            {
                return;
            }

            var inventoryState = nextStates.GetState(action.InventoryAddr.Value)!;
            var inventory = new Inventory((List)inventoryState);
            foreach (var (fungibleId, count) in action.FungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    AgentAddr,
                    fungibleId);
                Assert.True(nextStates.GetState(garageAddr) is Null);
                Assert.True(inventory.HasTradableFungibleItem(
                    fungibleId,
                    requiredBlockIndex: null,
                    blockIndex: 0,
                    count));
            }
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
                null,
                _inventoryAddr,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(tuple =>
                (tuple.balanceAddr, tuple.value * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                negativeFungibleAssetValues,
                _inventoryAddr,
                null));

            // InventoryAddr is null when FungibleIdAndCounts is not null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                null,
                _fungibleIdAndCounts));

            // Count of fungible id is negative.
            var negativeFungibleIdAndCounts = _fungibleIdAndCounts.Select(tuple => (
                tuple.fungibleId,
                tuple.count * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                _inventoryAddr,
                negativeFungibleIdAndCounts));
        }

        [Fact]
        public void Execute_Throws_Exception()
        {
            // Agent's FungibleAssetValue garages does not have enough balance.
            var previousStatesWithEmptyBalances = _previousStates;
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddr);
            foreach (var (_, value) in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances
                    .BurnAsset(garageBalanceAddress, value);
            }

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithEmptyBalances,
                new TestRandom(),
                _fungibleAssetValues,
                null,
                null));

            // Inventory state is null.
            var previousStatesWithNullInventoryState =
                _previousStates.SetState(_inventoryAddr!.Value, Null.Value);
            Assert.Throws<StateNullException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNullInventoryState,
                new TestRandom(),
                null,
                _inventoryAddr,
                _fungibleIdAndCounts));

            // The state in InventoryAddr is not Inventory.
            foreach (var invalidInventoryState in new IValue[]
                     {
                         new Integer(0),
                         Dictionary.Empty,
                     })
            {
                var previousStatesWithInvalidInventoryState =
                    _previousStates.SetState(_inventoryAddr.Value, invalidInventoryState);
                Assert.Throws<InvalidCastException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidInventoryState,
                    new TestRandom(),
                    null,
                    _inventoryAddr,
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
                    null,
                    _inventoryAddr,
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
                if (garage.Count == 0)
                {
                    Assert.Throws<StateNullException>(() => Execute(
                        AgentAddr,
                        0,
                        previousStatesWithNotEnoughCountOfGarageState,
                        new TestRandom(),
                        null,
                        _inventoryAddr,
                        _fungibleIdAndCounts));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                        AgentAddr,
                        0,
                        previousStatesWithNotEnoughCountOfGarageState,
                        new TestRandom(),
                        null,
                        _inventoryAddr,
                        _fungibleIdAndCounts));
                }
            }

            // Inventory can be overflowed.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var item = _tradableFungibleItems[i];
                var inventory = _previousStates.GetInventory(_inventoryAddr.Value);
                inventory.AddTradableFungibleItem(item, int.MaxValue);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(_inventoryAddr.Value, inventory.Serialize());
                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    null,
                    _inventoryAddr,
                    _fungibleIdAndCounts));
            }
        }

        private static (UnloadFromMyGarages action, IAccountStateDelta nextStates) Execute(
            Address signer,
            long blockIndex,
            IAccountStateDelta previousStates,
            IRandom random,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo = null)
        {
            var action = new UnloadFromMyGarages(
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                memo);
            return (
                action,
                action.Execute(new ActionContext
                {
                    Signer = signer,
                    BlockIndex = blockIndex,
                    Rehearsal = false,
                    PreviousStates = previousStates,
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
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            Address? inventoryAddr,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            ITradableFungibleItem[] _tradableFungibleItems,
            IAccountStateDelta previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddr);
            var fungibleAssetValues = GetFungibleAssetValues(
                AgentAddr,
                AvatarAddr);
            foreach (var (_, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        Addresses.Admin,
                        garageBalanceAddress,
                        value);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    garageBalanceAddress,
                    value);
            }

            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateTradableMaterial)
                .Select((tradableMaterial, index) =>
                {
                    var garageAddr = Addresses.GetGarageAddress(
                        AgentAddr,
                        tradableMaterial.FungibleId);
                    var count = index + 1;
                    var garage = new FungibleItemGarage(tradableMaterial, count);
                    previousStates = previousStates.SetState(
                        garageAddr,
                        garage.Serialize());

                    return (
                        tradableFungibleItem: (ITradableFungibleItem)tradableMaterial,
                        count);
                }).ToArray();
            return (
                fungibleAssetValues,
                inventoryAddr: Addresses.GetInventoryAddress(
                    AgentAddr,
                    AvatarIndex),
                fungibleItemAndCounts
                    .Select(tuple => (tuple.tradableFungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                fungibleItemAndCounts.Select(tuple => tuple.tradableFungibleItem).ToArray(),
                previousStates
            );
        }
    }
}
