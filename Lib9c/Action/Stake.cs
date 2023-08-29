using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Exceptions;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Stake;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("stake3")]
    public class Stake : GameAction, IStakeV1
    {
        internal BigInteger Amount { get; set; }

        BigInteger IStakeV1.Amount => Amount;

        public Stake(BigInteger amount)
        {
            Amount = amount >= 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public Stake()
        {
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty.Add(AmountKey, (IValue) (Integer) Amount);

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Amount = plainValue[AmountKey].ToBigInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
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

            // Validate plain values
            if (Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake exec started", addressesHex);
            var stakePolicySheet = states.GetSheet<StakePolicySheet>();
            var currentStakeRegularRewardSheetAddress =
                Addresses.GetSheetAddress(stakePolicySheet[nameof(StakeRegularRewardSheet)].TableName);

            var stakeRegularRewardSheet = states.GetSheet<StakeRegularRewardSheet>(
                currentStakeRegularRewardSheetAddress);
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

            var latestStakeContract = new Contract(
                stakeRegularFixedRewardSheetTableName: stakePolicySheet["StakeRegularFixedRewardSheet"].TableName,
                stakeRegularRewardSheetTableName: stakePolicySheet["StakeRegularRewardSheet"].TableName
            );

            if (!states.TryGetStakeStateV2(context.Signer, out var stakeStateV2))
            {
                if (Amount == 0)
                {
                    throw new StateNullException(stakeStateAddress);
                }

                stakeStateV2 = new StakeStateV2(latestStakeContract, context.BlockIndex);
                return states
                    .SetState(stakeStateAddress, stakeStateV2.Serialize())
                    .TransferAsset(context, context.Signer, stakeStateAddress, targetStakeBalance);
            }

            // NOTE: Cannot anything if staking state is claimable.
            if (stakeStateV2.ClaimableBlockIndex >= context.BlockIndex)
            {
                throw new StakeExistingClaimableException();
            }

            // NOTE: Try cancel.
            if (Amount == 0)
            {
                // NOTE: Cannot cancel until lockup ends.
                if (stakeStateV2.CancellableBlockIndex > context.BlockIndex)
                {
                    throw new RequiredBlockIndexException();
                }
                
                return states
                    .SetState(stakeStateAddress, Null.Value)
                    .TransferAsset(context, stakeStateAddress, context.Signer, stakedBalance);
            }

            // NOTE: Cannot re-contract with less balance when the staking is locked up.
            if (stakeStateV2.CancellableBlockIndex > context.BlockIndex &&
                targetStakeBalance <= stakedBalance)
            {
                throw new RequiredBlockIndexException();
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake Total Executed Time: {Elapsed}", addressesHex, ended - started);

            // Stake with more or less amount.
            return states
                .TransferAsset(context, stakeStateAddress, context.Signer, stakedBalance)
                .TransferAsset(context, context.Signer, stakeStateAddress, targetStakeBalance)
                .SetState(
                    stakeStateAddress,
                    new StakeStateV2(latestStakeContract, stakeStateV2.StartedBlockIndex)
                        .Serialize());
        }
    }
}
