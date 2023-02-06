using Libplanet;

namespace Nekoyume.Action
{
    public interface IHackAndSlashSweepV1
    {
        Address AvatarAddress { get; }
        int ApStoneCount { get; }
        int WorldId { get; }
        int StageId { get; }
    }
}
