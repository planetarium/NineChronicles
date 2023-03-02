using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1374
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash_sweep7")]
    [ActionObsolete(ActionObsoleteConfig.V100340ObsoleteIndex)]
    public class HackAndSlashSweep7 : GameAction, IHackAndSlashSweepV2
    {
        public const int UsableApStoneCount = 10;

        public List<Guid> costumes;
        public List<Guid> equipments;
        public Address avatarAddress;
        public int apStoneCount;
        public int actionPoint;
        public int worldId;
        public int stageId;

        IEnumerable<Guid> IHackAndSlashSweepV2.Costumes => costumes;
        IEnumerable<Guid> IHackAndSlashSweepV2.Equipments => equipments;
        Address IHackAndSlashSweepV2.AvatarAddress => avatarAddress;
        int IHackAndSlashSweepV2.ApStoneCount => apStoneCount;
        int IHackAndSlashSweepV2.ActionPoint => actionPoint;
        int IHackAndSlashSweepV2.WorldId => worldId;
        int IHackAndSlashSweepV2.StageId => stageId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["apStoneCount"] = apStoneCount.Serialize(),
                ["actionPoint"] = actionPoint.Serialize(),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            apStoneCount = plainValue["apStoneCount"].ToInteger();
            actionPoint = plainValue["actionPoint"].ToInteger();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            CheckObsolete(ActionObsoleteConfig.V100340ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashSweep exec started", addressesHex);

            if (apStoneCount > UsableApStoneCount)
            {
                throw new UsageLimitExceedException(
                    $"Exceeded the amount of ap stones that can be used " +
                    $"apStoneCount : {apStoneCount} > UsableApStoneCount : {UsableApStoneCount}");
            }

            states.ValidateWorldId(avatarAddress, worldId);

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    avatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(MaterialItemSheet),
                    typeof(StageWaveSheet),
                    typeof(CharacterLevelSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                    typeof(SweepRequiredCPSheet),
                    typeof(StakeActionPointCoefficientSheet),
                });

            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            if (!sheets.GetSheet<StageSheet>().TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, context.BlockIndex, worldSheet);
                if (!worldInformation.TryGetWorld(worldId, out world))
                {
                    // Do nothing.
                }
            }

            if (!world.IsPlayable(stageId))
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage isn't playable;" +
                    $"StageClearedId: {world.StageClearedId}"
                );
            }

            var equipmentList = avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(costumes);
            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeIds,
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            var sweepRequiredCpSheet = sheets.GetSheet<SweepRequiredCPSheet>();
            if (!sweepRequiredCpSheet.TryGetValue(stageId, out var cpRow))
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}There is no row in SweepRequiredCPSheet: {stageId}");
            }

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
            if (cp < cpRow.RequiredCP)
            {
                throw new NotEnoughCombatPointException(
                    $"{addressesHex}Aborted due to lack of player cp ({cp} < {cpRow.RequiredCP})");
            }

            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            if (apStoneCount > 0)
            {
                // use apStone
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex,
                        count: apStoneCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
                }
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            if (actionPoint > avatarState.actionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"use AP({actionPoint}) > current AP({avatarState.actionPoint})"
                );
            }

            // burn ap
            avatarState.actionPoint -= actionPoint;
            var costAp = sheets.GetSheet<StageSheet>()[stageId].CostAP;
            if (states.TryGetStakeState(context.Signer, out var stakeState))
            {
                var currency = states.GetGoldCurrency();
                var stakedAmount = states.GetBalance(stakeState.address, currency);
                var actionPointCoefficientSheet =
                    sheets.GetSheet<StakeActionPointCoefficientSheet>();
                var stakingLevel =
                    actionPointCoefficientSheet.FindLevelByStakedAmount(context.Signer,
                        stakedAmount);
                costAp = actionPointCoefficientSheet.GetActionPointByStaking(
                    costAp,
                    1,
                    stakingLevel);
            }

            var apMaxPlayCount = costAp > 0 ? gameConfigState.ActionPointMax / costAp : 0;
            var apStonePlayCount = apMaxPlayCount * apStoneCount;
            var apPlayCount = costAp > 0 ? actionPoint / costAp : 0;
            var playCount = apStonePlayCount + apPlayCount;
            if (playCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0. " +
                    $"current playCount : {playCount}");
            }

            var stageWaveSheet = sheets.GetSheet<StageWaveSheet>();
            avatarState.UpdateMonsterMap(stageWaveSheet, stageId);

            var rewardItems = HackAndSlashSweep6.GetRewardItems(
                context.Random,
                playCount,
                stageRow,
                materialItemSheet);
            avatarState.UpdateInventory(rewardItems);

            var levelSheet = sheets.GetSheet<CharacterLevelSheet>();
            var (level, exp) = avatarState.GetLevelAndExp(levelSheet, stageId, playCount);
            avatarState.UpdateExp(level, exp);

            if (migrationRequired)
            {
                states = states.SetState(
                    avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize());
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashSweep Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(
                    avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());
        }
    }
}
