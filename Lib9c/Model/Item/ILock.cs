using Bencodex.Types;

namespace Nekoyume.Model.Item
{
    public interface ILock
    {
        LockType Type { get; }

        IValue Serialize();
    }
}
