using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public class ClaimStakeReward : GameAction
    {
        internal Address AvatarAddress { get; private set; }

        public ClaimStakeReward(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward() : base()
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
                typeof(StakeAchievementRewardSheet),
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            });

            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            var stakeAchievementRewardSheet = sheets.GetSheet<StakeAchievementRewardSheet>();

            var currency = states.GetGoldCurrency();
            var balance = states.GetBalance(context.Signer, currency);

            if (!stakeState.IsClaimable(context.BlockIndex))
            {
                throw new RequiredBlockIndexException();
            }

            var avatarState = states.GetAvatarState(AvatarAddress);
            int level = stakeRegularRewardSheet.FindLevelByStakedAmount(balance);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            ItemSheet itemSheet = sheets.GetItemSheet();
            foreach (var reward in rewards)
            {
                var (quantity, _) = balance.DivRem(currency * reward.Rate);
                ItemSheet.Row row = itemSheet[reward.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, (int) quantity);
            }

            int achievementRewardLevel = stakeAchievementRewardSheet.FindLevel(balance);
            int achievementRewardStep = stakeAchievementRewardSheet.FindStep(
                achievementRewardLevel,
                context.BlockIndex - stakeState.StartedBlockIndex);
            for (int i = 0; i < achievementRewardStep; ++i)
            {
                if (!stakeState.Achievements.Check(achievementRewardLevel, i))
                {
                    var step = stakeAchievementRewardSheet[achievementRewardLevel].Steps[i];
                    foreach (var reward in step.Rewards)
                    {
                        ItemSheet.Row row = itemSheet[reward.ItemId];
                        ItemBase item = row is MaterialItemSheet.Row materialRow
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateItem(row, context.Random);
                        avatarState.inventory.AddItem(item, reward.Quantity);
                    }
                }
            }

            stakeState.Achievements.Achieve(achievementRewardLevel, achievementRewardStep);
            stakeState.Claim(context.BlockIndex);

            return states.SetState(stakeState.address, stakeState.Serialize())
                .SetState(avatarState.address, avatarState.Serialize());
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
