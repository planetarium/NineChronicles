using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;

using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100360ObsoleteIndex)]
    [ActionType("raid")]
    public class Raid1 : GameAction, IRaidV1
    {
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public List<Guid> CostumeIds;
        public List<Guid> FoodIds;
        public bool PayNcg;

        Address IRaidV1.AvatarAddress => AvatarAddress;
        IEnumerable<Guid> IRaidV1.EquipmentIds => EquipmentIds;
        IEnumerable<Guid> IRaidV1.CostumeIds => CostumeIds;
        IEnumerable<Guid> IRaidV1.FoodIds => FoodIds;
        bool IRaidV1.PayNcg => PayNcg;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            CheckObsolete(ActionObsoleteConfig.V100360ObsoleteIndex, context);

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress,
                    out AvatarState avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer was failed to load.");
            }
            // Check stage level.
            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInRaid))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out int current);
                throw new NotEnoughClearedStageLevelException(AvatarAddress.ToHex(),
                    GameConfig.RequireClearedStageLevel.ActionsInRaid, current);
            }

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheetsV1(
                containRaidSimulatorSheets: true,
                sheetTypes: new [] {
                typeof(MaterialItemSheet),
                typeof(SkillSheet),
                typeof(SkillBuffSheet),
                typeof(StatBuffSheet),
                typeof(CharacterLevelSheet),
                typeof(EquipmentItemSetEffectSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(WorldBossCharacterSheet),
                typeof(WorldBossListSheet),
                typeof(WorldBossGlobalHpSheet),
                typeof(WorldBossActionPatternSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
                typeof(RuneWeightSheet),
                typeof(WorldBossKillRewardSheet),
                typeof(RuneSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            var row = worldBossListSheet.FindRowByBlockIndex(context.BlockIndex);
            int raidId = row.Id;
            if (raidId > 1)
            {
                throw new ActionObsoletedException("raid action is obsoleted. please use new action.");
            }
            Address worldBossAddress = Addresses.GetWorldBossAddress(raidId);
            Address raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            // Check challenge count.
            RaiderState raiderState;
            if (states.TryGetState(raiderAddress, out List rawState))
            {
                raiderState = new RaiderState(rawState);
            }
            else
            {
                raiderState = new RaiderState();
                FungibleAssetValue crystalCost = CrystalCalculator.CalculateEntranceFee(avatarState.level, row.EntranceFee);
                states = states.TransferAsset(context.Signer, worldBossAddress, crystalCost);
            }

            if (context.BlockIndex - raiderState.UpdatedBlockIndex < Raid.RequiredInterval)
            {
                throw new RequiredBlockIntervalException($"wait for interval. {context.BlockIndex - raiderState.UpdatedBlockIndex}");
            }

            if (WorldBossHelper.CanRefillTicket(context.BlockIndex, raiderState.RefillBlockIndex, row.StartedBlockIndex))
            {
                raiderState.RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
                raiderState.RefillBlockIndex = context.BlockIndex;
            }

            if (raiderState.RemainChallengeCount < 1)
            {
                if (PayNcg)
                {
                    if (raiderState.PurchaseCount >= row.MaxPurchaseCount)
                    {
                        throw new ExceedTicketPurchaseLimitException("");
                    }
                    var goldCurrency = states.GetGoldCurrency();
                    states = states.TransferAsset(context.Signer, worldBossAddress,
                        WorldBossHelper.CalculateTicketPrice(row, raiderState, goldCurrency));
                    raiderState.PurchaseCount++;
                }
                else
                {
                    throw new ExceedPlayCountException("");
                }
            }

            // Validate equipment, costume.
            var equipmentList = avatarState.ValidateEquipmentsV2(EquipmentIds, context.BlockIndex);
            var foodIds = avatarState.ValidateConsumable(FoodIds, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(CostumeIds);
            int previousHighScore = raiderState.HighScore;
            WorldBossState bossState;
            WorldBossGlobalHpSheet hpSheet = sheets.GetSheet<WorldBossGlobalHpSheet>();
            if (states.TryGetState(worldBossAddress, out List rawBossState))
            {
                bossState = new WorldBossState(rawBossState);
            }
            else
            {
                bossState = new WorldBossState(row, hpSheet[1]);
            }

            var addressesHex = $"[{context.Signer.ToHex()}, {AvatarAddress.ToHex()}]";
            var items = EquipmentIds.Concat(CostumeIds);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            var raidSimulatorSheets = sheets.GetRaidSimulatorSheetsV1();

            // Simulate.
            var simulator = new RaidSimulatorV1(
                row.BossId,
                context.Random,
                avatarState,
                FoodIds,
                raidSimulatorSheets,
                sheets.GetSheet<CostumeStatSheet>());
            simulator.Simulate();
            avatarState.inventory = simulator.Player.Inventory;

            int score = simulator.DamageDealt;
            int cp = CPHelper.GetCPV2(avatarState, sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            raiderState.Update(avatarState, cp, score, PayNcg, context.BlockIndex);

            // Reward.
            bossState.CurrentHp -= score;
            if (bossState.CurrentHp <= 0)
            {
                if (bossState.Level < hpSheet.OrderedList.Last().Level)
                {
                    bossState.Level++;
                }
                bossState.CurrentHp = hpSheet[bossState.Level].Hp;
            }

            // Update State.

            // battle reward
            foreach (var battleReward in simulator.AssetReward)
            {
                if (battleReward.Currency.Equals(CrystalCalculator.CRYSTAL))
                {
                    states = states.MintAsset(context.Signer, battleReward);
                }
                else
                {
                    states = states.MintAsset(AvatarAddress, battleReward);
                }
            }

            if (raiderState.LatestBossLevel < bossState.Level)
            {
                // kill reward
                var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(AvatarAddress, raidId);
                WorldBossKillRewardRecord rewardRecord;
                if (states.TryGetState(worldBossKillRewardRecordAddress, out List rawList))
                {
                    var bossRow = raidSimulatorSheets.WorldBossCharacterSheet[row.BossId];
                    rewardRecord = new WorldBossKillRewardRecord(rawList);
                    // calculate with previous high score.
                    int rank = WorldBossHelper.CalculateRank(bossRow, previousHighScore);
                    states = states.SetWorldBossKillReward(
                        worldBossKillRewardRecordAddress,
                        rewardRecord,
                        rank,
                        bossState,
                        sheets.GetSheet<RuneWeightSheet>(),
                        sheets.GetSheet<WorldBossKillRewardSheet>(),
                        sheets.GetSheet<RuneSheet>(),
                        context.Random,
                        AvatarAddress,
                        context.Signer
                    );
                }
                else
                {
                    rewardRecord = new WorldBossKillRewardRecord();
                }

                // Save level infos;
                raiderState.LatestBossLevel = bossState.Level;
                if (!rewardRecord.ContainsKey(raiderState.LatestBossLevel))
                {
                    rewardRecord.Add(raiderState.LatestBossLevel, false);
                }
                states = states.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
            }

            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInfoAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(inventoryAddress, avatarState.inventory.Serialize())
                    .SetState(worldInfoAddress, avatarState.worldInformation.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize());
            }

            return states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldBossAddress, bossState.Serialize())
                .SetState(raiderAddress, raiderState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["e"] = new List(EquipmentIds.Select(e => e.Serialize())),
                    ["c"] = new List(CostumeIds.Select(c => c.Serialize())),
                    ["f"] = new List(FoodIds.Select(f => f.Serialize())),
                    ["p"] = PayNcg.Serialize(),
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
            CostumeIds = plainValue["c"].ToList(StateExtensions.ToGuid);
            FoodIds = plainValue["f"].ToList(StateExtensions.ToGuid);
            PayNcg = plainValue["p"].ToBoolean();
        }
    }
}
