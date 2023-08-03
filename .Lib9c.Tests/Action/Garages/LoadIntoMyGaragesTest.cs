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

    public class LoadIntoMyGaragesTest
    {
        private const int AvatarIndex = 0;
        private static readonly Address AgentAddr = Addresses.Admin;

        private readonly TableSheets _tableSheets;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;
        private readonly (Address balanceAddr, FungibleAssetValue value)[] _fungibleAssetValues;
        private readonly Address? _inventoryAddr;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly FungibleAssetValue _cost;
        private readonly ITradableFungibleItem[] _tradableFungibleItems;
        private readonly IAccountStateDelta _previousStates;

        public LoadIntoMyGaragesTest()
        {
            // NOTE: Garage actions does not consider the avatar state v1.
            (
                _tableSheets,
                _,
                _avatarAddress,
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
                _cost,
                _tradableFungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var avatarAddr = Addresses.GetAvatarAddress(AgentAddr, AvatarIndex);
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddr, avatarAddr);
            var inventoryAddr = Addresses.GetInventoryAddress(Addresses.Admin, AvatarIndex);

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
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                "memo",
            };
        }

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            Address inventoryAddr,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            string? memo)
        {
            var actions = new[]
            {
                new LoadIntoMyGarages(),
                new LoadIntoMyGarages(
                    fungibleAssetValues,
                    inventoryAddr,
                    fungibleIdAndCounts,
                    memo),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new LoadIntoMyGarages();
                des.LoadPlainValue(ser);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                            des.FungibleAssetValues is null);
                Assert.Equal(action.InventoryAddr, des.InventoryAddr);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                            des.FungibleIdAndCounts is null);
                Assert.Equal(action.Memo, des.Memo);
                Assert.Equal(ser, des.PlainValue);

                var actionInter = (ILoadIntoMyGaragesV1)action;
                var desInter = (ILoadIntoMyGaragesV1)des;
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
            Assert.Equal(
                new FungibleAssetValue(Currencies.Garage),
                nextStates.GetBalance(AgentAddr, Currencies.Garage));
            Assert.Equal(
                _cost,
                nextStates.GetBalance(Addresses.GarageWallet, Currencies.Garage));
            var garageBalanceAddr =
                Addresses.GetGarageBalanceAddress(AgentAddr);
            if (action.FungibleAssetValues is { })
            {
                foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                {
                    Assert.Equal(
                        value.Currency * 0,
                        nextStates.GetBalance(balanceAddr, value.Currency));
                    Assert.Equal(
                        value,
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
                Assert.False(inventory.HasTradableFungibleItem(
                    fungibleId,
                    requiredBlockIndex: null,
                    blockIndex: 0,
                    1));
                var garageAddr = Addresses.GetGarageAddress(
                    AgentAddr,
                    fungibleId);
                var garage = new FungibleItemGarage(nextStates.GetState(garageAddr));
                Assert.Equal(fungibleId, garage.Item.FungibleId);
                Assert.Equal(count, garage.Count);
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
                null,
                null));

            // Signer does not have permission of balance address.
            var invalidSignerAddr = new PrivateKey().ToAddress();
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                invalidSignerAddr,
                0,
                _previousStates,
                new TestRandom(),
                _fungibleAssetValues,
                null,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(tuple => (
                tuple.balanceAddr,
                tuple.value * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                negativeFungibleAssetValues,
                null,
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

            // AgentAddr does not have permission of inventory address.
            var invalidInventoryAddr = new PrivateKey().ToAddress();
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                invalidInventoryAddr,
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
            // Balance does not enough to pay cost.
            var previousStatesWithNotEnoughCost = _previousStates.BurnAsset(
                new ActionContext { Signer = AgentAddr },
                AgentAddr,
                new FungibleAssetValue(Currencies.Garage, 1, 0));
            Assert.Throws<InsufficientBalanceException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNotEnoughCost,
                new TestRandom(),
                _fungibleAssetValues,
                _inventoryAddr,
                _fungibleIdAndCounts));

            // Balance does not enough to send.
            var previousStatesWithEmptyBalances = _previousStates;
            foreach (var (balanceAddr, value) in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances.BurnAsset(
                    new ActionContext { Signer = AgentAddr },
                    balanceAddr,
                    value);
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

            // Inventory does not contain the tradable fungible item.
            var previousStatesWithEmptyInventoryState =
                _previousStates.SetState(_inventoryAddr.Value, new Inventory().Serialize());
            Assert.Throws<ItemNotFoundException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithEmptyInventoryState,
                new TestRandom(),
                null,
                _inventoryAddr,
                _fungibleIdAndCounts));

            // Inventory does not have enough tradable fungible item.
            var notEnoughInventory = _previousStates.GetInventory(_inventoryAddr.Value);
            foreach (var (fungibleId, count) in _fungibleIdAndCounts)
            {
                notEnoughInventory.RemoveTradableFungibleItem(
                    fungibleId,
                    requiredBlockIndex: null,
                    blockIndex: 0,
                    count - 1);
            }

            var previousStatesWithNotEnoughInventoryState =
                _previousStates.SetState(_inventoryAddr.Value, notEnoughInventory.Serialize());
            Assert.Throws<NotEnoughItemException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNotEnoughInventoryState,
                new TestRandom(),
                null,
                _inventoryAddr,
                _fungibleIdAndCounts));

            // Fungible item garage's item mismatch with fungible id.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(AgentAddr, fungibleId);
                var nextIndex = (i + 1) % _fungibleIdAndCounts.Length;
                var garage = new FungibleItemGarage(_tradableFungibleItems[nextIndex], 1);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(addr, garage.Serialize());
                Assert.Throws<Exception>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    null,
                    _inventoryAddr,
                    _fungibleIdAndCounts));
            }

            // Fungible item garages can be overflowed.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(AgentAddr, fungibleId);
                var garage = new FungibleItemGarage(_tradableFungibleItems[i], int.MaxValue);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(addr, garage.Serialize());
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

        private static (LoadIntoMyGarages action, IAccountStateDelta nextStates) Execute(
            Address signer,
            long blockIndex,
            IAccountStateDelta previousState,
            IRandom random,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo = null)
        {
            var action = new LoadIntoMyGarages(
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                memo);
            var context = new ActionContext
            {
                Signer = signer,
                BlockIndex = blockIndex,
                Rehearsal = false,
                PreviousState = previousState,
                Random = random,
            };
            return (action, action.Execute(context));
        }

        private static (Address balanceAddr, FungibleAssetValue value)[]
            GetFungibleAssetValues(
                Address agentAddr,
                Address avatarAddr,
                TableSheets? tableSheets = null)
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav =>
                    (tableSheets?.LoadIntoMyGaragesCostSheet.HasCost(fav.Currency.Ticker) ??
                     true) &&
                    fav.Sign > 0)
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
            FungibleAssetValue cost,
            ITradableFungibleItem[] _tradableFungibleItems,
            IAccountStateDelta previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var fungibleAssetValues = GetFungibleAssetValues(
                AgentAddr,
                _avatarAddress,
                _tableSheets);
            var actionContext = new ActionContext { Signer = Addresses.Admin };
            foreach (var (balanceAddr, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        actionContext,
                        Addresses.Admin,
                        balanceAddr,
                        value);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    actionContext,
                    balanceAddr,
                    value);
            }

            var inventoryAddr = Addresses.GetInventoryAddress(AgentAddr, AvatarIndex);
            var inventoryState = (List)previousStates.GetState(inventoryAddr)!;
            var inventory = new Inventory(inventoryState);
            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Where(row => _tableSheets.LoadIntoMyGaragesCostSheet.HasCost(row.ItemId))
                .Select(ItemFactory.CreateTradableMaterial)
                .Select((tradableMaterial, index) =>
                {
                    inventory.AddFungibleItem(tradableMaterial, index + 1);
                    return (
                        tradableFungibleItem: (ITradableFungibleItem)tradableMaterial,
                        count: index + 1);
                }).ToArray();
            var garageCost = _tableSheets.LoadIntoMyGaragesCostSheet.GetGarageCost(
                fungibleAssetValues.Select(tuple => tuple.value),
                fungibleItemAndCounts
                    .Select(tuple => (tuple.tradableFungibleItem.FungibleId, tuple.count)));
            previousStates = previousStates.MintAsset(
                new ActionContext { Signer = AgentAddr },
                AgentAddr,
                garageCost);
            return (
                fungibleAssetValues,
                inventoryAddr,
                fungibleItemAndCounts
                    .Select(tuple => (tuple.tradableFungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                garageCost,
                fungibleItemAndCounts.Select(tuple => tuple.tradableFungibleItem).ToArray(),
                previousStates.SetState(inventoryAddr, inventory.Serialize())
            );
        }
    }
}
