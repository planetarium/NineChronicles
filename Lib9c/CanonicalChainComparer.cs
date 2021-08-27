using Libplanet.Blockchain;
using Libplanet.Blocks;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;

namespace Lib9c
{
    public class CanonicalChainComparer : IComparer<IBlockExcerpt>
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
            _totalDifficultyComparer = new TotalDifficultyComparer();
        }

        public AuthorizedMinersState AuthorizedMinersState { get; internal set; }

        public int Compare(IBlockExcerpt x, IBlockExcerpt y)
        {
            if (AuthorizedMinersState is AuthorizedMinersState authorizedMinersState)
            {
                if (x.Index <= authorizedMinersState.ValidUntil ||
                    y.Index <= authorizedMinersState.ValidUntil)
                {
                    long xGen = x.Index / authorizedMinersState.Interval;
                    long yGen = y.Index / authorizedMinersState.Interval;
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
