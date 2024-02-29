namespace BalanceTool.Runtime.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    /// <summary>
    /// A mock implementation of <see cref="IAccountState"/> with various overloaded methods for
    /// improving QoL.
    /// </summary>
    /// <remarks>
    /// All methods are pretty self-explanatory with no side-effects.  There are some caveats:
    /// <list type="bullet">
    ///     <item><description>
    ///         Every balance related operation can accept a negative amount.  Each behave as expected.
    ///         That is, adding negative amount would decrease the balance.
    ///     </description></item>
    ///     <item><description>
    ///         Negative balance is allowed for all cases.  This includes total supply.
    ///     </description></item>
    ///     <item><description>
    ///         Total supply is not automatically tracked.  That is, changing the balance associated
    ///         with an <see cref="Address"/> does not change the total supply in any way.
    ///         Total supply must be explicitly set if needed.
    ///     </description></item>
    ///     <item><description>
    ///         There are only few restrictions that apply for manipulating this object, mostly
    ///         pertaining to total supplies:
    ///         <list type="bullet">
    ///             <item><description>
    ///                 It is not possible to set a total supply amount for a currency that is
    ///                 not trackable.
    ///             </description></item>
    ///             <item><description>
    ///                 It is not possible to set a total supply amount that is over the currency's
    ///                 capped maximum total supply.
    ///             </description></item>
    ///         </list>
    ///     </description></item>
    /// </list>
    /// </remarks>
    public class MockAccountState : IAccountState
    {
        private readonly IStateStore _stateStore;

        public MockAccountState()
            : this(new TrieStateStore(new MemoryKeyValueStore()), null)
        {
        }

        public MockAccountState(
            IStateStore stateStore,
            HashDigest<SHA256>? stateRootHash = null)
        {
            _stateStore = stateStore;
            Trie = stateStore.GetStateRoot(stateRootHash);
        }

        public ITrie Trie { get; }

        public IValue? GetState(Address address) =>
            Trie.Get(MockKeyConverters.ToStateKey(address));

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToList();

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            Trie.Get(MockKeyConverters.ToFungibleAssetKey(address, currency)) is Integer rawValue
                ? FungibleAssetValue.FromRawValue(currency, rawValue)
                : currency * 0;

        public FungibleAssetValue GetTotalSupply(Currency currency)
        {
            if (!currency.TotalSupplyTrackable)
            {
                var msg =
                    $"The total supply value of the currency {currency} is not trackable " +
                    "because it is a legacy untracked currency which might have been" +
                    "established before the introduction of total supply tracking support.";
                throw new TotalSupplyNotTrackableException(msg, currency);
            }

            return Trie.Get(MockKeyConverters.ToTotalSupplyKey(currency)) is Integer rawValue
                ? FungibleAssetValue.FromRawValue(currency, rawValue)
                : currency * 0;
        }

        public ValidatorSet GetValidatorSet() =>
             Trie.Get(MockKeyConverters.ValidatorSetKey) is List list
                ? new ValidatorSet(list)
                : new ValidatorSet();

        public MockAccountState SetState(Address address, IValue state) =>
            new MockAccountState(
                _stateStore,
                _stateStore.Commit(Trie.Set(MockKeyConverters.ToStateKey(address), state)).Hash);

        public MockAccountState SetBalance(
            Address address, FungibleAssetValue amount) =>
            SetBalance((address, amount.Currency), amount.RawValue);

        public MockAccountState SetBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            SetBalance((address, currency), rawAmount);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState SetBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            new MockAccountState(
                _stateStore,
                _stateStore.Commit(
                    Trie.Set(
                        MockKeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency),
                        new Integer(rawAmount))).Hash);
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockAccountState AddBalance(Address address, FungibleAssetValue amount) =>
            AddBalance((address, amount.Currency), amount.RawValue);

        public MockAccountState AddBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            AddBalance((address, currency), rawAmount);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState AddBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            SetBalance(
                pair,
                (Trie.Get(MockKeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency)) is
                    Integer amount ? amount : 0) + rawAmount);
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockAccountState SubtractBalance(
            Address address, FungibleAssetValue amount) =>
            SubtractBalance((address, amount.Currency), amount.RawValue);

        public MockAccountState SubtractBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            SubtractBalance((address, currency), rawAmount);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState SubtractBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            SetBalance(
                pair,
                (Trie.Get(MockKeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency)) is
                    Integer amount ? amount : 0) - rawAmount);
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockAccountState TransferBalance(
            Address sender, Address recipient, FungibleAssetValue amount) =>
            TransferBalance(sender, recipient, amount.Currency, amount.RawValue);

        public MockAccountState TransferBalance(
            Address sender, Address recipient, Currency currency, BigInteger rawAmount) =>
            SubtractBalance(sender, currency, rawAmount).AddBalance(recipient, currency, rawAmount);

        public MockAccountState SetTotalSupply(FungibleAssetValue amount) =>
            SetTotalSupply(amount.Currency, amount.RawValue);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState SetTotalSupply(Currency currency, BigInteger rawAmount) =>
            currency.TotalSupplyTrackable
                ? !(currency.MaximumSupply is FungibleAssetValue maximumSupply) ||
                    rawAmount <= maximumSupply.RawValue
                    ? new MockAccountState(
                        _stateStore,
                        _stateStore.Commit(
                            Trie.Set(MockKeyConverters.ToTotalSupplyKey(currency), new Integer(rawAmount))).Hash)
                    : throw new ArgumentException(
                        $"Given {currency}'s total supply is capped at {maximumSupply.RawValue} " +
                        $"and cannot be set to {rawAmount}.")
                : throw new ArgumentException(
                    $"Given {currency} is not trackable.");
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockAccountState AddTotalSupply(FungibleAssetValue amount) =>
            AddTotalSupply(amount.Currency, amount.RawValue);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState AddTotalSupply(Currency currency, BigInteger rawAmount) =>
            SetTotalSupply(
                currency,
                (Trie.Get(MockKeyConverters.ToTotalSupplyKey(currency)) is
                    Integer amount ? amount : 0) + rawAmount);
#pragma warning restore SA1118 // Parameter should not span multiple lines

        public MockAccountState SubtractTotalSupply(FungibleAssetValue amount) =>
            SubtractTotalSupply(amount.Currency, amount.RawValue);

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState SubtractTotalSupply(Currency currency, BigInteger rawAmount) =>
            SetTotalSupply(
                currency,
                (Trie.Get(MockKeyConverters.ToTotalSupplyKey(currency)) is
                    Integer amount ? amount : 0) - rawAmount);
#pragma warning restore SA1118 // Parameter should not span multiple lines

#pragma warning disable SA1118 // Parameter should not span multiple lines
        public MockAccountState SetValidator(Validator validator) =>
            new MockAccountState(
                _stateStore,
                _stateStore.Commit(
                    Trie.Set(MockKeyConverters.ValidatorSetKey, GetValidatorSet().Update(validator).Bencoded)).Hash);
#pragma warning restore SA1118 // Parameter should not span multiple lines

    }
}
