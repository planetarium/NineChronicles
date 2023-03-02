using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
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
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1663
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1649
    /// Updated at https://github.com/planetarium/lib9c/pull/1679
    /// </summary>
    [Serializable]
    [ActionType("battle_arena9")]
    public class BattleArena : GameAction, IBattleArenaV1
    {
        public const string PurchasedCountKey = "purchased_count_during_interval";
        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public int championshipId;
        public int round;
        public int ticket;

        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<RuneSlotInfo> runeInfos;

        public ArenaPlayerDigest ExtraMyArenaPlayerDigest;
        public ArenaPlayerDigest ExtraEnemyArenaPlayerDigest;
        public int ExtraPreviousMyScore;

        Address IBattleArenaV1.MyAvatarAddress => myAvatarAddress;

        Address IBattleArenaV1.EnemyAvatarAddress => enemyAvatarAddress;

        int IBattleArenaV1.ChampionshipId => championshipId;

        int IBattleArenaV1.Round => round;

        int IBattleArenaV1.Ticket => ticket;

        IEnumerable<Guid> IBattleArenaV1.Costumes => costumes;

        IEnumerable<Guid> IBattleArenaV1.Equipments => equipments;

        IEnumerable<IValue> IBattleArenaV1.RuneSlotInfos => runeInfos
            .Select(x => x.Serialize());

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
                [RuneInfos] = runeInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
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
            runeInfos = plainValue[RuneInfos].ToList(x => new RuneSlotInfo((List)x));
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
                    typeof(RuneListSheet),
                });

            avatarState.ValidEquipmentAndCostume(costumes, equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex, addressesHex);

            // update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var runeSlotState = states.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(runeInfos, runeListSheet);
            states = states.SetState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var itemSlotState = states.TryGetState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);
            itemSlotState.UpdateEquipment(equipments);
            itemSlotState.UpdateCostumes(costumes);
            states = states.SetState(itemSlotStateAddress, itemSlotState.Serialize());

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

            var gameConfigState = states.GetGameConfigState();
            var battleArenaInterval = roundData.ArenaType == ArenaType.OffSeason
                ? 0
                : gameConfigState.BattleArenaInterval;
            if (context.BlockIndex - myArenaAvatarState.LastBattleBlockIndex < battleArenaInterval)
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

            var dailyArenaInterval = gameConfigState.DailyArenaInterval;
            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                context.BlockIndex, roundData.StartBlockIndex, dailyArenaInterval);
            var purchasedCountAddr = arenaInformation.Address.Derive(PurchasedCountKey);
            if (!states.TryGetState(purchasedCountAddr, out Integer purchasedCountDuringInterval))
            {
                purchasedCountDuringInterval = 0;
            }

            if (arenaInformation.TicketResetCount < currentTicketResetCount)
            {
                arenaInformation.ResetTicket(currentTicketResetCount);
                purchasedCountDuringInterval = 0;
                states = states.SetState(purchasedCountAddr, purchasedCountDuringInterval);
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
            else if (ticket > 1)
            {
                throw new TicketPurchaseLimitExceedException(
                    $"[{nameof(ArenaInformation)}] tickets to buy : {ticket}");
            }
            else
            {
                var arenaAdr =
                    ArenaHelper.DeriveArenaAddress(roundData.ChampionshipId, roundData.Round);
                var goldCurrency = states.GetGoldCurrency();
                var ticketBalance =
                    ArenaHelper.GetTicketPrice(roundData, arenaInformation, goldCurrency);
                arenaInformation.BuyTicket(roundData.MaxPurchaseCount);
                if (purchasedCountDuringInterval >= roundData.MaxPurchaseCountWithInterval)
                {
                    throw new ExceedTicketPurchaseLimitDuringIntervalException(
                        $"[{nameof(ArenaInformation)}] PurchasedTicketCount({purchasedCountDuringInterval}) >= MAX({{max}})");
                }

                purchasedCountDuringInterval++;
                states = states
                    .TransferAsset(context.Signer, arenaAdr, ticketBalance)
                    .SetState(purchasedCountAddr, purchasedCountDuringInterval);
            }

            // update arena avatar state
            myArenaAvatarState.UpdateEquipment(equipments);
            myArenaAvatarState.UpdateCostumes(costumes);
            myArenaAvatarState.LastBattleBlockIndex = context.BlockIndex;
            var runeStates = new List<RuneState>();
            foreach (var address in runeInfos.Select(info => RuneState.DeriveAddress(myAvatarAddress, info.RuneId)))
            {
                if (states.TryGetState(address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }

            // get enemy equipped items
            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyItemSlotState = states.TryGetState(enemyItemSlotStateAddress, out List rawEnemyItemSlotState)
                ? new ItemSlotState(rawEnemyItemSlotState)
                : new ItemSlotState(BattleType.Arena);
            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyRuneSlotState = states.TryGetState(enemyRuneSlotStateAddress, out List enemyRawRuneSlotState)
                ? new RuneSlotState(enemyRawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = new List<RuneState>();
            var enemyRuneSlotInfos = enemyRuneSlotState.GetEquippedRuneSlotInfos();
            foreach (var address in enemyRuneSlotInfos.Select(info => RuneState.DeriveAddress(myAvatarAddress, info.RuneId)))
            {
                if (states.TryGetState(address, out List rawRuneState))
                {
                    enemyRuneStates.Add(new RuneState(rawRuneState));
                }
            }

            // simulate
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            ExtraMyArenaPlayerDigest = new ArenaPlayerDigest(
                avatarState,
                equipments,
                costumes,
                runeStates);
            ExtraEnemyArenaPlayerDigest = new ArenaPlayerDigest(
                enemyAvatarState,
                enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes,
                enemyRuneStates);
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
