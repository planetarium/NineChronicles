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
#if UNITY_EDITOR || UNITY_STANDALONE
using TentuPlay.Api;
#endif

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
                return states;
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                return states;
            }

            // 도전자의 장비가 유효한지 검사한다.
            // 피도전자의 장비도 검사해야 하는가는 모르겠다. 이후에 필요하다면 추가하는 것으로 한다.
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
                    
                    switch (equipment.Data.ItemSubType)
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
                    return states;
                }
            }

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                out var world))
                return states;

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                // 스테이지 클리어 부족 에러.
                return states;
            }

            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                return states;
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                return states;
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                return states;
            }

            if (!arenaInfo.Active)
            {
                if (agentState.gold >= 100)
                {
                    agentState.gold -= 100;
                    weeklyArenaState.Gold += 100;
                    arenaInfo.Activate();
                }
                else
                {
                    return states;
                }
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))

            {
                return states;
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

#if UNITY_EDITOR || UNITY_STANDALONE
            //TentuPlay
            TPStashEvent MyStashEvent = new TPStashEvent();
            MyStashEvent.CurrencyUse(
                player_uuid: agentState.address.ToHex(),
                currency_slug: "gold",
                currency_quantity: (float)100,
                currency_total_quantity: (float)agentState.gold,
                reference_entity: "stage_pvp",
                reference_category_slug: "arena",
                reference_slug: "WeeklyArena"
                );
#endif
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
