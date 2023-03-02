using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("monster_collect")]
    public class MonsterCollect0 : GameAction, IMonsterCollectV1
    {
        public int level;
        public int collectionRound;

        int IMonsterCollectV1.Level => level;
        int IMonsterCollectV1.CollectionRound => collectionRound;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address monsterCollectionAddress = MonsterCollectionState0.DeriveAddress(context.Signer, collectionRound);
            if (context.Rehearsal)
            {
                return states
                    .SetState(monsterCollectionAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, monsterCollectionAddress);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            MonsterCollectionSheet monsterCollectionSheet = states.GetSheet<MonsterCollectionSheet>();

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            if (agentState.MonsterCollectionRound != collectionRound)
            {
                throw new InvalidMonsterCollectionRoundException(
                    $"Expected collection round is {agentState.MonsterCollectionRound}, but actual collection round is {collectionRound}.");
            }

            if (!monsterCollectionSheet.TryGetValue(level, out MonsterCollectionSheet.Row _))
            {
                throw new SheetRowNotFoundException(nameof(MonsterCollectionSheet), level);
            }

            Currency currency = states.GetGoldCurrency();
            // Set default gold value.
            FungibleAssetValue requiredGold = currency * 0;
            FungibleAssetValue balance = states.GetBalance(context.Signer, states.GetGoldCurrency());

            MonsterCollectionState0 monsterCollectionState;
            int currentLevel = 1;
            MonsterCollectionRewardSheet monsterCollectionRewardSheet = states.GetSheet<MonsterCollectionRewardSheet>();
            if (states.TryGetState(monsterCollectionAddress, out Dictionary stateDict))
            {
                monsterCollectionState = new MonsterCollectionState0(stateDict);

                if (monsterCollectionState.ExpiredBlockIndex < context.BlockIndex)
                {
                    throw new MonsterCollectionExpiredException(
                        $"{monsterCollectionAddress} has already expired on {monsterCollectionState.ExpiredBlockIndex}");
                }

                if (monsterCollectionState.Level >= level)
                {
                    throw new InvalidLevelException($"The level must be greater than {monsterCollectionState.Level}.");
                }

                currentLevel = monsterCollectionState.Level + 1;
                long rewardLevel = monsterCollectionState.GetRewardLevel(context.BlockIndex);
                monsterCollectionState.Update(level, rewardLevel, monsterCollectionRewardSheet);
            }
            else
            {
                monsterCollectionState = new MonsterCollectionState0(monsterCollectionAddress, level, context.BlockIndex, monsterCollectionRewardSheet);
            }

            for (int i = currentLevel; i < level + 1; i++)
            {
                requiredGold += currency * monsterCollectionSheet[i].RequiredGold;
            }

            if (balance < requiredGold)
            {
                throw new InsufficientBalanceException(
                    $"There is no sufficient balance for {context.Signer}: {balance} < {requiredGold}",
                    context.Signer,
                    requiredGold);
            }
            states = states.TransferAsset(context.Signer, monsterCollectionAddress, requiredGold);
            states = states.SetState(monsterCollectionAddress, monsterCollectionState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [LevelKey] = level.Serialize(),
            [MonsterCollectionRoundKey] = collectionRound.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            level = plainValue[LevelKey].ToInteger();
            collectionRound = plainValue[MonsterCollectionRoundKey].ToInteger();
        }
    }
}
