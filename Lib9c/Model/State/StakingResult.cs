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
    public class StakingResult : AttachmentActionResult
    {
        public Guid id;
        public Address avatarAddress;
        public List<StakingRewardSheet.RewardInfo> rewards;

        public StakingResult(Guid guid, Address address, List<StakingRewardSheet.RewardInfo> rewardInfos)
        {
            id = guid;
            avatarAddress = address;
            rewards = rewardInfos;
        }

        public StakingResult(Dictionary serialized)
        {
            id = serialized[SerializeKeys.IdKey].ToGuid();
            avatarAddress = serialized[SerializeKeys.AvatarAddressKey].ToAddress();
            rewards = serialized[SerializeKeys.StakingResultKey]
                .ToList(s => new StakingRewardSheet.RewardInfo((Dictionary) s));
        }

        protected override string TypeId => "staking.result";

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)SerializeKeys.IdKey] = id.Serialize(),
                [(Text)SerializeKeys.AvatarAddressKey] = avatarAddress.Serialize(),
                [(Text)SerializeKeys.StakingResultKey] = new List(rewards.Select(r => r.Serialize())).Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
