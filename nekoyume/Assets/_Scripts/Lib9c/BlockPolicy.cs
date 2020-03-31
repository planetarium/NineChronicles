using System;
using System.Linq;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.BlockChain
{
    public class BlockPolicy
    {
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);

        private static Func<WhiteListSheet> GetWhiteListSheet { get; set; }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold {Gold = 1};

            public InvalidBlockException ValidateNextBlock(
                BlockChain<PolymorphicAction<ActionBase>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock
            )
            {
                return null;
            }

            public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
            {
                return blocks.Tip is null ? 0 : 1;
            }

            public bool DoesTransactionFollowsPolicy(
                Transaction<PolymorphicAction<ActionBase>> transaction
            ) =>
                true;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public static IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(
            int miniumDifficulty,
            Func<WhiteListSheet> getWhiteListSheet)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            GetWhiteListSheet = getWhiteListSheet;
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 1 },
                BlockInterval,
                miniumDifficulty,
                2048,
                doesTransactionFollowPolicy: IsSignerAuthorized
            );
#endif
        }

        private static bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            var signerPublicKey = transaction.PublicKey;
            var whiteListSheet = GetWhiteListSheet?.Invoke();

            return whiteListSheet is null
                   || whiteListSheet.Count == 0
                   || whiteListSheet.Values.Any(row => signerPublicKey.Equals(row.PublicKey));
        }
    }
}
