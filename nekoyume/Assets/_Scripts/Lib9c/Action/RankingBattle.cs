using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("ranking_battle")]
    public class RankingBattle : GameAction
    {
        public Address AvatarAddress;
        public Address EnemyAddress;
        public Address WeeklyArenaAddress;
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
                    .SetState(ctx.Signer, MarkChanged);
            }

            if (AvatarAddress.Equals(EnemyAddress))
            {
                return LogError(context, "Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }

            // 도전자의 장비가 유효한지 검사한다.
            // 피도전자의 장비도 검사해야 하는가는 모르겠다. 이후에 필요하다면 추가하는 것으로 한다.
            // FIXME: 이하의 코드 블록이 상당부분 HackAndSlash.Execute()에도 중복되어 들어있습니다.
            // 한 쪽 로직만 수정하는 실수가 일어나기 쉬우니 로직을 하나로 합쳐서 양쪽에서 공통 로직을 가져다 쓰게 하는 게
            // 좋을 듯합니다.
            {
                var equipments = avatarState.inventory.Items
                    .Select(e => e.item)
                    .OfType<Equipment>()
                    .Where(e => e.equipped)
                    .ToList();
                var level = avatarState.level;
                var ringCount = 0;
                var failed = false;
                foreach (var equipment in equipments)
                {
                    if (equipment.RequiredBlockIndex > context.BlockIndex)
                    {
                        failed = true;
                        break;
                    }
                    
                    switch (equipment.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                            break;
                        case ItemSubType.Armor:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                            break;
                        case ItemSubType.Belt:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                            break;
                        case ItemSubType.Necklace:
                            failed = level < GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                            break;
                        case ItemSubType.Ring:
                            ringCount++;
                            var requireLevel = ringCount == 1
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                                : ringCount == 2
                                    ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                    : int.MaxValue;
                            failed = level < requireLevel;
                            break;
                        default:
                            failed = true;
                            break;
                    }

                    if (failed)
                        break;
                }

                if (failed)
                {
                    // 장비가 유효하지 않은 에러.
                    return LogError(context, "Aborted as the equipment is invalid.");
                }
            }

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
            {
                return LogError(context, "Aborted as the WorldInformation was failed to load or not cleared yet.");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                // 스테이지 클리어 부족 에러.
                return LogError(
                    context,
                    "Aborted as the signer is not cleared the minimum stage level required to battle with other players yet: {ClearedLevel} < {RequiredLevel}.",
                    world.StageClearedId,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard
                );
            }

            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                return LogError(
                    context,
                    "Aborted as the avatar state of the opponent ({OpponentAddress}) was failed to load.",
                    EnemyAddress
                );
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                return LogError(context, "Aborted as the weekly arena state was failed to load.");
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                return LogError(context, "Aborted as the arena state reached the daily limit.");
            }

            if (!arenaInfo.Active)
            {
                const decimal EntranceFee = 100;
                if (agentState.gold >= EntranceFee)
                {
                    agentState.gold -= EntranceFee;
                    weeklyArenaState.Gold += EntranceFee;
                    arenaInfo.Activate();
                }
                else
                {
                    return LogError(
                        context,
                        "Aborted as the signer's balance ({Balance}) is insufficient to pay entrance fee/stake ({EntranceFee}).",
                        agentState.gold,
                        EntranceFee
                    );
                }
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))
            {
                return LogError(
                    context,
                    "Aborted as the opponent ({OpponentAddress}) is not registered in the weekly arena state.",
                    EnemyAddress
                );
            }

            Log.Debug(weeklyArenaState.address.ToHex());

            var tableSheetState = TableSheetsState.FromActionContext(ctx);
            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);

            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                new List<Consumable>(),
                tableSheets);

            simulator.Simulate();

            simulator.Log.diffScore = arenaInfo.Update(avatarState, weeklyArenaState[EnemyAddress], simulator.Result);
            simulator.Log.score = arenaInfo.Score;

            Result = simulator.Log;

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
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
        }
    }
}
