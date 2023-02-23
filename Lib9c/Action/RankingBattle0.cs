using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
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
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("ranking_battle")]
    public class RankingBattle0 : GameAction, IRankingBattleV1
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

        Address IRankingBattleV1.AvatarAddress => AvatarAddress;
        Address IRankingBattleV1.EnemyAddress => EnemyAddress;
        Address IRankingBattleV1.WeeklyArenaAddress => WeeklyArenaAddress;
        IEnumerable<int> IRankingBattleV1.CostumeIds => costumeIds;
        IEnumerable<Guid> IRankingBattleV1.EquipmentIds => equipmentIds;
        IEnumerable<Guid> IRankingBattleV1.ConsumableIds => consumableIds;

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

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress, EnemyAddress);

            Log.Warning("ranking_battle is deprecated. Please use ranking_battle2");
            if (AvatarAddress.Equals(EnemyAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAgentAvatarStates(
                ctx.Signer,
                AvatarAddress,
                out var agentState,
                out var avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var costumes = new HashSet<int>(costumeIds);

            avatarState.ValidateEquipments(equipmentIds, context.BlockIndex);
            avatarState.ValidateConsumable(consumableIds, context.BlockIndex);
            avatarState.ValidateCostume(costumes);

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world) ||
                world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            avatarState.EquipCostumes(costumes);
            avatarState.EquipEquipments(equipmentIds);

            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the opponent ({EnemyAddress}) was failed to load.");
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);

            if (weeklyArenaState.Ended)
            {
                throw new WeeklyArenaStateAlreadyEndedException(
                    addressesHex + WeeklyArenaStateAlreadyEndedException.BaseMessage);
            }

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(addressesHex, AvatarAddress);
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException(
                    addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
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
                    throw new NotEnoughFungibleAssetValueException(addressesHex, EntranceFee, agentBalance);
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
                    throw new NotEnoughFungibleAssetValueException(addressesHex, EntranceFee, agentBalance);
                }
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(addressesHex, EnemyAddress);
            }

            Log.Verbose("{WeeklyArenaStateAddress}", weeklyArenaState.address.ToHex());

            var simulator = new RankingSimulatorV1(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                consumableIds,
                states.GetRankingSimulatorSheetsV1(),
                StageId,
                arenaInfo,
                weeklyArenaState[EnemyAddress]);

            simulator.SimulateV1();

            Result = simulator.Log;

            foreach (var itemBase in simulator.Reward.OrderBy(i => i.Id))
            {
                avatarState.inventory.AddItem2(itemBase);
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
                ["costume_ids"] = new Bencodex.Types.List(costumeIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["equipment_ids"] = new Bencodex.Types.List(equipmentIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["consumable_ids"] = new Bencodex.Types.List(consumableIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((Bencodex.Types.List) plainValue["costume_ids"])
                .Select(e => e.ToInteger())
                .ToList();
            equipmentIds = ((Bencodex.Types.List) plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            consumableIds = ((Bencodex.Types.List) plainValue["consumable_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
