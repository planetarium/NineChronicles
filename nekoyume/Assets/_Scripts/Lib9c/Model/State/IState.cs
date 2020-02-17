using Bencodex.Types;

namespace Nekoyume.Model.State
{
    public interface IState
    {
        IValue Serialize();
    }
}
