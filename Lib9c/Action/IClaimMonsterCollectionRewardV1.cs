using Libplanet;

namespace Nekoyume.Action
{
    public interface IClaimMonsterCollectionRewardV1
    {
        Address AvatarAddress { get; }
        int CollectionRound { get; }
    }
}
