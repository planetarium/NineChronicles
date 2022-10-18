using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class MaxTransactionsBytesPolicy : VariableSubPolicy<long>
    {
        private MaxTransactionsBytesPolicy(long defaultValue)
            : base(defaultValue)
        {
        }

        private MaxTransactionsBytesPolicy(
            MaxTransactionsBytesPolicy maxTransactionsBytesPolicy,
            SpannedSubPolicy<long> spannedSubPolicy)
            : base(maxTransactionsBytesPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<long> Default =>
            new MaxTransactionsBytesPolicy(long.MaxValue);

        public static IVariableSubPolicy<long> Mainnet =>
            Default
                // Note: The genesis block of 9c-main weighs 11,085,640 B (11 MiB).
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 0L,
                    value: 1024L * 1024L * 15L))    // 15 MiB
                // Note: Initial analysis of the heaviest block of 9c-main
                // (except for the genesis) weighs 58,408 B (58 KiB).
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 1L,
                    value: 1024L * 100L))           // 100 KiB
                // Note: Temporary limit increase for resolving
                // https://github.com/planetarium/NineChronicles/issues/777.
                // Issued for v100081.  Temporary ad hoc increase was introduced
                // around 2_500_000.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_000_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                // Note: Reverting back to the previous limit.  Issued for v100086.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_800_001L,
                    value: 1024L * 100L))           // 100 KiB
                // Note: Temporary limit increase for 50_000 blocks to accommodate
                // issuing new invitation codes.  Issued for v100089.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_876_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_926_001L,
                    value: 1024L * 100L))           // 100 KiB
                // Note: Limit increase to accommodate issuing new invitation codes.
                // Issued for v100098.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 3_150_001L,
                    value: 1024L * 500L));          // 500 KiB

        // Note: For internal testing.
        public static IVariableSubPolicy<long> Internal =>
            Default
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 0L,
                    value: 1024L * 1024L * 15L))   // 15 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 1L,
                    value: 1024L * 100L))           // 100 KiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_000_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_800_001L,
                    value: 1024L * 100L))           // 100 KiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_876_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_926_001L,
                    value: 1024L * 100L))           // 100 KiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 3_150_001L,
                    value: 1024L * 500L));          // 500 KiB
    }
}
