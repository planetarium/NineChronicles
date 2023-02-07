using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Arena;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.GrandFinale;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.GrandFinale;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1679
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeName)]
    public class BattleGrandFinale : GameAction, IBattleGrandFinaleV1
    {
        private const string ActionTypeName = "battle_grand_finale2";
        public const int WinScore = 20;
        public const int LoseScore = 1;
        public const int DefaultScore = 1000;
        public const string ScoreDeriveKey = "grand_finale_score_{0}";

        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public int grandFinaleId;

        public List<Guid> costumes;
        public List<Guid> equipments;

        public ArenaPlayerDigest ExtraMyArenaPlayerDigest;
        public ArenaPlayerDigest ExtraEnemyArenaPlayerDigest;

        Address IBattleGrandFinaleV1.MyAvatarAddress => myAvatarAddress;
        Address IBattleGrandFinaleV1.EnemyAvatarAddress => enemyAvatarAddress;
        int IBattleGrandFinaleV1.GrandFinaleId => grandFinaleId;
        IEnumerable<Guid> IBattleGrandFinaleV1.Costumes => costumes;
        IEnumerable<Guid> IBattleGrandFinaleV1.Equipments => equipments;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                [MyAvatarAddressKey] = myAvatarAddress.Serialize(),
                [EnemyAvatarAddressKey] = enemyAvatarAddress.Serialize(),
                [GrandFinaleIdKey] = grandFinaleId.Serialize(),
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
            grandFinaleId = plainValue[GrandFinaleIdKey].ToInteger();
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
            Log.Debug("{AddressesHex}BattleGrandFinale exec started", addressesHex);

            #region Validate

            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    myAvatarAddress,
                    out var avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(GrandFinaleScheduleSheet),
                    typeof(GrandFinaleParticipantsSheet),
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

            var grandFinaleSheet = sheets.GetSheet<GrandFinaleScheduleSheet>();
            if (!grandFinaleSheet.TryGetValue(grandFinaleId, out var grandFinaleRow))
            {
                throw new SheetRowNotFoundException(nameof(GrandFinaleScheduleSheet),
                    $"grandFinale Id : {grandFinaleId}");
            }

            if (!grandFinaleRow.IsOpened(context.BlockIndex))
            {
                throw new ThisArenaIsClosedException(
                    $"{nameof(BattleGrandFinale)} : block index({context.BlockIndex}) - ");
            }

            var grandFinaleParticipantsSheet = sheets.GetSheet<GrandFinaleParticipantsSheet>();
            if (!grandFinaleParticipantsSheet.TryGetValue(grandFinaleId, out var participantsRow))
            {
                throw new SheetRowNotFoundException(nameof(GrandFinaleParticipantsSheet),
                    $"grandFinale Id : {grandFinaleId}");
            }

            if (!participantsRow.Participants.Contains(myAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleGrandFinale)}] my avatar address : {myAvatarAddress}");
            }

            if (!participantsRow.Participants.Contains(enemyAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleGrandFinale)}] enemy avatar address : {enemyAvatarAddress}");
            }

            var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            if (!states.TryGetArenaAvatarState(myArenaAvatarStateAdr, out var myArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"[{nameof(BattleGrandFinale)}] my avatar address : {myAvatarAddress}");
            }

            var enemyArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(enemyAvatarAddress);
            if (!states.TryGetArenaAvatarState(
                    enemyArenaAvatarStateAdr,
                    out var enemyArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}");
            }

            var scoreAddress = myAvatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, ScoreDeriveKey, grandFinaleId));
            if (!states.TryGetState(scoreAddress, out Integer grandFinaleScore))
            {
                grandFinaleScore = DefaultScore;
            }

            var informationAdr = GrandFinaleInformation.DeriveAddress(
                myAvatarAddress,
                grandFinaleId);
            GrandFinaleInformation grandFinaleInformation;
            if (states.TryGetState(informationAdr, out List serialized))
            {
                grandFinaleInformation = new GrandFinaleInformation(serialized);
            }
            else
            {
                grandFinaleInformation = new GrandFinaleInformation(
                    myAvatarAddress,
                    grandFinaleId);
            }

            if (grandFinaleInformation.TryGetBattleRecord(enemyAvatarAddress,
                    out _))
            {
                throw new AlreadyFoughtAvatarException(
                    $"[{nameof(BattleGrandFinale)}] enemy avatar address : {enemyAvatarAddress}");
            }

            #endregion

            // update arena avatar state
            myArenaAvatarState.UpdateEquipment(equipments);
            myArenaAvatarState.UpdateCostumes(costumes);

            // simulate
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            ExtraMyArenaPlayerDigest = new ArenaPlayerDigest(avatarState, myArenaAvatarState);
            ExtraEnemyArenaPlayerDigest =
                new ArenaPlayerDigest(enemyAvatarState, enemyArenaAvatarState);
            var arenaSheets = sheets.GetArenaSimulatorSheets();
            var simulator = new ArenaSimulator(context.Random);
            var log = simulator.Simulate(
                ExtraMyArenaPlayerDigest,
                ExtraEnemyArenaPlayerDigest,
                arenaSheets);

            var win = log.Result.Equals(ArenaLog.ArenaResult.Win);
            grandFinaleScore += win ? WinScore : LoseScore;
            grandFinaleInformation.UpdateRecord(enemyAvatarAddress, win);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}BattleGrandFinale Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(myArenaAvatarStateAdr, myArenaAvatarState.Serialize())
                .SetState(scoreAddress, grandFinaleScore)
                .SetState(informationAdr, grandFinaleInformation.Serialize());
        }
    }
}
