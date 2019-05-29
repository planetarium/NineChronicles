using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// RankingState 포함하는 값의 변화를 ActionBase.EveryRender<T>()를 통해 감지하고, 동기화한다.
    /// 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveRankingState
    {
        public static readonly ReactiveProperty<RankingState> RankingState = new ReactiveProperty<RankingState>();

        public static void Initialize(RankingState rankingState)
        {
            if (ReferenceEquals(rankingState, null))
            {
                return;
            }

            RankingState.Value = rankingState;
        }
    }
}
