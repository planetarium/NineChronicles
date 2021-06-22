using Libplanet.Blockchain;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;

namespace Lib9c
{
    public class CanonicalChainComparer : IComparer<BlockPerception>
    {
        private readonly TotalDifficultyComparer _totalDifficultyComparer;

        public CanonicalChainComparer(
            AuthorizedMinersState authorizedMinersState,
            TimeSpan outdateAfter
        )
            : this(authorizedMinersState, outdateAfter, () => DateTimeOffset.UtcNow)
        {
        }

        public CanonicalChainComparer(
            AuthorizedMinersState authorizedMinersState,
            TimeSpan outdateAfter,
            Func<DateTimeOffset> currentTimeGetter
        )
        {
            AuthorizedMinersState = authorizedMinersState;
            _totalDifficultyComparer = new TotalDifficultyComparer(outdateAfter, currentTimeGetter);
        }

        public AuthorizedMinersState AuthorizedMinersState { get; internal set; }

        public int Compare(BlockPerception x, BlockPerception y)
        {
            if (AuthorizedMinersState is AuthorizedMinersState authorizedMinersState)
            {
                long xIndex = x.BlockExcerpt.Index;
                long yIndex = y.BlockExcerpt.Index;
                if (xIndex <= authorizedMinersState.ValidUntil ||
                    yIndex <= authorizedMinersState.ValidUntil)
                {
                    long xGen = xIndex / authorizedMinersState.Interval;
                    long yGen = yIndex / authorizedMinersState.Interval;
                    if (xGen != yGen)
                    {
                        return xGen < yGen ? -1 : 1;
                    }
                }
            }

            return _totalDifficultyComparer.Compare(x, y);
        }
    }
}
