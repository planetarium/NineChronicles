using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
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
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("claim_monster_collection_reward2")]
    public class ClaimMonsterCollectionReward2 : GameAction, IClaimMonsterCollectionRewardV2
    {
        public Address avatarAddress;

        Address IClaimMonsterCollectionRewardV2.AvatarAddress => avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            Address worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            Address questListAddress = avatarAddress.Derive(LegacyQuestListKey);

            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 0), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 1), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 2), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 3), MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards =
                monsterCollectionState.CalculateRewards(
                    states.GetSheet<MonsterCollectionRewardSheet>(),
                    context.BlockIndex
                );

            if (rewards.Count == 0)
            {
                throw new RequiredBlockIndexException($"{collectionAddress} is not available yet");
            }

            Guid id = context.Random.GenerateRandomGuid();
            var result = new MonsterCollectionResult(id, avatarAddress, rewards);
            var mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
            avatarState.Update(mail);

            ItemSheet itemSheet = states.GetItemSheet();
            foreach (MonsterCollectionRewardSheet.RewardInfo rewardInfo in rewards)
            {
                ItemSheet.Row row = itemSheet[rewardInfo.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem2(item, rewardInfo.Quantity);
            }
            monsterCollectionState.Claim(context.BlockIndex);

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(collectionAddress, monsterCollectionState.Serialize());
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
