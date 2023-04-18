using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1458
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimStakeReward : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        private const string ActionTypeText = "claim_stake_reward3";

        private const string StakeRegularRewardSheetV1Data =
            @"level,required_gold,item_id,rate,type
1,50,400000,10,Item
1,50,500000,800,Item
1,50,20001,6000,Rune
2,500,400000,8,Item
2,500,500000,800,Item
2,500,20001,6000,Rune
3,5000,400000,5,Item
3,5000,500000,800,Item
3,5000,20001,6000,Rune
4,50000,400000,5,Item
4,50000,500000,800,Item
4,50000,20001,6000,Rune
5,500000,400000,5,Item
5,500000,500000,800,Item
5,500000,20001,6000,Rune";

        private const string StakeRegularFixedRewardSheetV1Data =
            @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2";

        private readonly ImmutableSortedDictionary<string,
                ImmutableSortedDictionary<int, IStakeRewardSheet>>
            _stakeRewardHistoryDict;

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward(Address avatarAddress) : this()
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward()
        {
            var regularRewardSheetV1 = new StakeRegularRewardSheet();
            regularRewardSheetV1.Set(StakeRegularRewardSheetV1Data);
            var fixedRewardSheetV1 = new StakeRegularFixedRewardSheet();
            fixedRewardSheetV1.Set(StakeRegularFixedRewardSheetV1Data);
            _stakeRewardHistoryDict =
                new Dictionary<string, ImmutableSortedDictionary<int, IStakeRewardSheet>>
                {
                    {
                        "StakeRegularRewardSheet", new Dictionary<int, IStakeRewardSheet>
                        {
                            { 1, regularRewardSheetV1 },
                        }.ToImmutableSortedDictionary()
                    },
                    {
                        "StakeRegularFixedRewardSheet",
                        new Dictionary<int, IStakeRewardSheet>
                        {
                            { 1, fixedRewardSheetV1 }
                        }.ToImmutableSortedDictionary()
                    },
                }.ToImmutableSortedDictionary();
        }

        private IAccountStateDelta ProcessReward(IActionContext context, IAccountStateDelta states,
            ref AvatarState avatarState,
            ItemSheet itemSheet, FungibleAssetValue stakedAmount,
            int rewardStep, int runeRewardStep,
            List<StakeRegularFixedRewardSheet.RewardInfo> fixedReward,
            List<StakeRegularRewardSheet.RewardInfo> regularReward)
        {
            var currency = stakedAmount.Currency;

            // Regular Reward
            foreach (var reward in regularReward)
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
                        avatarState.inventory.AddItem(item, (int)quantity * rewardStep);
                        break;
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                        var runeReward = runeRewardStep *
                                         RuneHelper.CalculateStakeReward(stakedAmount, reward.Rate);
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

            // Fixed Reward
            foreach (var reward in fixedReward)
            {
                ItemSheet.Row row = itemSheet[reward.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, reward.Count * rewardStep);
            }

            return states;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            CheckActionAvailable(ClaimStakeReward2.ObsoletedIndex, context);
            // TODO: Uncomment this when new version of action is created
            // CheckObsolete(ObsoletedIndex, context);
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(StakeState),
                    StakeState.DeriveAddress(context.Signer));
            }

            if (!stakeState.IsClaimable(context.BlockIndex, out _, out _))
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
            var accumulatedRewards =
                stakeState.CalculateAccumulatedRewards(context.BlockIndex, out var v1Step,
                    out var v2Step);
            var accumulatedRuneRewards =
                stakeState.CalculateAccumulatedRuneRewards(context.BlockIndex, out var runeV1Step,
                    out var runeV2Step);
            if (v1Step > 0)
            {
                var fixedReward =
                    ((StakeRegularFixedRewardSheet)_stakeRewardHistoryDict[
                        "StakeRegularFixedRewardSheet"][1])
                    [level].Rewards;
                var regularReward =
                    ((StakeRegularRewardSheet)_stakeRewardHistoryDict["StakeRegularRewardSheet"][1])
                    [level].Rewards;
                states = ProcessReward(context, states, ref avatarState, itemSheet,
                    stakedAmount, v1Step, runeV1Step, fixedReward, regularReward);
            }

            if (v2Step > 0)
            {
                var fixedReward =
                    states.TryGetSheet<StakeRegularFixedRewardSheet>(out var fixedRewardSheet)
                        ? fixedRewardSheet[level].Rewards
                        : new List<StakeRegularFixedRewardSheet.RewardInfo>();
                var regularReward = sheets.GetSheet<StakeRegularRewardSheet>()[level].Rewards;
                states = ProcessReward(context, states, ref avatarState, itemSheet,
                    stakedAmount, v2Step, runeV2Step, fixedReward, regularReward);
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
