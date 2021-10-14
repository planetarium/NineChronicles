using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.BlockChain.Policy
{
    public static class MaxBlockBytesPolicy
    {
        public static readonly int DefaultValue = int.MaxValue;

        public static VariableSubPolicy<int> Default
        {
            get
            {
                return VariableSubPolicy<int>
                    .Create(DefaultValue);
            }
        }

        public static VariableSubPolicy<int> Mainnet
        {
            get
            {
                return Default
                    .Add(new SpannedSubPolicy<int>(
                        startIndex: 0,
                        value: BlockPolicySource.MaxGenesisBytes))
                    .Add(new SpannedSubPolicy<int>(
                        startIndex: 1,
                        value: BlockPolicySource.MaxBlockBytes));
            }
        }
    }
}
