using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IClaimMonsterCollectionRewardV1
    {
        Address AvatarAddress { get; }
        int CollectionRound { get; }
    }
}
