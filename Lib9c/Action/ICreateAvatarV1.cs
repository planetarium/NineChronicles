using Libplanet;

namespace Nekoyume.Action
{
    public interface ICreateAvatarV1
    {
        Address AvatarAddress { get; }
        int Index { get; }
        int Hair { get; }
        int Lens { get; }
        int Ear { get; }
        int Tail { get; }
        string Name { get; }
    }
}
