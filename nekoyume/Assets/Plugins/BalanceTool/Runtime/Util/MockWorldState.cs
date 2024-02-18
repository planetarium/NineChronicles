namespace BalanceTool.Runtime.Util
{
    using System.Numerics;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Consensus;

    public class MockWorldState : IWorldState
    {
        private readonly IStateStore _stateStore;

        public MockWorldState()
            : this(new TrieStateStore(new MemoryKeyValueStore()), null)
        {
        }

        public MockWorldState(
            IStateStore stateStore,
            HashDigest<SHA256>? stateRootHash = null)
        {
            _stateStore = stateStore;
            Trie = stateStore.GetStateRoot(stateRootHash);
            Legacy = false;
        }

        public ITrie Trie { get; }

        public bool Legacy { get; private set; }

        public IAccountState GetAccountState(Address address) => GetMockAccountState(address);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState GetMockAccountState(Address address)
            => Legacy
                ? new MockAccountState(_stateStore, Trie.Hash)
                : new MockAccountState(
                    _stateStore,
                    Trie.Get(MockKeyConverters.ToStateKey(address)) is { } stateRootNotNull
                        ? new HashDigest<SHA256>(stateRootNotNull)
                        : null);
#pragma warning restore SA1118 // Parameter should not span multiple lines

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockWorldState SetAccountState(Address address, IAccountState accountState)
            => Legacy
            ? new MockWorldState(_stateStore, accountState.Trie.Hash)
            : new MockWorldState(
                _stateStore,
                _stateStore.Commit(
                    Trie.Set(
                        MockKeyConverters.ToStateKey(address),
                        new Binary(accountState.Trie.Hash.ByteArray))).Hash);
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockWorldState SetState(Address accountAddress, Address address, IValue state)
            => SetAccountState(accountAddress, GetMockAccountState(accountAddress).SetState(address, state));

        public MockWorldState SetBalance(
            Address address,
            FungibleAssetValue amount)
            => SetBalance((address, amount.Currency), amount.RawValue);

        public MockWorldState SetBalance(
            Address address, Currency currency, BigInteger rawAmount)
            => SetBalance((address, currency), rawAmount);

        public MockWorldState SetBalance(
            (Address Address, Currency Currency) pair,
            BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .SetBalance((pair.Address, pair.Currency), rawAmount));

        public MockWorldState AddBalance(
            Address address, FungibleAssetValue amount)
            => AddBalance((address, amount.Currency), amount.RawValue);

        public MockWorldState AddBalance(
            Address address, Currency currency, BigInteger rawAmount)
            => AddBalance((address, currency), rawAmount);

        public MockWorldState AddBalance(
            (Address Address, Currency Currency) pair,
            BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .AddBalance(pair, rawAmount));

        public MockWorldState SubtractBalance(
            Address address, FungibleAssetValue amount)
            => SubtractBalance((address, amount.Currency), amount.RawValue);

        public MockWorldState SubtractBalance(
            Address address, Currency currency, BigInteger rawAmount)
            => SubtractBalance((address, currency), rawAmount);

        public MockWorldState SubtractBalance(
            (Address Address, Currency Currency) pair,
            BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .SubtractBalance(pair, rawAmount));

        public MockWorldState TransferBalance(
            Address sender,
            Address recipient,
            FungibleAssetValue amount) =>
            TransferBalance(
                sender,
                recipient,
                amount.Currency,
                amount.RawValue);

        public MockWorldState TransferBalance(
            Address sender,
            Address recipient,
            Currency currency,
            BigInteger rawAmount)
            => SubtractBalance(sender, currency, rawAmount)
            .AddBalance(recipient, currency, rawAmount);

        public MockWorldState SetTotalSupply(FungibleAssetValue amount)
            => SetTotalSupply(amount.Currency, amount.RawValue);

        public MockWorldState SetTotalSupply(Currency currency, BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .SetTotalSupply(currency, rawAmount));

        public MockWorldState AddTotalSupply(FungibleAssetValue amount)
            => AddTotalSupply(amount.Currency, amount.RawValue);

        public MockWorldState AddTotalSupply(Currency currency, BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .AddTotalSupply(currency, rawAmount));

        public MockWorldState SubtractTotalSupply(FungibleAssetValue amount)
            => SubtractTotalSupply(amount.Currency, amount.RawValue);

        public MockWorldState SubtractTotalSupply(Currency currency, BigInteger rawAmount)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .SubtractTotalSupply(currency, rawAmount));

        public MockWorldState SetValidator(Validator validator)
            => SetAccountState(
                ReservedAddresses.LegacyAccount,
                GetMockAccountState(ReservedAddresses.LegacyAccount)
                    .SetValidator(validator));
    }
}
