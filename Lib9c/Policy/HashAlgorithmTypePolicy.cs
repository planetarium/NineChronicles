using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class HashAlgorithmTypePolicy : VariableSubPolicy<HashAlgorithmType>
    {
        private HashAlgorithmTypePolicy(HashAlgorithmType defaultValue)
            : base(defaultValue)
        {
        }

        private HashAlgorithmTypePolicy(
            HashAlgorithmTypePolicy hashAlgorithmTypePolicy,
            SpannedSubPolicy<HashAlgorithmType> spannedSubPolicy)
            : base(hashAlgorithmTypePolicy, spannedSubPolicy)
        {
        }

        public static VariableSubPolicy<HashAlgorithmType> Default =>
            new HashAlgorithmTypePolicy(HashAlgorithmType.Of<SHA256>());

        public static VariableSubPolicy<HashAlgorithmType> Mainnet =>
            Default;
    }
}
