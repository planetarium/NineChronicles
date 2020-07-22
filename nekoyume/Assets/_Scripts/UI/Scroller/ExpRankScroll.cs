using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ExpRankScroll : RectScroll<ExpRankCell.ViewModel, ExpRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext, IDisposable
        {
            public readonly Subject<ExpRankCell> OnClick = new Subject<ExpRankCell>();

            public void Dispose()
            {
                OnClick?.Dispose();
            }
        }

        public IObservable<ExpRankCell> OnClick => Context.OnClick;

        public void Show(IEnumerable<(int rank, RankingInfo rankingInfo)> itemData)
        {
            Show(itemData
                .Select(tuple => new ExpRankCell.ViewModel
                {
                    rank = tuple.rank,
                    rankingInfo = tuple.rankingInfo
                })
                .ToList());
        }
    }
}
