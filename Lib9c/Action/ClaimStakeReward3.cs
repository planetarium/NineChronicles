using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1371
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimStakeReward3 : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        private const string ActionTypeText = "claim_stake_reward3";

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward3(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward3()
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            CheckActionAvailable(ClaimStakeReward.ObsoletedIndex, context);
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(StakeState),
                    StakeState.DeriveAddress(context.Signer));
            }

            if (!stakeState.IsClaimable(context.BlockIndex))
            {
                throw new RequiredBlockIndexException(
                    ActionTypeText,
                    addressesHex,
                    context.BlockIndex);
            }

            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            var sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(StakeRegularRewardSheet),
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            });

            var currency = states.GetGoldCurrency();
            var stakedAmount = states.GetBalance(stakeState.address, currency);
            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            int level =
                stakeRegularRewardSheet.FindLevelByStakedAmount(context.Signer, stakedAmount);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            ItemSheet itemSheet = sheets.GetItemSheet();
            var accumulatedRewards = stakeState.CalculateAccumulatedRewards(context.BlockIndex);
            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case StakeRegularRewardSheet.StakeRewardType.Item:
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
                        avatarState.inventory.AddItem(item, (int)quantity * accumulatedRewards);
                        break;
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                        var accumulatedRuneRewards =
                            stakeState.CalculateAccumulatedRuneRewards(context.BlockIndex);
                        var runeReward = accumulatedRuneRewards * RuneHelper.CalculateStakeReward(stakedAmount, reward.Rate);
                        if (runeReward < 1 * RuneHelper.StakeRune)
                        {
                            continue;
                        }
                        states = states.MintAsset(AvatarAddress, runeReward);
                        break;
                    default:
                        break;
                }
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

            if (migrationRequired)
            {
                states = states
                    .SetState(avatarState.address, avatarState.SerializeV2())
                    .SetState(
                        avatarState.address.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        avatarState.address.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            return states
                .SetState(stakeState.address, stakeState.Serialize())
                .SetState(
                    avatarState.address.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
