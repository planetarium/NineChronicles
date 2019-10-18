using Bencodex.Types;

namespace Nekoyume.State
{
    public interface IState
    {
        IValue Serialize();
    }
}
