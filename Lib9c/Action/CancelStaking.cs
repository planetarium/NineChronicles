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
    [ActionType("cancel_staking")]
    public class CancelStaking : GameAction
    {
        public int stakingRound;
        public int level;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address stakingAddress = StakingState.DeriveAddress(context.Signer, stakingRound);
            if (context.Rehearsal)
            {
                return states
                    .SetState(stakingAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, stakingAddress, context.Signer);
            }

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state is failed to load.");
            }

            if (!states.TryGetState(stakingAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the staking state is failed to load.");
            }

            StakingState stakingState = new StakingState(stateDict);
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue balance = 0 * currency;
            StakingSheet stakingSheet = states.GetSheet<StakingSheet>();
            int currentLevel = stakingState.Level;
            if (currentLevel <= level)
            {
                throw new InvalidLevelException($"The level must be less than {currentLevel}.");
            }

            if (stakingState.End)
            {
                throw new StakingExpiredException($"{stakingAddress} is already expired on {stakingState.ExpiredBlockIndex}");
            }

            stakingState.Update(level);
            for (int i = currentLevel; i > level; i--)
            {
                balance += stakingSheet[i].RequiredGold * currency;
            }

            return states
                .SetState(stakingAddress, stakingState.Serialize())
                .TransferAsset(stakingAddress, context.Signer, balance);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [StakingRoundKey] = stakingRound.Serialize(),
                [LevelKey] = level.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            stakingRound = plainValue[StakingRoundKey].ToInteger();
            level = plainValue[LevelKey].ToInteger();
        }
    }
}
