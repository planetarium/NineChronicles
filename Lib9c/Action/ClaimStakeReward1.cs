using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("claim_stake_reward")]
    public class ClaimStakeReward1 : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward1(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward1() : base()
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                throw new FailedLoadStateException(nameof(StakeState));
            }

            var sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(StakeRegularRewardSheet),
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            });

            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();

            var currency = states.GetGoldCurrency();
            var stakedAmount = states.GetBalance(stakeState.address, currency);

            if (!stakeState.IsClaimable(context.BlockIndex))
            {
                throw new RequiredBlockIndexException();
            }

            var avatarState = states.GetAvatarStateV2(AvatarAddress);
            int level = stakeRegularRewardSheet.FindLevelByStakedAmount(context.Signer, stakedAmount);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            ItemSheet itemSheet = sheets.GetItemSheet();
            var accumulatedRewards = stakeState.CalculateAccumulatedRewards(context.BlockIndex);
            foreach (var reward in rewards)
            {
                var (quantity, _) = stakedAmount.DivRem(currency * reward.Rate);
                if (quantity < 1)
                {
                    // If the quantity is zero, it doesn't add the item into inventory.
                    continue;
                }

                ItemSheet.Row row = itemSheet[reward.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, (int) quantity * accumulatedRewards);
            }

            if (states.TryGetSheet<StakeRegularFixedRewardSheet>(
                    out var stakeRegularFixedRewardSheet))
            {
                var fixedRewards = stakeRegularFixedRewardSheet[level].Rewards;
                foreach (var reward in fixedRewards)
                {
                    ItemSheet.Row row = itemSheet[reward.ItemId];
                    ItemBase item = row is MaterialItemSheet.Row materialRow
                        ? ItemFactory.CreateTradableMaterial(materialRow)
                        : ItemFactory.CreateItem(row, context.Random);
                    avatarState.inventory.AddItem(item, reward.Count * accumulatedRewards);
                }
            }

            stakeState.Claim(context.BlockIndex);
            return states.SetState(stakeState.address, stakeState.Serialize())
                .SetState(avatarState.address, avatarState.SerializeV2())
                .SetState(
                    avatarState.address.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize());

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
