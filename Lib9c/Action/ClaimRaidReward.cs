using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("claim_raid_reward")]
    public class ClaimRaidReward: GameAction, IClaimRaidRewardV1
    {
        public Address AvatarAddress;

        Address IClaimRaidRewardV1.AvatarAddress => AvatarAddress;

        public ClaimRaidReward()
        {
        }

        public ClaimRaidReward(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimRaidReward exec started", addressesHex);
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[] {
                typeof(RuneWeightSheet),
                typeof(WorldBossRankRewardSheet),
                typeof(WorldBossCharacterSheet),
                typeof(WorldBossListSheet),
                typeof(RuneSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            int raidId;
            try
            {
                raidId = worldBossListSheet.FindRaidIdByBlockIndex(context.BlockIndex);
            }
            catch (InvalidOperationException)
            {
                // Find Latest raidId.
                raidId = worldBossListSheet.FindPreviousRaidIdByBlockIndex(context.BlockIndex);
            }
            var row = sheets.GetSheet<WorldBossListSheet>().Values.First(r => r.Id == raidId);
            var bossRow = sheets.GetSheet<WorldBossCharacterSheet>().Values.First(x => x.BossId == row.BossId);
            var raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            RaiderState raiderState = states.GetRaiderState(raiderAddress);
            int rank = WorldBossHelper.CalculateRank(bossRow, raiderState.HighScore);
            if (raiderState.LatestRewardRank < rank)
            {
                for (int i = raiderState.LatestRewardRank; i < rank; i++)
                {
                    List<FungibleAssetValue> rewards = RuneHelper.CalculateReward(
                        i + 1,
                        row.BossId,
                        sheets.GetSheet<RuneWeightSheet>(),
                        sheets.GetSheet<WorldBossRankRewardSheet>(),
                        sheets.GetSheet<RuneSheet>(),
                        context.Random
                    );

                    foreach (var reward in rewards)
                    {
                        if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                        {
                            states = states.MintAsset(context.Signer, reward);
                        }
                        else
                        {
                            states = states.MintAsset(AvatarAddress, reward);
                        }
                    }
                }

                raiderState.LatestRewardRank = rank;
                raiderState.ClaimedBlockIndex = context.BlockIndex;
                states = states.SetState(raiderAddress, raiderState.Serialize());
                var ended = DateTimeOffset.UtcNow;
                Log.Debug("{AddressesHex}ClaimRaidReward Total Executed Time: {Elapsed}", addressesHex, ended - started);
                return states;
            }

            throw new NotEnoughRankException();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
