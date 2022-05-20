using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("hack_and_slash_random_buff")]
    public class HackAndSlashRandomBuff : GameAction
    {
        public enum BuffRank
        {
            SS = 1,
            S = 2,
            A = 3,
            B = 4,
        }

        public const int MinimumGachaCount = 5;
        public const int MaximumGachaCount = 10;

        public Address AvatarAddress;
        public int GachaCount;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
            => new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = GachaCount.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            GachaCount = plainValue["c"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var gachaStateAddress = AvatarAddress.Derive(Lib9c.SerializeKeys.HackAndSlashBuffStateKey);

            // Invalid Avatar address, or does not have GachaState.
            if (!states.TryGetState(gachaStateAddress, out List rawGachaState))
            {
                throw new FailedLoadStateException(
                    $"Can't find {nameof(HackAndSlashBuffState)}. Gacha state address:{gachaStateAddress}");
            }

            var gachaState = new HackAndSlashBuffState(rawGachaState);
            var stageBuffSheet = states.GetSheet<CrystalStageBuffGachaSheet>();

            // Insufficient gathered star.
            if (gachaState.StarCount < stageBuffSheet[gachaState.StageId].MaxStar)
            {
                throw new NotEnoughGatheredStarException(
                    $"Not enough gathered stars. Need : {stageBuffSheet[gachaState.StageId].MaxStar}, own : {gachaState.StarCount}");
            }

            // Invalid Gacha count.
            if (GachaCount != MinimumGachaCount && GachaCount != MaximumGachaCount)
            {
                throw new InvalidGachaCountException(
                    $"Gacha count must equal with {MinimumGachaCount} or {MaximumGachaCount}. input count : {GachaCount}");
            }

            var cost =
                CrystalCalculator.CalculateBuffGachaCost(gachaState.StageId,
                    GachaCount,
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
                buffSelector.Add(buffRow.BuffId, buffRow.Ratio);
            }

            var buffIds = buffSelector.Select(GachaCount - 1).ToList();
            var needPitySystem = IsNeedPitySystem(buffIds, GachaCount, buffSheet);
            if (needPitySystem)
            {
                var newBuffSelector = new WeightedSelector<int>(context.Random);
                var minimumRank = GachaCount == MinimumGachaCount ? BuffRank.A : BuffRank.S;
                foreach (var buffRow in buffSheet.Values.Where(row => row.Rank <= (int) minimumRank))
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

            return states
                .SetState(gachaStateAddress, gachaState.Serialize())
                .TransferAsset(context.Signer, Addresses.HasRandomBuffGacha, cost);
        }

        private static bool IsNeedPitySystem(IEnumerable<int> buffIds, int gachaCount, CrystalRandomBuffSheet sheet)
        {
            switch (gachaCount)
            {
                case MinimumGachaCount:
                    return buffIds.All(i => sheet[i].Rank > (int) BuffRank.A);
                case MaximumGachaCount:
                    return buffIds.All(i => sheet[i].Rank > (int) BuffRank.S);
            }

            return false;
        }
    }
}
