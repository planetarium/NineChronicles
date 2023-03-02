using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("stake")]
    public class Stake0 : ActionBase, IStakeV1
    {
        internal BigInteger Amount { get; set; }

        BigInteger IStakeV1.Amount => Amount;

        public Stake0(BigInteger amount)
        {
            Amount = amount >= 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public Stake0()
        {
        }

        public override IValue PlainValue =>
            Dictionary.Empty.Add(AmountKey, (IValue) (Integer) Amount);

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary) plainValue;
            Amount = dictionary[AmountKey].ToBigInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;

            // Restrict staking if there is a monster collection until now.
            if (states.GetAgentState(context.Signer) is { } agentState &&
                states.TryGetState(MonsterCollectionState.DeriveAddress(
                    context.Signer,
                    agentState.MonsterCollectionRound), out Dictionary _))
            {
                throw new MonsterCollectionExistingException();
            }

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

            var stakeRegularRewardSheet = states.GetSheet<StakeRegularRewardSheet>();
            var minimumRequiredGold = stakeRegularRewardSheet.OrderedRows.Min(x => x.RequiredGold);
            if (Amount != 0 && Amount < minimumRequiredGold)
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
                    currentBalance);
            }

            // Stake if it doesn't exist yet.
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                var stakeAchievementRewardSheet = states.GetSheet<StakeAchievementRewardSheet>();

                stakeState = new StakeState(stakeStateAddress, context.BlockIndex);
                return states
                    .SetState(
                        stakeStateAddress,
                        stakeState.SerializeV2())
                    .TransferAsset(context.Signer, stakeStateAddress, targetStakeBalance);
            }

            if (stakeState.IsClaimable(context.BlockIndex))
            {
                throw new StakeExistingClaimableException();
            }

            if (!stakeState.IsCancellable(context.BlockIndex) &&
                (context.BlockIndex >= 4611070
                    ? targetStakeBalance <= stakedBalance
                    : targetStakeBalance < stakedBalance))
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
