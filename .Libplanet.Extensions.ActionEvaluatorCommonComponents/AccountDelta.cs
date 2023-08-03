using System.Collections.Immutable;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Libplanet.Action.State;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents
{
    public class AccountDelta : IAccountDelta
    {
        public AccountDelta()
        {
            States = ImmutableDictionary<Address, IValue>.Empty;
            Fungibles = ImmutableDictionary<(Address, Currency), BigInteger>.Empty;
            TotalSupplies = ImmutableDictionary<Currency, BigInteger>.Empty;
            ValidatorSet = null;
        }

        public AccountDelta(
            IImmutableDictionary<Address, IValue> statesDelta,
            IImmutableDictionary<(Address, Currency), BigInteger> fungiblesDelta,
            IImmutableDictionary<Currency, BigInteger> totalSuppliesDelta,
            ValidatorSet? validatorSetDelta)
        {
            States = statesDelta;
            Fungibles = fungiblesDelta;
            TotalSupplies = totalSuppliesDelta;
            ValidatorSet = validatorSetDelta;
        }

        /// <inheritdoc cref="IAccountDelta.UpdatedAddresses"/>
        public IImmutableSet<Address> UpdatedAddresses =>
            StateUpdatedAddresses.Union(FungibleUpdatedAddresses);

        /// <inheritdoc cref="IAccountDelta.StateUpdatedAddresses"/>
        public IImmutableSet<Address> StateUpdatedAddresses =>
            States.Keys.ToImmutableHashSet();

        /// <inheritdoc cref="IAccountDelta.States"/>
        public IImmutableDictionary<Address, IValue> States { get; }

        /// <inheritdoc cref="IAccountDelta.FungibleUpdatedAddresses"/>
        public IImmutableSet<Address> FungibleUpdatedAddresses =>
            Fungibles.Keys.Select(pair => pair.Item1).ToImmutableHashSet();

        /// <inheritdoc cref="IAccountDelta.UpdatedFungibleAssets"/>
        public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets =>
            Fungibles.Keys.ToImmutableHashSet();

        /// <inheritdoc cref="IAccountDelta.Fungibles"/>
        public IImmutableDictionary<(Address, Currency), BigInteger> Fungibles { get; }

        /// <inheritdoc cref="IAccountDelta.UpdatedTotalSupplyCurrencies"/>
        public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies =>
            TotalSupplies.Keys.ToImmutableHashSet();

        /// <inheritdoc cref="IAccountDelta.TotalSupplies"/>
        public IImmutableDictionary<Currency, BigInteger> TotalSupplies { get; }

        /// <inheritdoc cref="IAccountDelta.ValidatorSet"/>
        public ValidatorSet? ValidatorSet { get; }
    }
}
