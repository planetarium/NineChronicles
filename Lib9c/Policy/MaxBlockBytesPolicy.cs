using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class MaxBlockBytesPolicy : VariableSubPolicy<int>
    {
        private MaxBlockBytesPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MaxBlockBytesPolicy(
            MaxBlockBytesPolicy maxBlockBytesPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(maxBlockBytesPolicy, spannedSubPolicy)
        {
        }

        public static MaxBlockBytesPolicy Default =>
            new MaxBlockBytesPolicy(int.MaxValue);

        public static MaxBlockBytesPolicy Mainnet =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: BlockPolicySource.MaxGenesisBytes))
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 1,
                    value: BlockPolicySource.MaxBlockBytes));
    }
}
