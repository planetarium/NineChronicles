using Libplanet;

namespace Nekoyume.Action
{
    public interface IRuneEnhancementV1
    {
        Address AvatarAddress { get; }
        int RuneId { get; }
        int TryCount { get; }
    }
}
