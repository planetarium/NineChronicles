using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashRandomBuffV1
    {
        Address AvatarAddress { get; }
        bool AdvancedGacha { get; }
    }
}
