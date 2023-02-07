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
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData.Crystal;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Created at https://github.com/planetarium/lib9c/pull/1031
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash_random_buff")]
    public class HackAndSlashRandomBuff : GameAction, IHackAndSlashRandomBuffV1
    {
        public const int NormalGachaCount = 5;
        public const int AdvancedGachaCount = 10;

        public Address AvatarAddress;
        public bool AdvancedGacha;

        Address IHackAndSlashRandomBuffV1.AvatarAddress => AvatarAddress;
        bool IHackAndSlashRandomBuffV1.AdvancedGacha => AdvancedGacha;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
            => new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["adv"] = AdvancedGacha.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            AdvancedGacha = plainValue["adv"].ToBoolean();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var gachaStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(AvatarAddress);
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashRandomBuff exec started", addressesHex);

            // Invalid Avatar address, or does not have GachaState.
            if (!states.TryGetState(gachaStateAddress, out List rawGachaState))
            {
                throw new FailedLoadStateException(
                    $"Can't find {nameof(CrystalRandomSkillState)}. Gacha state address:{gachaStateAddress}");
            }

            var gachaState = new CrystalRandomSkillState(gachaStateAddress, rawGachaState);
            var stageBuffSheet = states.GetSheet<CrystalStageBuffGachaSheet>();

            // Insufficient gathered star.
            if (gachaState.StarCount < stageBuffSheet[gachaState.StageId].MaxStar)
            {
                throw new NotEnoughStarException(
                    $"Not enough gathered stars. Need : {stageBuffSheet[gachaState.StageId].MaxStar}, own : {gachaState.StarCount}");
            }

            var cost =
                CrystalCalculator.CalculateBuffGachaCost(gachaState.StageId,
                    AdvancedGacha,
                    stageBuffSheet);
            var balance = states.GetBalance(context.Signer, cost.Currency);

            // Insufficient CRYSTAL.
            if (balance < cost)
            {
                throw new NotEnoughFungibleAssetValueException(
                    $"{nameof(HackAndSlashRandomBuff)} required {cost}, but balance is {balance}");
            }

            var buffSelector = new WeightedSelector<int>(context.Random);
            var buffSheet = states.GetSheet<CrystalRandomBuffSheet>();
            foreach (var buffRow in buffSheet.Values)
            {
                buffSelector.Add(buffRow.Id, buffRow.Ratio);
            }

            var gachaCount = AdvancedGacha ? AdvancedGachaCount : NormalGachaCount;
            var buffIds = buffSelector.Select(gachaCount - 1).ToList();
            var needPitySystem = IsPitySystemNeeded(buffIds, gachaCount, buffSheet);
            if (needPitySystem)
            {
                var newBuffSelector = new WeightedSelector<int>(context.Random);
                var minimumRank = AdvancedGacha
                    ? CrystalRandomBuffSheet.Row.BuffRank.S
                    : CrystalRandomBuffSheet.Row.BuffRank.A;
                foreach (var buffRow in buffSheet.Values.Where(row => row.Rank <= minimumRank))
                {
                    newBuffSelector.Add(buffRow.Key, buffRow.Ratio);
                }
                buffIds.Add(newBuffSelector.Select(1).First());
            }
            else
            {
                buffIds.Add(buffSelector.Select(1).First());
            }

            gachaState.Update(buffIds);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashRandomBuff Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(gachaStateAddress, gachaState.Serialize())
                .TransferAsset(context.Signer, Addresses.StageRandomBuff, cost);
        }

        private static bool IsPitySystemNeeded(IEnumerable<int> buffIds, int gachaCount, CrystalRandomBuffSheet sheet)
        {
            return gachaCount switch
            {
                NormalGachaCount => buffIds.All(i =>
                    sheet[i].Rank > CrystalRandomBuffSheet.Row.BuffRank.A),
                AdvancedGachaCount => buffIds.All(i =>
                    sheet[i].Rank > CrystalRandomBuffSheet.Row.BuffRank.S),
                _ => false
            };
        }
    }
}
