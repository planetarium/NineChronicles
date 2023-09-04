using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
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
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2097
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimStakeReward : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        private const string ActionTypeText = "claim_stake_reward9";

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

        // StakeState -> StakeStateV2 migration (refactoring?)
        // StakeStateV2.Contract 기준으로 시트 가져와서 보상 주기
        // StakeStateV2.ClaimedBlockIndex를 셀프로 액션에서 직접 넣어주니까 잘 넣어줘야...
        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var stakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var ncg = states.GetGoldCurrency();
            var stakedAmount = states.GetBalance(stakeStateAddress, ncg);
            if (!states.TryGetStakeStateV2(context.Signer, out var stakeStateV2))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(StakeState),
                    stakeStateAddress);
            }

            if (stakeStateV2.ClaimableBlockIndex > context.BlockIndex)
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

            var sheets = states.GetSheets(sheetTuples: new[]
            {
                (
                    typeof(StakeRegularFixedRewardSheet),
                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName
                ),
                (
                    typeof(StakeRegularRewardSheet),
                    stakeStateV2.Contract.StakeRegularRewardSheetTableName
                ),
                (typeof(ConsumableItemSheet), nameof(ConsumableItemSheet)),
                (typeof(CostumeItemSheet), nameof(CostumeItemSheet)),
                (typeof(EquipmentItemSheet), nameof(EquipmentItemSheet)),
                (typeof(MaterialItemSheet), nameof(MaterialItemSheet)),
            });
            var stakeRegularFixedRewardSheet = sheets.GetSheet<StakeRegularFixedRewardSheet>();
            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            // NOTE:
            var stakingLevel = Math.Min(
                stakeRegularRewardSheet.FindLevelByStakedAmount(
                    context.Signer,
                    stakedAmount),
                stakeRegularRewardSheet.Keys.Max());
            var itemSheet = sheets.GetItemSheet();
            // The first reward is given at the claimable block index.
            var rewardSteps = 1 + (int)Math.DivRem(
                context.BlockIndex - stakeStateV2.ClaimableBlockIndex,
                StakeState.RewardInterval,
                out _);

            // Fixed Reward
            foreach (var reward in stakeRegularFixedRewardSheet[stakingLevel].Rewards)
            {
                var itemRow = itemSheet[reward.ItemId];
                var item = itemRow is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(itemRow, context.Random);
                avatarState.inventory.AddItem(item, reward.Count * rewardSteps);
            }

            // Regular Reward
            foreach (var reward in stakeRegularRewardSheet[stakingLevel].Rewards)
            {
                var rateFav = FungibleAssetValue.Parse(
                    stakedAmount.Currency,
                    reward.DecimalRate.ToString(CultureInfo.InvariantCulture));
                var rewardQuantityForSingleStep = stakedAmount.DivRem(rateFav, out _);
                if (rewardQuantityForSingleStep <= 0)
                {
                    continue;
                }

                switch (reward.Type)
                {
                    case StakeRegularRewardSheet.StakeRewardType.Item:
                    {
                        var majorUnit = (int)rewardQuantityForSingleStep * rewardSteps;
                        if (majorUnit < 1)
                        {
                            continue;
                        }

                        var itemRow = itemSheet[reward.ItemId];
                        var item = itemRow is MaterialItemSheet.Row materialRow
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateItem(itemRow, context.Random);

                        avatarState.inventory.AddItem(item, majorUnit);
                        break;
                    }
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                    {
                        var majorUnit = rewardQuantityForSingleStep * rewardSteps;
                        if (majorUnit < 1)
                        {
                            continue;
                        }

                        var runeReward = RuneHelper.StakeRune * majorUnit;
                        states = states.MintAsset(context, AvatarAddress, runeReward);
                        break;
                    }
                    case StakeRegularRewardSheet.StakeRewardType.Currency:
                    {
                        if (string.IsNullOrEmpty(reward.CurrencyTicker))
                        {
                            throw new NullReferenceException("currency ticker is null or empty");
                        }

                        // NOTE: prepare reward currency.
                        Currency rewardCurrency;
                        // NOTE: this line covers the reward.CurrencyTicker is following cases:
                        //       - Currencies.Crystal.Ticker
                        //       - Currencies.Garage.Ticker
                        //       - lower case is starting with "rune_" or "runestone_"
                        //       - lower case is starting with "soulstone_"
                        try
                        {
                            rewardCurrency =
                                Currencies.GetMinterlessCurrency(reward.CurrencyTicker);
                        }
                        // NOTE: throw exception if reward.CurrencyTicker is null or empty.
                        catch (ArgumentNullException)
                        {
                            throw;
                        }
                        // NOTE: handle the case that reward.CurrencyTicker isn't covered by
                        //       Currencies.GetMinterlessCurrency().
                        catch (ArgumentException)
                        {
                            // NOTE: throw exception if reward.CurrencyDecimalPlaces is null.
                            if (reward.CurrencyDecimalPlaces is null)
                            {
                                throw new ArgumentException(
                                    $"Decimal places of {reward.CurrencyTicker} is null");
                            }

                            // NOTE: new currency is created as uncapped currency.
                            rewardCurrency = Currency.Uncapped(
                                reward.CurrencyTicker,
                                Convert.ToByte(reward.CurrencyDecimalPlaces.Value),
                                minters: null);
                        }

                        var majorUnit = rewardQuantityForSingleStep * rewardSteps;
                        var rewardFav = rewardCurrency * majorUnit;
                        states = states.MintAsset(
                            context,
                            context.Signer,
                            rewardFav);
                        break;
                    }
                    default:
                        throw new ArgumentException($"Can't handle reward type: {reward.Type}");
                }
            }

            // NOTE: update claimed block index.
            stakeStateV2 = new StakeStateV2(
                stakeStateV2.Contract,
                stakeStateV2.StartedBlockIndex,
                context.BlockIndex);

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
                .SetState(stakeStateAddress, stakeStateV2.Serialize())
                .SetState(
                    avatarState.address.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
        }
    }
}
