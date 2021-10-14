using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public static class HashAlgorithmTypePolicy
    {
        public static readonly HashAlgorithmType DefaultValue = HashAlgorithmType.Of<SHA256>();

        public static VariableSubPolicy<HashAlgorithmType> Default
        {
            get
            {
                return VariableSubPolicy<HashAlgorithmType>
                    .Create(DefaultValue);
            }
        }

        public static VariableSubPolicy<HashAlgorithmType> Mainnet
        {
            get
            {
                return Default;
            }
        }
    }
}
