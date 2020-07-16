using System;
using FancyScrollView;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankScroll : BaseScroll<
        (int rank, ArenaInfo arenaInfo, ArenaInfo currentAvatarArenaInfo),
        ArenaRankScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public readonly Subject<ArenaRankCell> OnClickAvatarInfo = new Subject<ArenaRankCell>();
            public readonly Subject<ArenaRankCell> OnClickChallenge = new Subject<ArenaRankCell>();

            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => Context.OnClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => Context.OnClickChallenge;
    }
}
