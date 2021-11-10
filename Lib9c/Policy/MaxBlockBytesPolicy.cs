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

        public static IVariableSubPolicy<int> Default =>
            new MaxBlockBytesPolicy(int.MaxValue);

        public static IVariableSubPolicy<int> Mainnet =>
            Default
                // Note: The genesis block of 9c-main weighs 11,085,640 B (11 MiB).
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: 1024 * 1024 * 15))   // 15 MiB
                // Note: Initial analysis of the heaviest block of 9c-main
                // (except for the genesis) weighs 58,408 B (58 KiB).
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 1,
                    value: 1024 * 100))         // 100 KiB
                // Note: Temporary limit increase for resolving
                // https://github.com/planetarium/NineChronicles/issues/777.
                // Issued for v100081.  Temporary ad hoc increase was introduced
                // around 2_500_000.
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 2_000_001,
                    value: 1024 * 1024 * 10))    // 10 MiB
                // Note: Reverting back to the previous limit.  Issued for v100086.
                // FIXME: Starting index must be finalized accordingly before deployment.
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 3_000_001,
                    value: 1024 * 100));        // 100 KiB

        // Note: For internal testing.
        public static IVariableSubPolicy<int> Internal =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: 1024 * 1024 * 15))   // 15 MiB
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 1,
                    value: 1024 * 100))         // 100 KiB
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 2_000_001,
                    value: 1024 * 1024 * 10))    // 10 MiB
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 3_000_001,
                    value: 1024 * 100));        // 100 KiB
    }
}
