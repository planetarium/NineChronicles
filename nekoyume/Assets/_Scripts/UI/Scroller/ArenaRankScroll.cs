using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankScroll : RectScroll<ArenaRankCell.ViewModel, ArenaRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext, IDisposable
        {
            public readonly Subject<ArenaRankCell> OnClickAvatarInfo = new Subject<ArenaRankCell>();
            public readonly Subject<ArenaRankCell> OnClickChallenge = new Subject<ArenaRankCell>();

            public void Dispose()
            {
                OnClickAvatarInfo?.Dispose();
                OnClickChallenge?.Dispose();
            }
        }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => Context.OnClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => Context.OnClickChallenge;
    }
}
