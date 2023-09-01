using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IAuraSummonV1
    {
        Address AvatarAddress { get; }
        int GroupId { get; }

        int SummonCount { get; }
    }
}
