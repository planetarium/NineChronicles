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
            // FIXME Tx가 들어간 블록에 대해 검사가 너무 느려서 테스트 기간 동안은 사용하지 않습니다.
            // 속도를 정상화한 다음 복원해야 합니다.
            // 참고: https://github.com/planetarium/nekoyume-unity/pull/1826
            return true;
        }
    }
}
