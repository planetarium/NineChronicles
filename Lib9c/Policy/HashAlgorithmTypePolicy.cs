using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public static class HashAlgorithmTypePolicy
    {
        public static VariableSubPolicy<HashAlgorithmType> Default
        {
            get
            {
                return VariableSubPolicy<HashAlgorithmType>
                    .Create(HashAlgorithmType.Of<SHA256>());
            }
        }

        public static VariableSubPolicy<HashAlgorithmType> Mainnet
        {
            get
            {
                return VariableSubPolicy<HashAlgorithmType>
                    .Create(HashAlgorithmType.Of<SHA256>());
            }
        }
    }
}
