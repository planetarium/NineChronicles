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
    [ActionObsolete(2100000)]
    [ActionType("cancel_monster_collect")]
    public class CancelMonsterCollect : GameAction
    {
        public int collectRound;
        public int level;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(context.Signer, collectRound);
            if (context.Rehearsal)
            {
                return states
                    .SetState(collectionAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, collectionAddress, context.Signer);
            }

            CheckObsolete(2100000, context);

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(stateDict);
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue balance = 0 * currency;
            MonsterCollectionSheet monsterCollectionSheet = states.GetSheet<MonsterCollectionSheet>();
            int currentLevel = monsterCollectionState.Level;
            if (currentLevel <= level || level <= 0)
            {
                throw new InvalidLevelException($"The level must be greater than 0 and less than {currentLevel}.");
            }

            if (monsterCollectionState.End)
            {
                throw new MonsterCollectionExpiredException($"{collectionAddress} has already expired on {monsterCollectionState.ExpiredBlockIndex}");
            }

            long rewardLevel = monsterCollectionState.GetRewardLevel(context.BlockIndex);
            MonsterCollectionRewardSheet monsterCollectionRewardSheet = states.GetSheet<MonsterCollectionRewardSheet>();
            monsterCollectionState.Update(level, rewardLevel, monsterCollectionRewardSheet);
            for (int i = currentLevel; i > level; i--)
            {
                balance += monsterCollectionSheet[i].RequiredGold * currency;
            }

            return states
                .SetState(collectionAddress, monsterCollectionState.Serialize())
                .TransferAsset(collectionAddress, context.Signer, balance);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [MonsterCollectionRoundKey] = collectRound.Serialize(),
                [LevelKey] = level.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            collectRound = plainValue[MonsterCollectionRoundKey].ToInteger();
            level = plainValue[LevelKey].ToInteger();
        }
    }
}
