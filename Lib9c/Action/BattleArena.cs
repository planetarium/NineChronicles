using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1370
    /// </summary>
    [Serializable]
    [ActionType("battle_arena5")]
    public class BattleArena : GameAction
    {
        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public int championshipId;
        public int round;
        public int ticket;

        public List<Guid> costumes;
        public List<Guid> equipments;

        public ArenaPlayerDigest ExtraMyArenaPlayerDigest;
        public ArenaPlayerDigest ExtraEnemyArenaPlayerDigest;
        public int ExtraPreviousMyScore;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                [MyAvatarAddressKey] = myAvatarAddress.Serialize(),
                [EnemyAvatarAddressKey] = enemyAvatarAddress.Serialize(),
                [ChampionshipIdKey] = championshipId.Serialize(),
                [RoundKey] = round.Serialize(),
                [TicketKey] = ticket.Serialize(),
                [CostumesKey] = new List(costumes
                    .OrderBy(element => element).Select(e => e.Serialize())),
                [EquipmentsKey] = new List(equipments
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            myAvatarAddress = plainValue[MyAvatarAddressKey].ToAddress();
            enemyAvatarAddress = plainValue[EnemyAvatarAddressKey].ToAddress();
            championshipId = plainValue[ChampionshipIdKey].ToInteger();
            round = plainValue[RoundKey].ToInteger();
            ticket = plainValue[TicketKey].ToInteger();
            costumes = ((List)plainValue[CostumesKey]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue[EquipmentsKey]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(
                context,
                myAvatarAddress,
                enemyAvatarAddress);

            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}BattleArena exec started", addressesHex);
            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    myAvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                throw new NotEnoughClearedStageLevelException(
                    $"{addressesHex}Aborted as NotEnoughClearedStageLevelException");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(MaterialItemSheet),
                });

            avatarState.ValidEquipmentAndCostume(costumes, equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex, addressesHex);

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var arenaRow))
            {
                throw new SheetRowNotFoundException(nameof(ArenaSheet),
                    $"championship Id : {championshipId}");
            }

            if (!arenaRow.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({arenaRow.ChampionshipId}) - " +
                    $"round({round})");
            }

            if (!roundData.IsTheRoundOpened(context.BlockIndex))
            {
                throw new ThisArenaIsClosedException(
                    $"{nameof(BattleArena)} : block index({context.BlockIndex}) - " +
                    $"championshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            var arenaParticipantsAdr =
                ArenaParticipants.DeriveAddress(roundData.ChampionshipId, roundData.Round);
            if (!states.TryGetArenaParticipants(arenaParticipantsAdr, out var arenaParticipants))
            {
                throw new ArenaParticipantsNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({roundData.ChampionshipId}) - " +
                    $"round({roundData.Round})");
            }

            if (!arenaParticipants.AvatarAddresses.Contains(myAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
            }

            if (!arenaParticipants.AvatarAddresses.Contains(enemyAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}");
            }

            var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            if (!states.TryGetArenaAvatarState(myArenaAvatarStateAdr, out var myArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
            }

            if (context.BlockIndex - myArenaAvatarState.LastBattleBlockIndex < 2)
            {
                throw new CoolDownBlockException(
                    $"[{nameof(BattleArena)}] LastBattleBlockIndex : " +
                    $"{myArenaAvatarState.LastBattleBlockIndex} " +
                    $"CurrentBlockIndex : {context.BlockIndex}");
            }

            var enemyArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(enemyAvatarAddress);
            if (!states.TryGetArenaAvatarState(
                    enemyArenaAvatarStateAdr,
                    out var enemyArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}");
            }

            var myArenaScoreAdr = ArenaScore.DeriveAddress(
                myAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaScore(myArenaScoreAdr, out var myArenaScore))
            {
                throw new ArenaScoreNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            var enemyArenaScoreAdr = ArenaScore.DeriveAddress(
                enemyAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaScore(enemyArenaScoreAdr, out var enemyArenaScore))
            {
                throw new ArenaScoreNotFoundException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            var arenaInformationAdr = ArenaInformation.DeriveAddress(
                myAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaInformation(arenaInformationAdr, out var arenaInformation))
            {
                throw new ArenaInformationNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            if (!ArenaHelper.ValidateScoreDifference(
                    ArenaHelper.ScoreLimits,
                    roundData.ArenaType,
                    myArenaScore.Score,
                    enemyArenaScore.Score))
            {
                var scoreDiff = enemyArenaScore.Score - myArenaScore.Score;
                throw new ValidateScoreDifferenceException(
                    $"[{nameof(BattleArena)}] Arena Type({roundData.ArenaType}) : " +
                    $"enemyScore({enemyArenaScore.Score}) - myScore({myArenaScore.Score}) = " +
                    $"diff({scoreDiff})");
            }

            var gameConfigState = states.GetGameConfigState();
            var interval = gameConfigState.DailyArenaInterval;
            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                context.BlockIndex, roundData.StartBlockIndex, interval);
            if (arenaInformation.TicketResetCount < currentTicketResetCount)
            {
                arenaInformation.ResetTicket(currentTicketResetCount);
            }

            if (roundData.ArenaType != ArenaType.OffSeason && ticket > 1)
            {
                throw new ExceedPlayCountException($"[{nameof(BattleArena)}] " +
                                                   $"ticket : {ticket} / arenaType : " +
                                                   $"{roundData.ArenaType}");
            }

            if (arenaInformation.Ticket > 0)
            {
                arenaInformation.UseTicket(ticket);
            }
            else
            {
                var arenaAdr =
                    ArenaHelper.DeriveArenaAddress(roundData.ChampionshipId, roundData.Round);
                var goldCurrency = states.GetGoldCurrency();
                for (var i = 0; i < ticket; i++)
                {
                    var ticketBalance =
                        ArenaHelper.GetTicketPrice(roundData, arenaInformation, goldCurrency);
                    states = states.TransferAsset(
                        context.Signer,
                        arenaAdr,
                        ticketBalance);
                    arenaInformation.BuyTicket(roundData);
                }
            }

            // update arena avatar state
            myArenaAvatarState.UpdateEquipment(equipments);
            myArenaAvatarState.UpdateCostumes(costumes);
            myArenaAvatarState.LastBattleBlockIndex = context.BlockIndex;

            // simulate
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            ExtraMyArenaPlayerDigest = new ArenaPlayerDigest(avatarState, myArenaAvatarState);
            ExtraEnemyArenaPlayerDigest =
                new ArenaPlayerDigest(enemyAvatarState, enemyArenaAvatarState);
            ExtraPreviousMyScore = myArenaScore.Score;
            var arenaSheets = sheets.GetArenaSimulatorSheets();
            var winCount = 0;
            var defeatCount = 0;
            var rewards = new List<ItemBase>();
            for (var i = 0; i < ticket; i++)
            {
                var simulator = new ArenaSimulator(context.Random);
                var log = simulator.Simulate(
                    ExtraMyArenaPlayerDigest,
                    ExtraEnemyArenaPlayerDigest,
                    arenaSheets);
                if (log.Result.Equals(ArenaLog.ArenaResult.Win))
                {
                    winCount++;
                }
                else
                {
                    defeatCount++;
                }

                var reward = RewardSelector.Select(
                    context.Random,
                    sheets.GetSheet<WeeklyArenaRewardSheet>(),
                    sheets.GetSheet<MaterialItemSheet>(),
                    ExtraMyArenaPlayerDigest.Level,
                    maxCount: ArenaHelper.GetRewardCount(ExtraPreviousMyScore));
                rewards.AddRange(reward);
            }

            // add reward
            foreach (var itemBase in rewards.OrderBy(x => x.Id))
            {
                avatarState.inventory.AddItem(itemBase);
            }

            // add medal
            if (roundData.ArenaType != ArenaType.OffSeason &&
                winCount > 0)
            {
                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var medal = ArenaHelper.GetMedal(
                    roundData.ChampionshipId,
                    roundData.Round,
                    materialSheet);
                avatarState.inventory.AddItem(medal, count: winCount);
            }

            // update record
            var (myWinScore, myDefeatScore, enemyWinScore) =
                ArenaHelper.GetScores(ExtraPreviousMyScore, enemyArenaScore.Score);
            var myScore = (myWinScore * winCount) + (myDefeatScore * defeatCount);
            myArenaScore.AddScore(myScore);
            enemyArenaScore.AddScore(enemyWinScore * winCount);
            arenaInformation.UpdateRecord(winCount, defeatCount);

            if (migrationRequired)
            {
                states = states
                    .SetState(myAvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        myAvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        myAvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}BattleArena Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(myArenaAvatarStateAdr, myArenaAvatarState.Serialize())
                .SetState(myArenaScoreAdr, myArenaScore.Serialize())
                .SetState(enemyArenaScoreAdr, enemyArenaScore.Serialize())
                .SetState(arenaInformationAdr, arenaInformation.Serialize())
                .SetState(
                    myAvatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
        }
    }
}
