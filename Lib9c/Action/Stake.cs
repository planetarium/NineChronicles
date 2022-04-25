using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public class Stake : ActionBase
    {
        private BigInteger Amount { get; set; }

        public Stake(BigInteger amount)
        {
            Amount = amount >= 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public override IValue PlainValue { get; }
        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary) plainValue;
            Amount = dictionary[AmountKey].ToBigInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;

            if (context.Rehearsal)
            {
                return states.SetState(StakeState.DeriveAddress(context.Signer), MarkChanged)
                    .MarkBalanceChanged(
                        GoldCurrencyMock,
                        context.Signer,
                        StakeState.DeriveAddress(context.Signer));
            }

            if (Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var stakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var currency = states.GetGoldCurrency();
            var currentBalance = states.GetBalance(context.Signer, currency);
            var stakedBalance = states.GetBalance(stakeStateAddress, currency);
            var targetStakeBalance = currency * Amount;
            if (currentBalance + stakedBalance < targetStakeBalance)
            {
                throw new NotEnoughFungibleAssetValueException(
                    context.Signer.ToHex(),
                    Amount,
                    currentBalance.RawValue);
            }

            // Stake if it doesn't exist yet.
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                return states
                    .SetState(
                        stakeStateAddress,
                        new StakeState(stakeStateAddress, context.BlockIndex).SerializeV2())
                    .TransferAsset(context.Signer, stakeStateAddress, targetStakeBalance);
            }

            if (!stakeState.IsCancellable(context.BlockIndex))
            {
                throw new RequiredBlockIndexException();
            }

            // Cancel
            if (Amount == 0)
            {
                if (stakeState.IsCancellable(context.BlockIndex))
                {
                    return states.SetState(stakeState.address, Null.Value)
                        .TransferAsset(stakeState.address, context.Signer, stakedBalance);
                }
            }

            // Stake with more or less amount.
            return states.TransferAsset(stakeState.address, context.Signer, stakedBalance)
                .TransferAsset(context.Signer, stakeState.address, targetStakeBalance)
                .SetState(
                    stakeState.address,
                    new StakeState(stakeState.address, context.BlockIndex).SerializeV2());
        }
    }
}
