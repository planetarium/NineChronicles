using System;
using FancyScrollView;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ExpRankScroll : BaseScroll<
        (int ranking, Nekoyume.Model.State.RankingInfo rankingInfo),
        ExpRankScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public readonly Subject<ExpRankCell> OnClick = new Subject<ExpRankCell>();

            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }

        public IObservable<ExpRankCell> OnClick => Context.OnClick;
    }
}
