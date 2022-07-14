using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("raid")]
    public class Raid : GameAction
    {
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public List<Guid> CostumeIds;
        public int RaidId;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            Address worldBossAddress = Addresses.GetWorldBossAddress(RaidId);
            Address raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, RaidId);
            Address raidersAddress = Addresses.GetRaidersAddress(RaidId);
            if (context.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldBossAddress, MarkChanged)
                    .SetState(raiderAddress, MarkChanged)
                    .SetState(raidersAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, worldBossAddress);
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress,
                    out AvatarState avatarState,
                    out bool migrationRequired))
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
            // Validate equipment, costume.
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
                states = states
                    .SetState(raidersAddress, new List(raiders.Select(a => a.Serialize())))
                    .TransferAsset(context.Signer, worldBossAddress,
                    300 * CrystalCalculator.CRYSTAL);
            }

            if (raiderState.RemainChallengeCount < 1)
            {
                // TODO Charge challenge count by interval or NCG.
                raiderState.RemainChallengeCount = 3;
            }

            int score = 10_000;
            if (raiderState.HighScore < score)
            {
                raiderState.HighScore = score;
            }

            raiderState.TotalScore += score;
            raiderState.RemainChallengeCount--;
            raiderState.TotalChallengeCount++;
            // Simulate.
            // Reward.
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new [] {
                typeof(WorldBossListSheet),
            });
            WorldBossState bossState;
            if (states.TryGetState(worldBossAddress, out List rawBossState))
            {
                bossState = new WorldBossState(rawBossState);
            }
            else
            {
                var bossListSheet = sheets.GetSheet<WorldBossListSheet>();
                var row = bossListSheet.OrderedList
                    .First(r =>
                        r.StartedBlockIndex > context.BlockIndex &&
                        context.BlockIndex <= r.EndedBlockIndex
                    );
                bossState = new WorldBossState(row);
            }
            bossState.CurrentHP -= score;
            if (bossState.CurrentHP <= 0)
            {
                bossState.Level++;
                bossState.CurrentHP = 20_000 + Math.Max(bossState.Level - 1, 1) * 10_000;
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
                    ["r"] = RaidId.Serialize()
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
            CostumeIds = plainValue["c"].ToList(StateExtensions.ToGuid);
            RaidId = plainValue["r"].ToInteger();
        }
    }
}
