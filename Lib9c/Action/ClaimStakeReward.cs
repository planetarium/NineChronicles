using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
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
        private const string ActionTypeText = "claim_stake_reward5";

        /// <summary>
        /// This is the version 1 of the stake reward sheet.
        /// The version 1 is used for calculating the reward for the stake
        /// that is accumulated before the table patch.
        /// </summary>
        public static class V1
        {
            public const int MaxLevel = 5;

            public const string StakeRegularRewardSheetCsv =
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

            public const string StakeRegularFixedRewardSheetCsv =
                @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2";

            private static StakeRegularRewardSheet _stakeRegularRewardSheet;
            private static StakeRegularFixedRewardSheet _stakeRegularFixedRewardSheet;

            public static StakeRegularRewardSheet StakeRegularRewardSheet
            {
                get
                {
                    if (_stakeRegularRewardSheet is null)
                    {
                        _stakeRegularRewardSheet = new StakeRegularRewardSheet();
                        _stakeRegularRewardSheet.Set(StakeRegularRewardSheetCsv);
                    }

                    return _stakeRegularRewardSheet;
                }
            }

            public static StakeRegularFixedRewardSheet StakeRegularFixedRewardSheet
            {
                get
                {
                    if (_stakeRegularFixedRewardSheet is null)
                    {
                        _stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                        _stakeRegularFixedRewardSheet.Set(StakeRegularFixedRewardSheetCsv);
                    }

                    return _stakeRegularFixedRewardSheet;
                }
            }
        }

        // NOTE: Use this when the <see cref="StakeRegularFixedRewardSheet"/> or
        // <see cref="StakeRegularRewardSheet"/> is patched.
        // public static class V2
        // {
        // }

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward(Address avatarAddress) : this()
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward()
        {
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!states.TryGetStakeState(context.Signer, out var stakeState))
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
            var level =
                stakeRegularRewardSheet.FindLevelByStakedAmount(context.Signer, stakedAmount);
            var itemSheet = sheets.GetItemSheet();
            stakeState.CalculateAccumulatedItemRewards(
                context.BlockIndex,
                out var itemV1Step,
                out var itemV2Step);
            stakeState.CalculateAccumulatedRuneRewards(
                context.BlockIndex,
                out var runeV1Step,
                out var runeV2Step);
            stakeState.CalculateAccumulatedCurrencyRewards(
                context.BlockIndex,
                out var currencyV1Step,
                out var currencyV2Step);
            if (itemV1Step > 0)
            {
                var v1Level = Math.Min(level, V1.MaxLevel);
                var fixedRewardV1 = V1.StakeRegularFixedRewardSheet[v1Level].Rewards;
                var regularRewardV1 = V1.StakeRegularRewardSheet[v1Level].Rewards;
                states = ProcessReward(
                    context,
                    states,
                    ref avatarState,
                    itemSheet,
                    stakedAmount,
                    itemV1Step,
                    runeV1Step,
                    currencyV1Step,
                    fixedRewardV1,
                    regularRewardV1);
            }

            if (itemV2Step > 0)
            {
                var regularFixedReward =
                    states.TryGetSheet<StakeRegularFixedRewardSheet>(out var fixedRewardSheet)
                        ? fixedRewardSheet[level].Rewards
                        : new List<StakeRegularFixedRewardSheet.RewardInfo>();
                var regularReward = sheets.GetSheet<StakeRegularRewardSheet>()[level].Rewards;
                states = ProcessReward(
                    context,
                    states,
                    ref avatarState,
                    itemSheet,
                    stakedAmount,
                    itemV2Step,
                    runeV2Step,
                    currencyV2Step,
                    regularFixedReward,
                    regularReward);
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

        private IAccountStateDelta ProcessReward(
            IActionContext context,
            IAccountStateDelta states,
            ref AvatarState avatarState,
            ItemSheet itemSheet,
            FungibleAssetValue stakedAmount,
            int itemRewardStep,
            int runeRewardStep,
            int currencyRewardStep,
            List<StakeRegularFixedRewardSheet.RewardInfo> fixedReward,
            List<StakeRegularRewardSheet.RewardInfo> regularReward)
        {
            var stakedCurrency = stakedAmount.Currency;

            // Regular Reward
            foreach (var reward in regularReward)
            {
                switch (reward.Type)
                {
                    case StakeRegularRewardSheet.StakeRewardType.Item:
                        var (quantity, _) = stakedAmount.DivRem(stakedCurrency * reward.Rate);
                        if (quantity < 1)
                        {
                            // If the quantity is zero, it doesn't add the item into inventory.
                            continue;
                        }

                        ItemSheet.Row row = itemSheet[reward.ItemId];
                        ItemBase item = row is MaterialItemSheet.Row materialRow
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateItem(row, context.Random);
                        avatarState.inventory.AddItem(item, (int)quantity * itemRewardStep);
                        break;
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                        var runeReward = runeRewardStep *
                                         RuneHelper.CalculateStakeReward(stakedAmount, reward.Rate);
                        if (runeReward < 1 * RuneHelper.StakeRune)
                        {
                            continue;
                        }

                        states = states.MintAsset(context, AvatarAddress, runeReward);
                        break;
                    case StakeRegularRewardSheet.StakeRewardType.Currency:
                        if (string.IsNullOrEmpty(reward.CurrencyTicker))
                        {
                            throw new NullReferenceException("currency ticker is null or empty");
                        }

                        var rewardCurrency =
                            Currencies.GetMinterlessCurrency(reward.CurrencyTicker);
                        var rewardCurrencyQuantity =
                            stakedAmount.DivRem(reward.Rate * stakedAmount.Currency).Quotient;
                        if (rewardCurrencyQuantity <= 0)
                        {
                            continue;
                        }

                        states = states.MintAsset(
                            context,
                            context.Signer,
                            rewardCurrencyQuantity * currencyRewardStep * rewardCurrency);
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
                avatarState.inventory.AddItem(item, reward.Count * itemRewardStep);
            }

            return states;
        }
    }
}
