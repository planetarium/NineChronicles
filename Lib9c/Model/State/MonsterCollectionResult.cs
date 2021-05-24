using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class MonsterCollectionResult : AttachmentActionResult
    {
        public Guid id;
        public Address avatarAddress;
        public List<MonsterCollectionRewardSheet.RewardInfo> rewards;

        public MonsterCollectionResult(Guid guid, Address address, List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos)
        {
            id = guid;
            avatarAddress = address;
            rewards = rewardInfos;
        }

        public MonsterCollectionResult(Dictionary serialized)
        {
            id = serialized[SerializeKeys.IdKey].ToGuid();
            avatarAddress = serialized[SerializeKeys.AvatarAddressKey].ToAddress();
            rewards = serialized[SerializeKeys.MonsterCollectionResultKey]
                .ToList(s => new MonsterCollectionRewardSheet.RewardInfo((Dictionary) s));
        }

        protected override string TypeId => "monsterCollection.result";

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)SerializeKeys.IdKey] = id.Serialize(),
                [(Text)SerializeKeys.AvatarAddressKey] = avatarAddress.Serialize(),
                [(Text)SerializeKeys.MonsterCollectionResultKey] = new List(rewards.Select(r => r.Serialize())).Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
