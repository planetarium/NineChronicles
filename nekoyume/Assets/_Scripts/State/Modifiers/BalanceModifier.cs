using System;
using Libplanet.Types.Assets;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class BalanceModifier : IAccumulatableValueModifier<FungibleAssetValue>
    {
        [FormerlySerializedAs("value")]
        [SerializeField]
        private FungibleAssetValue balance;

        public bool IsEmpty => balance.Sign == 0;

        public BalanceModifier(FungibleAssetValue value)
        {
            balance = value;
        }

        public FungibleAssetValue Modify(FungibleAssetValue value)
        {
            if (!value.Currency.Equals(balance.Currency))
            {
                Debug.Log($"{value.Currency} != {balance.Currency}");
                return value;
            }

            return value + balance;
        }

        public void Add(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not BalanceModifier m)
            {
                return;
            }

            balance += m.balance;
        }

        public void Remove(IAccumulatableValueModifier<FungibleAssetValue> modifier)
        {
            if (modifier is not BalanceModifier m)
            {
                return;
            }

            balance -= m.balance;
        }

        public override string ToString() => balance.ToString();
    }
}
