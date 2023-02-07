using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ICreateAvatarV2
    {
        int Index { get; }
        int Hair { get; }
        int Lens { get; }
        int Ear { get; }
        int Tail { get; }
        string Name { get; }
    }
}
