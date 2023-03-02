using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IRuneEnhancementV1
    {
        Address AvatarAddress { get; }
        int RuneId { get; }
        int TryCount { get; }
    }
}
