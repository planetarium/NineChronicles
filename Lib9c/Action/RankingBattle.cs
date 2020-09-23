using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("ranking_battle")]
    public class RankingBattle : GameAction
    {
        public const int StageId = 999999;
        public static readonly BigInteger EntranceFee = 100;

        public Address AvatarAddress;
        public Address EnemyAddress;
        public Address WeeklyArenaAddress;
        public List<int> costumeIds;
        public List<Guid> equipmentIds;
        public List<Guid> consumableIds;
        public BattleLog Result { get; private set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(WeeklyArenaAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, WeeklyArenaAddress);
            }

            if (AvatarAddress.Equals(EnemyAddress))
            {
                throw new InvalidAddressException("Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAgentAvatarStates(
                ctx.Signer,
                AvatarAddress,
                out var agentState,
                out var avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            avatarState.ValidateEquipments(equipmentIds, context.BlockIndex);

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world) ||
                world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            avatarState.EquipCostumes(costumeIds);
            avatarState.EquipEquipments(equipmentIds);

            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the opponent ({EnemyAddress}) was failed to load.");
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);
            
            if (weeklyArenaState.Ended)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(AvatarAddress);
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException();
            }

            if (!arenaInfo.Active)
            {
                FungibleAssetValue agentBalance = default;
                try
                {
                    agentBalance = states.GetBalance(ctx.Signer, states.GetGoldCurrency());
                }
                catch (InvalidOperationException)
                {
                    throw new NotEnoughFungibleAssetValueException(EntranceFee, agentBalance);
                }

                if (agentBalance >= new FungibleAssetValue(agentBalance.Currency, EntranceFee, 0))
                {
                    states = states.TransferAsset(
                        ctx.Signer,
                        WeeklyArenaAddress,
                        new FungibleAssetValue(
                            states.GetGoldCurrency(),
                            EntranceFee,
                            0
                        )
                    );
                    arenaInfo.Activate();
                }
                else
                {
                    throw new NotEnoughFungibleAssetValueException(EntranceFee, agentBalance);
                }
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(EnemyAddress);
            }

            Log.Debug(weeklyArenaState.address.ToHex());

            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                consumableIds,
                states.GetRankingSimulatorSheets(),
                StageId,
                arenaInfo,
                weeklyArenaState[EnemyAddress]);

            simulator.Simulate();

            Result = simulator.Log;

            foreach (var itemBase in simulator.Reward)
            {
                avatarState.inventory.AddItem(itemBase);
            }

            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(WeeklyArenaAddress, weeklyArenaState.Serialize())
                .SetState(AvatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["enemyAddress"] = EnemyAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
                ["costume_ids"] = new Bencodex.Types.List(costumeIds.Select(e => e.Serialize())),
                ["equipment_ids"] = new Bencodex.Types.List(equipmentIds.Select(e => e.Serialize())),
                ["consumable_ids"] = new Bencodex.Types.List(consumableIds.Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((Bencodex.Types.List) plainValue["costume_ids"]).Select(
                e => e.ToInteger()
            ).ToList();
            equipmentIds = ((Bencodex.Types.List) plainValue["equipment_ids"]).Select(
                e => e.ToGuid()
            ).ToList();
            consumableIds = ((Bencodex.Types.List) plainValue["consumable_ids"]).Select(
                e => e.ToGuid()
            ).ToList();

        }
    }
}
