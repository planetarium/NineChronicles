#nullable enable

namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Consensus;
    using Libplanet.State;

    /// <summary>
    /// <para>
    /// A rough replica of https://github.com/planetarium/libplanet/blob/main/Libplanet/State/AccountStateDelta.cs
    /// except this has its constructors exposed as public for testing with many of
    /// checks removed.
    /// </para>
    /// <para>
    /// Notable differences from the original are:
    /// <type list="bullet">
    ///     <item><description>
    ///         There is no authority check for <see cref="MintAsset"/> and <see cref="BurnAsset"/>,
    ///         i.e. any <see cref="Address"/> can mint and burn.
    ///     </description></item>
    ///     <item><description>
    ///         There is no amount check for <see cref="MintAsset"/>, <see cref="BurnAsset"/>,
    ///         i.e. 0 or even a negative amount is allowed.
    ///     </description></item>
    ///     <item><description>
    ///         The <see cref="TransferAsset"/> behaves as if any given <see cref="IActionContext"/>
    ///         has <see cref="IActionContext.BlockProtocolVersion"/> value equal to 0.
    ///     </description></item>
    /// </list>
    /// </para>
    /// </summary>
    [Pure]
    public class MockStateDelta : IAccountStateDelta
    {
        public MockStateDelta()
            : this(MockState.Empty)
        {
        }

        public MockStateDelta(IAccountState mockState)
            : this(
                mockState.GetStates,
                mockState.GetBalance,
                mockState.GetTotalSupply,
                mockState.GetValidatorSet)
        {
        }

        private MockStateDelta(
            AccountStateGetter accountStateGetter,
            AccountBalanceGetter accountBalanceGetter,
            TotalSupplyGetter totalSupplyGetter,
            ValidatorSetGetter validatorSetGetter)
        {
            Delta = new MockDelta();
            StateGetter = accountStateGetter;
            BalanceGetter = accountBalanceGetter;
            TotalSupplyGetter = totalSupplyGetter;
            ValidatorSetGetter = validatorSetGetter;
            TotalUpdatedFungibles = ImmutableDictionary<(Address, Currency), BigInteger>.Empty;
        }

        /// <inheritdoc/>
        public IAccountDelta Delta { get; private set; }

        /// <inheritdoc/>
        public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets =>
            TotalUpdatedFungibles.Keys.ToImmutableHashSet();

        public IImmutableDictionary<(Address, Currency), BigInteger> TotalUpdatedFungibles
            { get; protected set; }

        private AccountStateGetter StateGetter { get; set; }

        private AccountBalanceGetter BalanceGetter { get; set; }

        private TotalSupplyGetter TotalSupplyGetter { get; set; }

        private ValidatorSetGetter ValidatorSetGetter { get; set; }

        /// <inheritdoc/>
        [Pure]
        public IValue? GetState(Address address)
        {
            IValue? state = GetStates(new[] { address })[0];
            return state;
        }

        /// <inheritdoc cref="IAccountState.GetStates(IReadOnlyList{Address})"/>
        [Pure]
        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses)
        {
            int length = addresses.Count;
            IValue?[] values = new IValue?[length];
            var notFoundIndices = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                Address address = addresses[i];
                if (Delta.States.TryGetValue(address, out IValue? updatedValue))
                {
                    values[i] = updatedValue;
                }
                else
                {
                    notFoundIndices.Add(i);
                }
            }

            if (notFoundIndices.Count > 0)
            {
                IReadOnlyList<IValue?> restValues = StateGetter(
                    notFoundIndices.Select(index => addresses[index]).ToArray());
                foreach ((var v, var i) in notFoundIndices.Select((v, i) => (v, i)))
                {
                    values[v] = restValues[i];
                }
            }

            return values;
        }

        /// <inheritdoc/>
        [Pure]
        public IAccountStateDelta SetState(Address address, IValue state) =>
            UpdateStates(Delta.States.SetItem(address, state));

        /// <inheritdoc/>
        [Pure]
        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            GetBalance(address, currency, Delta.Fungibles);

        /// <inheritdoc/>
        [Pure]
        public FungibleAssetValue GetTotalSupply(Currency currency)
        {
            if (!currency.TotalSupplyTrackable)
            {
                throw new TotalSupplyNotTrackableException(
                    $"The total supply value of the currency {currency} is not trackable " +
                    "because it is a legacy untracked currency which might have been " +
                    "established before the introduction of total supply tracking support.",
                    currency);
            }

            // Return dirty state if it exists.
            if (Delta.TotalSupplies.TryGetValue(currency, out BigInteger totalSupplyValue))
            {
                return FungibleAssetValue.FromRawValue(currency, totalSupplyValue);
            }

            return TotalSupplyGetter(currency);
        }

        /// <inheritdoc/>
        [Pure]
        public ValidatorSet GetValidatorSet() =>
            Delta.ValidatorSet ?? ValidatorSetGetter();

        /// <inheritdoc/>
        [Pure]
        public IAccountStateDelta MintAsset(
            IActionContext context, Address recipient, FungibleAssetValue value)
        {
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The value to mint has to be greater than zero."
                );
            }

            Currency currency = value.Currency;
            if (!currency.AllowsToMint(context.Signer))
            {
                throw new CurrencyPermissionException(
                    $"The account {context.Signer} has no permission to mint currency {currency}.",
                    context.Signer,
                    currency
                );
            }

            FungibleAssetValue balance = GetBalance(recipient, currency);
            (Address, Currency) assetKey = (recipient, currency);
            BigInteger rawBalance = (balance + value).RawValue;

            if (currency.TotalSupplyTrackable)
            {
                var currentTotalSupply = GetTotalSupply(currency);
                if (currency.MaximumSupply < currentTotalSupply + value)
                {
                    var msg = $"The amount {value} attempted to be minted added to the current"
                              + $" total supply of {currentTotalSupply} exceeds the"
                              + $" maximum allowed supply of {currency.MaximumSupply}.";
                    throw new SupplyOverflowException(msg, value);
                }

                return UpdateFungibleAssets(
                    Delta.Fungibles.SetItem(assetKey, rawBalance),
                    TotalUpdatedFungibles.SetItem(assetKey, rawBalance),
                    Delta.TotalSupplies.SetItem(currency, (currentTotalSupply + value).RawValue)
                );
            }

            return UpdateFungibleAssets(
                Delta.Fungibles.SetItem(assetKey, rawBalance),
                TotalUpdatedFungibles.SetItem(assetKey, rawBalance)
            );
        }

        /// <inheritdoc/>
        [Pure]
        public IAccountStateDelta TransferAsset(
            IActionContext context,
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false) => context.BlockProtocolVersion > 0
                ? TransferAssetV1(sender, recipient, value, allowNegativeBalance)
                : TransferAssetV0(sender, recipient, value, allowNegativeBalance);

        /// <inheritdoc/>
        [Pure]
        public IAccountStateDelta BurnAsset(
            IActionContext context, Address owner, FungibleAssetValue value)
        {
            string msg;

            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The value to burn has to be greater than zero."
                );
            }

            Currency currency = value.Currency;
            if (!currency.AllowsToMint(context.Signer))
            {
                msg = $"The account {context.Signer} has no permission to burn assets of " +
                      $"the currency {currency}.";
                throw new CurrencyPermissionException(msg, context.Signer, currency);
            }

            FungibleAssetValue balance = GetBalance(owner, currency);

            if (balance < value)
            {
                msg = $"The account {owner}'s balance of {currency} is insufficient to burn: " +
                      $"{balance} < {value}.";
                throw new InsufficientBalanceException(msg, owner, balance);
            }

            (Address, Currency) assetKey = (owner, currency);
            BigInteger rawBalance = (balance - value).RawValue;
            if (currency.TotalSupplyTrackable)
            {
                return UpdateFungibleAssets(
                    Delta.Fungibles.SetItem(assetKey, rawBalance),
                    TotalUpdatedFungibles.SetItem(assetKey, rawBalance),
                    Delta.TotalSupplies.SetItem(
                        currency,
                        (GetTotalSupply(currency) - value).RawValue)
                );
            }

            return UpdateFungibleAssets(
                Delta.Fungibles.SetItem(assetKey, rawBalance),
                TotalUpdatedFungibles.SetItem(assetKey, rawBalance)
            );
        }

        /// <inheritdoc/>
        [Pure]
        public IAccountStateDelta SetValidator(Validator validator)
        {
            return UpdateValidatorSet(GetValidatorSet().Update(validator));
        }

        [Pure]
        private FungibleAssetValue GetBalance(
            Address address,
            Currency currency,
            IImmutableDictionary<(Address, Currency), BigInteger> balances) =>
            balances.TryGetValue((address, currency), out BigInteger balance)
                ? FungibleAssetValue.FromRawValue(currency, balance)
                : BalanceGetter(address, currency);

        [Pure]
        private IAccountStateDelta UpdateStates(
            IImmutableDictionary<Address, IValue> updatedStates
        ) =>
            new MockStateDelta(
                StateGetter,
                BalanceGetter,
                TotalSupplyGetter,
                ValidatorSetGetter)
            {
                Delta = new MockDelta(
                    updatedStates,
                    Delta.Fungibles,
                    Delta.TotalSupplies,
                    Delta.ValidatorSet),
                TotalUpdatedFungibles = TotalUpdatedFungibles,
            };

        [Pure]
        private IAccountStateDelta UpdateFungibleAssets(
            IImmutableDictionary<(Address, Currency), BigInteger> updatedFungibleAssets,
            IImmutableDictionary<(Address, Currency), BigInteger> totalUpdatedFungibles
        ) =>
            UpdateFungibleAssets(
                updatedFungibleAssets,
                totalUpdatedFungibles,
                Delta.TotalSupplies);

        [Pure]
        private IAccountStateDelta UpdateFungibleAssets(
            IImmutableDictionary<(Address, Currency), BigInteger> updatedFungibleAssets,
            IImmutableDictionary<(Address, Currency), BigInteger> totalUpdatedFungibles,
            IImmutableDictionary<Currency, BigInteger> updatedTotalSupply
        ) =>
            new MockStateDelta(
                StateGetter,
                BalanceGetter,
                TotalSupplyGetter,
                ValidatorSetGetter)
            {
                Delta = new MockDelta(
                    Delta.States,
                    updatedFungibleAssets,
                    updatedTotalSupply,
                    Delta.ValidatorSet),
                TotalUpdatedFungibles = totalUpdatedFungibles,
            };

        [Pure]
        private IAccountStateDelta UpdateValidatorSet(
            ValidatorSet updatedValidatorSet
        ) =>
            new MockStateDelta(
                StateGetter,
                BalanceGetter,
                TotalSupplyGetter,
                ValidatorSetGetter)
            {
                Delta = new MockDelta(
                    Delta.States,
                    Delta.Fungibles,
                    Delta.TotalSupplies,
                    updatedValidatorSet),
                TotalUpdatedFungibles = TotalUpdatedFungibles,
            };

        [Pure]
        private IAccountStateDelta TransferAssetV0(
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false)
        {
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The value to transfer has to be greater than zero."
                );
            }

            Currency currency = value.Currency;
            FungibleAssetValue senderBalance = GetBalance(sender, currency);
            FungibleAssetValue recipientBalance = GetBalance(recipient, currency);

            if (!allowNegativeBalance && senderBalance < value)
            {
                var msg = $"The account {sender}'s balance of {currency} is insufficient to " +
                          $"transfer: {senderBalance} < {value}.";
                throw new InsufficientBalanceException(msg, sender, senderBalance);
            }

            return UpdateFungibleAssets(
                Delta.Fungibles
                    .SetItem((sender, currency), (senderBalance - value).RawValue)
                    .SetItem((recipient, currency), (recipientBalance + value).RawValue),
                TotalUpdatedFungibles
                    .SetItem((sender, currency), (senderBalance - value).RawValue)
                    .SetItem((recipient, currency), (recipientBalance + value).RawValue)
            );
        }

        [Pure]
        private IAccountStateDelta TransferAssetV1(
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false)
        {
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The value to transfer has to be greater than zero."
                );
            }

            Currency currency = value.Currency;
            FungibleAssetValue senderBalance = GetBalance(sender, currency);

            if (!allowNegativeBalance && senderBalance < value)
            {
                var msg = $"The account {sender}'s balance of {currency} is insufficient to " +
                          $"transfer: {senderBalance} < {value}.";
                throw new InsufficientBalanceException(msg, sender, senderBalance);
            }

            (Address, Currency) senderAssetKey = (sender, currency);
            BigInteger senderRawBalance = (senderBalance - value).RawValue;

            IImmutableDictionary<(Address, Currency), BigInteger> updatedFungibleAssets =
                Delta.Fungibles.SetItem(senderAssetKey, senderRawBalance);
            IImmutableDictionary<(Address, Currency), BigInteger> totalUpdatedFungibles =
                TotalUpdatedFungibles.SetItem(senderAssetKey, senderRawBalance);

            FungibleAssetValue recipientBalance = GetBalance(
                recipient,
                currency,
                updatedFungibleAssets);
            (Address, Currency) recipientAssetKey = (recipient, currency);
            BigInteger recipientRawBalance = (recipientBalance + value).RawValue;

            return UpdateFungibleAssets(
                updatedFungibleAssets.SetItem(recipientAssetKey, recipientRawBalance),
                totalUpdatedFungibles.SetItem(recipientAssetKey, recipientRawBalance)
            );
        }
    }
}
