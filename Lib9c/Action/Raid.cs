using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("raid")]
    public class Raid : GameAction
    {
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public List<Guid> CostumeIds;
        public List<Guid> FoodIds;
        public bool PayNcg;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress,
                    out AvatarState avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer was failed to load.");
            }
            // Check stage level.
            if (!avatarState.worldInformation.IsStageCleared(50))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out int current);
                throw new NotEnoughClearedStageLevelException(AvatarAddress.ToHex(),
                    50, current);
            }

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(
                containRaidSimulatorSheets: true,
                sheetTypes: new [] {
                typeof(MaterialItemSheet),
                typeof(SkillSheet),
                typeof(SkillBuffSheet),
                typeof(BuffSheet),
                typeof(CharacterLevelSheet),
                typeof(EquipmentItemSetEffectSheet),
                typeof(WorldBossCharacterSheet),
                typeof(WorldBossListSheet),
                typeof(WorldBossGlobalHpSheet),
                typeof(EnemySkillSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
                typeof(RuneWeightSheet),
                typeof(WorldBossKillRewardSheet),
                typeof(RuneSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            var row = worldBossListSheet.FindRowByBlockIndex(context.BlockIndex);
            int raidId = row.Id;
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
            avatarState.ValidateEquipmentsV2(EquipmentIds, context.BlockIndex);
            avatarState.ValidateConsumable(FoodIds, context.BlockIndex);
            avatarState.ValidateCostume(CostumeIds);
            // Reward.
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
            // Update State.
            if (raiderState.LatestBossLevel < bossState.Level)
            {
                // reward
                var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(AvatarAddress, raidId);
                WorldBossKillRewardRecord rewardRecord;
                if (states.TryGetState(worldBossKillRewardRecordAddress, out List rawList))
                {
                    rewardRecord = new WorldBossKillRewardRecord(rawList);
                    // calculate with previous high score.
                    int rank = WorldBossHelper.CalculateRank(previousHighScore);
                    states = states.SetWorldBossKillReward(
                        worldBossKillRewardRecordAddress,
                        rewardRecord,
                        rank,
                        bossState,
                        sheets.GetSheet<RuneWeightSheet>(),
                        sheets.GetSheet<WorldBossKillRewardSheet>(),
                        sheets.GetSheet<RuneSheet>(),
                        context.Random,
                        AvatarAddress
                    );
                }
                else
                {
                    rewardRecord = new WorldBossKillRewardRecord();
                }

                // Simulate.
                var simulator = new RaidSimulator(
                    row.BossId,
                    context.Random,
                    avatarState,
                    FoodIds,
                    sheets.GetRaidSimulatorSheets());
                simulator.Simulate();

                int score = simulator.DamageDealt;
                int cp = CPHelper.GetCPV2(avatarState, sheets.GetSheet<CharacterSheet>(),
                    sheets.GetSheet<CostumeStatSheet>());
                raiderState.Update(avatarState, cp, score, PayNcg);

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
                // Save level infos;
                raiderState.LatestBossLevel = bossState.Level;
                if (!rewardRecord.ContainsKey(raiderState.LatestBossLevel))
                {
                    rewardRecord.Add(raiderState.LatestBossLevel, false);
                }
                states = states.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
            }
            return states
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
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
            CostumeIds = plainValue["c"].ToList(StateExtensions.ToGuid);
            FoodIds = plainValue["f"].ToList(StateExtensions.ToGuid);
        }
    }
}
