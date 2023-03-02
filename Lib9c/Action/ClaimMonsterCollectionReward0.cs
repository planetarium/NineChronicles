using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("claim_monster_collection_reward")]
    public class ClaimMonsterCollectionReward0 : GameAction, IClaimMonsterCollectionRewardV1
    {
        public Address avatarAddress;
        public int collectionRound;

        Address IClaimMonsterCollectionRewardV1.AvatarAddress => avatarAddress;
        int IClaimMonsterCollectionRewardV1.CollectionRound => collectionRound;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(context.Signer, collectionRound);

            if (context.Rehearsal)
            {
                return states
                    .SetState(context.Signer, MarkChanged)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(collectionAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(stateDict);
            if (monsterCollectionState.End)
            {
                throw new MonsterCollectionExpiredException($"{collectionAddress} has already expired on {monsterCollectionState.ExpiredBlockIndex}");
            }

            if (!monsterCollectionState.CanReceive(context.BlockIndex))
            {
                throw new RequiredBlockIndexException(
                    $"{collectionAddress} is not available yet; it will be available after {Math.Max(monsterCollectionState.StartedBlockIndex, monsterCollectionState.ReceivedBlockIndex) + MonsterCollectionState0.RewardInterval}");
            }

            long rewardLevel = monsterCollectionState.GetRewardLevel(context.BlockIndex);
            ItemSheet itemSheet = states.GetItemSheet();
            for (int i = 0; i < rewardLevel; i++)
            {
                int level = i + 1;
                if (level <= monsterCollectionState.RewardLevel)
                {
                    continue;
                }

                List<MonsterCollectionRewardSheet.RewardInfo> rewards = monsterCollectionState.RewardLevelMap[level];
                Guid id = context.Random.GenerateRandomGuid();
                MonsterCollectionResult result = new MonsterCollectionResult(id, avatarAddress, rewards);
                MonsterCollectionMail mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
                avatarState.Update(mail);
                foreach (var rewardInfo in rewards)
                {
                    var row = itemSheet[rewardInfo.ItemId];
                    var item = row is MaterialItemSheet.Row materialRow
                        ? ItemFactory.CreateTradableMaterial(materialRow)
                        : ItemFactory.CreateItem(row, context.Random);
                    avatarState.inventory.AddItem2(item, rewardInfo.Quantity);
                }
                monsterCollectionState.UpdateRewardMap(level, result, context.BlockIndex);
            }

            // Return gold at the end of monster collect.
            if (rewardLevel == 4)
            {
                MonsterCollectionSheet monsterCollectionSheet = states.GetSheet<MonsterCollectionSheet>();
                Currency currency = states.GetGoldCurrency();
                // Set default gold value.
                FungibleAssetValue gold = currency * 0;
                for (int i = 0; i < monsterCollectionState.Level; i++)
                {
                    int level = i + 1;
                    gold += currency * monsterCollectionSheet[level].RequiredGold;
                }
                agentState.IncreaseCollectionRound();
                states = states.SetState(context.Signer, agentState.Serialize());
                if (gold > currency * 0)
                {
                    states = states.TransferAsset(collectionAddress, context.Signer, gold);
                }
            }

            return states
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(collectionAddress, monsterCollectionState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [AvatarAddressKey] = avatarAddress.Serialize(),
            [MonsterCollectionRoundKey] = collectionRound.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            collectionRound = plainValue[MonsterCollectionRoundKey].ToInteger();
        }
    }
}
