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
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/500
    /// Updated at https://github.com/planetarium/lib9c/pull/958
    /// </summary>
    [Serializable]
    [ActionType("monster_collect3")]
    [ActionObsolete(ActionObsoleteConfig.V100220ObsoleteIndex)]
    public class MonsterCollect : GameAction, IMonsterCollectV2
    {
        public int level;

        int IMonsterCollectV2.Level => level;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 0), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 1), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 2), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 3), MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, MonsterCollectionState.DeriveAddress(context.Signer, 0))
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, MonsterCollectionState.DeriveAddress(context.Signer, 1))
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, MonsterCollectionState.DeriveAddress(context.Signer, 2))
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, MonsterCollectionState.DeriveAddress(context.Signer, 3));
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MonsterCollect exec started", addressesHex);
            if (states.TryGetStakeState(context.Signer, out StakeState _))
            {
                throw new InvalidOperationException(
                    "The user has staked. You cannot stake and monster-collect at the same time.");
            }

            MonsterCollectionSheet monsterCollectionSheet = states.GetSheet<MonsterCollectionSheet>();

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            if (level < 0 || level > 0 && !monsterCollectionSheet.TryGetValue(level, out MonsterCollectionSheet.Row _))
            {
                throw new MonsterCollectionLevelException();
            }

            Currency currency = states.GetGoldCurrency();
            // Set default gold value.
            FungibleAssetValue requiredGold = currency * 0;
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                context.Signer,
                agentState.MonsterCollectionRound
            );
            if (states.TryGetState(monsterCollectionAddress, out Dictionary stateDict))
            {
                var existingStates = new MonsterCollectionState(stateDict);
                int previousLevel = existingStates.Level;
                // Check collection level and required block index
                if (level < previousLevel && existingStates.IsLocked(context.BlockIndex))
                {
                    throw new RequiredBlockIndexException();
                }

                if (level == previousLevel)
                {
                    throw new MonsterCollectionLevelException();
                }

                if (existingStates.CalculateStep(context.BlockIndex) > 0)
                {
                    throw new MonsterCollectionExistingClaimableException();
                }

                // Refund holding NCG to user
                FungibleAssetValue gold = states.GetBalance(monsterCollectionAddress, currency);
                states = states.TransferAsset(monsterCollectionAddress, context.Signer, gold);
            }

            if (level == 0)
            {
                return states.SetState(monsterCollectionAddress, new Null());
            }

            FungibleAssetValue balance = states.GetBalance(context.Signer, currency);
            var monsterCollectionState = new MonsterCollectionState(monsterCollectionAddress, level, context.BlockIndex);
            for (int i = 0; i < level; i++)
            {
                requiredGold += currency * monsterCollectionSheet[i + 1].RequiredGold;
            }

            if (balance < requiredGold)
            {
                throw new InsufficientBalanceException(
                    $"There is no sufficient balance for {context.Signer}: {balance} < {requiredGold}",
                    context.Signer,
                    requiredGold
                    );
            }
            states = states.TransferAsset(context.Signer, monsterCollectionAddress, requiredGold);
            states = states.SetState(monsterCollectionAddress, monsterCollectionState.Serialize());
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MonsterCollect Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [LevelKey] = level.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            level = plainValue[LevelKey].ToInteger();
        }
    }
}
