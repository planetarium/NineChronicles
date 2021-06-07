using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("claim_monster_collection_reward2")]
    public class ClaimMonsterCollectionReward : GameAction
    {
        public Address avatarAddress;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, 0);
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(collectionAddress, MarkChanged);
            }

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            MonsterCollectionState monsterCollectionState = new MonsterCollectionState(stateDict);

            if (!monsterCollectionState.CanReceive(context.BlockIndex))
            {
                throw new RequiredBlockIndexException(
                    $"{collectionAddress} is not available yet; it will be available after {Math.Max(monsterCollectionState.StartedBlockIndex, monsterCollectionState.ReceivedBlockIndex) + MonsterCollectionState.RewardInterval}");
            }

            monsterCollectionState.Receive(context.BlockIndex);
            int rewardLevel = (int) ((context.BlockIndex - monsterCollectionState.ReceivedBlockIndex) /
                                     MonsterCollectionState.RewardInterval);
            ItemSheet itemSheet = states.GetItemSheet();
            MonsterCollectionRewardSheet monsterCollectionRewardSheet = states.GetSheet<MonsterCollectionRewardSheet>();
            int level = monsterCollectionState.Level;
            List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos = monsterCollectionRewardSheet[level].Rewards;
            Dictionary<int, int> map = new Dictionary<int, int>();
            foreach (var rewardInfo in rewardInfos)
            {
                int itemId = rewardInfo.ItemId;
                int quantity = rewardInfo.Quantity * rewardLevel;
                if (map.ContainsKey(itemId))
                {
                    map[itemId] += quantity;
                }
                else
                {
                    map[itemId] = quantity;
                }
            }

            List<MonsterCollectionRewardSheet.RewardInfo> rewards = map
                .OrderBy(i => i.Key)
                .Select(i =>
                    new MonsterCollectionRewardSheet.RewardInfo(i.Key, i.Value)
                )
                .ToList();
            Guid id = context.Random.GenerateRandomGuid();
            MonsterCollectionResult result = new MonsterCollectionResult(id, avatarAddress, rewards);
            MonsterCollectionMail mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
            avatarState.UpdateV3(mail);
            foreach (var rewardInfo in rewards)
            {
                var row = itemSheet[rewardInfo.ItemId];
                var item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, rewardInfo.Quantity);
            }

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(collectionAddress, monsterCollectionState.SerializeV2());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [AvatarAddressKey] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
