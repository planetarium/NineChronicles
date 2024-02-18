#nullable enable

// This file is copied from Assets/_Scripts/Lib9c/lib9c/.Lib9c.Tests/Action/MockState.cs
namespace BalanceTool.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet.Action.State;
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
    public class MockState : IAccountState
    {
        private static readonly MockState _empty = new MockState();

        private MockState()
            : this(new TrieStateStore(new MemoryKeyValueStore()).GetStateRoot(null))
        {
        }

        private MockState(ITrie trie)
        {
            Trie = trie;
        }

        public static MockState Empty => _empty;

        public ITrie Trie { get; }

        public IValue? GetState(Address address) => Trie.Get(KeyConverters.ToStateKey(address));

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(GetState).ToList();

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            Trie.Get(KeyConverters.ToFungibleAssetKey(address, currency)) is Integer rawValue
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

            return Trie.Get(KeyConverters.ToTotalSupplyKey(currency)) is Integer rawValue
                ? FungibleAssetValue.FromRawValue(currency, rawValue)
                : currency * 0;
        }

        public ValidatorSet GetValidatorSet() =>
             Trie.Get(KeyConverters.ValidatorSetKey) is List list
                ? new ValidatorSet(list)
                : new ValidatorSet();

        public MockState SetState(Address address, IValue state) =>
            new MockState(Trie.Set(KeyConverters.ToStateKey(address), state));

        public MockState SetBalance(
            Address address, FungibleAssetValue amount) =>
            SetBalance((address, amount.Currency), amount.RawValue);

        public MockState SetBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            SetBalance((address, currency), rawAmount);

        public MockState SetBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            new MockState(Trie.Set(
                KeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency),
                new Integer(rawAmount)));

        public MockState AddBalance(Address address, FungibleAssetValue amount) =>
            AddBalance((address, amount.Currency), amount.RawValue);

        public MockState AddBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            AddBalance((address, currency), rawAmount);

        public MockState AddBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            SetBalance(
                pair,
                (Trie.Get(KeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency)) is Integer amount ? amount : 0) + rawAmount);

        public MockState SubtractBalance(
            Address address, FungibleAssetValue amount) =>
            SubtractBalance((address, amount.Currency), amount.RawValue);

        public MockState SubtractBalance(
            Address address, Currency currency, BigInteger rawAmount) =>
            SubtractBalance((address, currency), rawAmount);

        public MockState SubtractBalance(
            (Address Address, Currency Currency) pair, BigInteger rawAmount) =>
            SetBalance(
                pair,
                (Trie.Get(KeyConverters.ToFungibleAssetKey(pair.Address, pair.Currency)) is Integer amount ? amount : 0) - rawAmount);

        public MockState TransferBalance(
            Address sender, Address recipient, FungibleAssetValue amount) =>
            TransferBalance(sender, recipient, amount.Currency, amount.RawValue);

        public MockState TransferBalance(
            Address sender, Address recipient, Currency currency, BigInteger rawAmount) =>
            SubtractBalance(sender, currency, rawAmount).AddBalance(recipient, currency, rawAmount);

        public MockState SetTotalSupply(FungibleAssetValue amount) =>
            SetTotalSupply(amount.Currency, amount.RawValue);

        public MockState SetTotalSupply(Currency currency, BigInteger rawAmount) =>
            currency.TotalSupplyTrackable
                ? !(currency.MaximumSupply is FungibleAssetValue maximumSupply) ||
                    rawAmount <= maximumSupply.RawValue
                    ? new MockState(
                        Trie.Set(KeyConverters.ToTotalSupplyKey(currency), new Integer(rawAmount)))
                    : throw new ArgumentException(
                        $"Given {currency}'s total supply is capped at {maximumSupply.RawValue} " +
                        $"and cannot be set to {rawAmount}.")
                : throw new ArgumentException(
                    $"Given {currency} is not trackable.");

        public MockState AddTotalSupply(FungibleAssetValue amount) =>
            AddTotalSupply(amount.Currency, amount.RawValue);

        public MockState AddTotalSupply(Currency currency, BigInteger rawAmount) =>
            SetTotalSupply(
                currency,
                (Trie.Get(KeyConverters.ToTotalSupplyKey(currency)) is Integer amount ? amount : 0) + rawAmount);

        public MockState SubtractTotalSupply(FungibleAssetValue amount) =>
            SubtractTotalSupply(amount.Currency, amount.RawValue);

        public MockState SubtractTotalSupply(Currency currency, BigInteger rawAmount) =>
            SetTotalSupply(
                currency,
                (Trie.Get(KeyConverters.ToTotalSupplyKey(currency)) is Integer amount ? amount : 0) - rawAmount);

        public MockState SetValidator(Validator validator) =>
            new MockState(
                Trie.Set(KeyConverters.ValidatorSetKey, GetValidatorSet().Update(validator).Bencoded));

        private static class KeyConverters
        {
            // "___"
            internal static readonly KeyBytes ValidatorSetKey =
                new KeyBytes(new byte[] { _underScore, _underScore, _underScore });

            private const byte _underScore = 95;  // '_'

            private static readonly byte[] _conversionTable =
            {
                48,  // '0'
                49,  // '1'
                50,  // '2'
                51,  // '3'
                52,  // '4'
                53,  // '5'
                54,  // '6'
                55,  // '7'
                56,  // '8'
                57,  // '9'
                97,  // 'a'
                98,  // 'b'
                99,  // 'c'
                100, // 'd'
                101, // 'e'
                102, // 'f'
            };

            // $"{ByteUtil.Hex(address.ByteArray)}"
            internal static KeyBytes ToStateKey(Address address)
            {
                var addressBytes = address.ByteArray;
                byte[] buffer = new byte[addressBytes.Length * 2];
                for (int i = 0; i < addressBytes.Length; i++)
                {
                    buffer[i * 2] = _conversionTable[addressBytes[i] >> 4];
                    buffer[i * 2 + 1] = _conversionTable[addressBytes[i] & 0xf];
                }

                return new KeyBytes(buffer);
            }

            // $"_{ByteUtil.Hex(address.ByteArray)}_{ByteUtil.Hex(currency.Hash.ByteArray)}"
            internal static KeyBytes ToFungibleAssetKey(Address address, Currency currency)
            {
                var addressBytes = address.ByteArray;
                var currencyBytes = currency.Hash.ByteArray;
                byte[] buffer = new byte[addressBytes.Length * 2 + currencyBytes.Length * 2 + 2];

                buffer[0] = _underScore;
                for (int i = 0; i < addressBytes.Length; i++)
                {
                    buffer[1 + i * 2] = _conversionTable[addressBytes[i] >> 4];
                    buffer[1 + i * 2 + 1] = _conversionTable[addressBytes[i] & 0xf];
                }

                var offset = addressBytes.Length * 2;
                buffer[offset + 1] = _underScore;
                for (int i = 0; i < currencyBytes.Length; i++)
                {
                    buffer[offset + 2 + i * 2] = _conversionTable[currencyBytes[i] >> 4];
                    buffer[offset + 2 + i * 2 + 1] = _conversionTable[currencyBytes[i] & 0xf];
                }

                return new KeyBytes(buffer);
            }

            internal static KeyBytes ToFungibleAssetKey(
                (Address Address, Currency Currency) pair) =>
                ToFungibleAssetKey(pair.Address, pair.Currency);

            // $"__{ByteUtil.Hex(currency.Hash.ByteArray)}"
            internal static KeyBytes ToTotalSupplyKey(Currency currency)
            {
                var currencyBytes = currency.Hash.ByteArray;
                byte[] buffer = new byte[currencyBytes.Length * 2 + 2];

                buffer[0] = _underScore;
                buffer[1] = _underScore;

                for (int i = 0; i < currencyBytes.Length; i++)
                {
                    buffer[2 + i * 2] = _conversionTable[currencyBytes[i] >> 4];
                    buffer[2 + i * 2 + 1] = _conversionTable[currencyBytes[i] & 0xf];
                }

                return new KeyBytes(buffer);
            }
        }
    }
}
