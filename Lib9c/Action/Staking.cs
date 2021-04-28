using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("staking")]
    public class Staking : GameAction
    {
        public int level;
        public int stakingRound;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address stakingAddress = StakingState.DeriveAddress(context.Signer, stakingRound);
            if (context.Rehearsal)
            {
                return states
                    .SetState(stakingAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, stakingAddress);
            }

            StakingSheet stakingSheet = states.GetSheet<StakingSheet>();

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state is failed to load.");
            }

            if (agentState.StakingRound != stakingRound)
            {
                throw new InvalidStakingRoundException(
                    $"Expected staking round is {agentState.StakingRound}, but actual staking round is {stakingRound}.");
            }

            if (!stakingSheet.TryGetValue(level, out StakingSheet.Row _))
            {
                throw new SheetRowNotFoundException(nameof(StakingSheet), level);
            }

            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue requiredGold = currency * 0;
            FungibleAssetValue balance = states.GetBalance(context.Signer, states.GetGoldCurrency());

            StakingState stakingState;
            int currentLevel = 1;
            StakingRewardSheet stakingRewardSheet = states.GetSheet<StakingRewardSheet>();
            if (states.TryGetState(stakingAddress, out Dictionary stateDict))
            {
                stakingState = new StakingState(stateDict);

                if (stakingState.ExpiredBlockIndex < context.BlockIndex)
                {
                    throw new StakingExpiredException(
                        $"{stakingAddress} is already expired on {stakingState.ExpiredBlockIndex}");
                }

                if (stakingState.Level >= level)
                {
                    throw new InvalidLevelException($"The level must be greater than {stakingState.Level}.");
                }

                currentLevel = stakingState.Level + 1;
                long rewardLevel = stakingState.GetRewardLevel(context.BlockIndex);
                stakingState.Update(level, rewardLevel, stakingRewardSheet);
            }
            else
            {
                stakingState = new StakingState(stakingAddress, level, context.BlockIndex, stakingRewardSheet);
            }

            for (int i = currentLevel; i < level + 1; i++)
            {
                requiredGold += currency * stakingSheet[i].RequiredGold;
            }

            if (balance < requiredGold)
            {
                throw new InsufficientBalanceException(context.Signer, requiredGold,
                    $"There is no sufficient balance for {context.Signer}: {balance} < {requiredGold}");
            }
            states = states.TransferAsset(context.Signer, stakingAddress, requiredGold);
            states = states.SetState(stakingAddress, stakingState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [LevelKey] = level.Serialize(),
            [StakingRoundKey] = stakingRound.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            level = plainValue[LevelKey].ToInteger();
            stakingRound = plainValue[StakingRoundKey].ToInteger();
        }
    }
}
