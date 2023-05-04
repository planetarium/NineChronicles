using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Tx;

namespace Nekoyume.BlockChain
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
            var types = _actionLoader.Load(_nextBlockIndex);

            return transaction.Actions?.All(ca =>
                ca is Dictionary dictionary &&
                dictionary.TryGetValue((Text)"type_id", out IValue typeIdValue) &&
                typeIdValue is Text typeId &&
                types.ContainsKey(typeId) &&
                dictionary.TryGetValue((Text)"values", out IValue values) &&
                Activator.CreateInstance(types[typeId]) is IAction action &&
                DoesNotThrowsAnyException(() => action.LoadPlainValue(values))) == true;
        }

        private bool DoesNotThrowsAnyException(System.Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
