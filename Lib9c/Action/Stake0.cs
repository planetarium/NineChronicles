using System;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("stake")]
    [ActionObsolete(ObsoleteIndex)]
    public class Stake0 : ActionBase, IStakeV1
    {
        public const long ObsoleteIndex = ActionObsoleteConfig.V200030ObsoleteIndex;

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

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "stake")
            .Add("values", Dictionary.Empty.Add(AmountKey, Amount));

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)((Dictionary)plainValue)["values"];
            Amount = dictionary[AmountKey].ToBigInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            CheckObsolete(ActionObsoleteConfig.V200030ObsoleteIndex, context);
            IAccountStateDelta states = context.PreviousState;

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
                        context,
                        GoldCurrencyMock,
                        context.Signer,
                        StakeState.DeriveAddress(context.Signer));
            }

            CheckObsolete(ObsoleteIndex, context);

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
                    .TransferAsset(context, context.Signer, stakeStateAddress, targetStakeBalance);
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
                    return states
                        .SetState(stakeState.address, Null.Value)
                        .TransferAsset(context, stakeState.address, context.Signer, stakedBalance);
                }
            }

            // Stake with more or less amount.
            return states
                .TransferAsset(context, stakeState.address, context.Signer, stakedBalance)
                .TransferAsset(context, context.Signer, stakeState.address, targetStakeBalance)
                .SetState(
                    stakeState.address,
                    new StakeState(stakeState.address, context.BlockIndex).SerializeV2());
        }
    }
}
