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

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new [] {
                typeof(WorldBossListSheet),
                typeof(WorldBossGlobalHpSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            var row = worldBossListSheet.FindRowByBlockIndex(context.BlockIndex);
            int raidId = row.Id;
            Address worldBossAddress = Addresses.GetWorldBossAddress(raidId);
            Address raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            Address raidersAddress = Addresses.GetRaidersAddress(raidId);

            // Check challenge count.
            RaiderState raiderState;
            if (states.TryGetState(raiderAddress, out List rawState))
            {
                raiderState = new RaiderState(rawState);
            }
            else
            {
                raiderState = new RaiderState();
                // FIXME delete raiders & calculate rank in DP.
                List<Address> raiders = states.TryGetState(raidersAddress, out List rawRaiders)
                    ? rawRaiders.ToList(StateExtensions.ToAddress)
                    : new List<Address>();
                raiders.Add(AvatarAddress);
                FungibleAssetValue crystalCost = CrystalCalculator.CalculateEntranceFee(avatarState.level, row.EntranceFee);
                states = states
                    .SetState(raidersAddress, new List(raiders.Select(a => a.Serialize())))
                    .TransferAsset(context.Signer, worldBossAddress, crystalCost);
            }

            if (raiderState.RemainChallengeCount < 1)
            {
                if (WorldBossHelper.CanRefillTicket(context.BlockIndex, raiderState.RefillBlockIndex, row.StartedBlockIndex))
                {
                    raiderState.RemainChallengeCount = 3;
                    raiderState.RefillBlockIndex = context.BlockIndex;
                }
                else
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
            }

            // Validate equipment, costume.
            avatarState.ValidateEquipmentsV2(EquipmentIds, context.BlockIndex);
            avatarState.ValidateConsumable(FoodIds, context.BlockIndex);
            avatarState.ValidateCostume(CostumeIds);
            // Simulate.
            int score = 10_000;
            int cp = CPHelper.GetCPV2(avatarState, sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            raiderState.Update(avatarState, cp, score, PayNcg);
            // Reward.
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
