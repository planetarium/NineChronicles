using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Faucet
{
    [Serializable]
    public class FaucetRuneInfo
    {
        public int RuneId { get; }
        public int Amount { get; }

        public FaucetRuneInfo(int runeId, int amount)
        {
            RuneId = runeId;
            Amount = amount;
        }

        public FaucetRuneInfo(List serialized)
        {
            RuneId = serialized[0].ToInteger();
            Amount = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(RuneId.Serialize())
                .Add(Amount.Serialize());
        }

        protected bool Equals(FaucetRuneInfo other)
        {
            if (other is null)
            {
                return false;
            }

            return RuneId == other.RuneId && Amount == other.Amount;
        }
    }
}
