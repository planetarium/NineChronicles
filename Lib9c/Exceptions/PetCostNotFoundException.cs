using System;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class PetCostNotFoundException : Exception
    {
        public PetCostNotFoundException()
        {
        }

        public PetCostNotFoundException(string message) : base(message)
        {
        }
    }
}
