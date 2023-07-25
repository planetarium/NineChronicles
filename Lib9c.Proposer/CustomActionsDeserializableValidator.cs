using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.Loader;
using Libplanet.Types.Tx;

namespace Nekoyume.Blockchain
{
    public class CustomActionsDeserializableValidator
    {
        private readonly IActionLoader _actionLoader;
        private readonly long _nextBlockIndex;

        public CustomActionsDeserializableValidator(IActionLoader actionLoader, long nextBlockIndex)
        {
            _actionLoader = actionLoader;
            _nextBlockIndex = nextBlockIndex;
        }

        public bool Validate(ITransaction transaction)
        {
            if (!(transaction.Actions is { } actions))
            {
                return true;
            }
            else
            {
                return actions.All(action => CanLoadAction(_nextBlockIndex, action));
            }
        }

        private bool CanLoadAction(long index, IValue value)
        {
            try
            {
                _ = _actionLoader.LoadAction(index, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
