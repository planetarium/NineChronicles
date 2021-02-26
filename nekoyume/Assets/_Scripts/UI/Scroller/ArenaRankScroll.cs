using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankScroll : RectScroll<ArenaRankCell.ViewModel, ArenaRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<ArenaRankCell> OnClickAvatarInfo = new Subject<ArenaRankCell>();
            public readonly Subject<ArenaRankCell> OnClickChallenge = new Subject<ArenaRankCell>();

            public override void Dispose()
            {
                OnClickAvatarInfo?.Dispose();
                OnClickChallenge?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => Context.OnClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => Context.OnClickChallenge;
    }
}
