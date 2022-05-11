using Nekoyume.State;
using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankScroll : RectScroll<ArenaRankCell.ViewModel, ArenaRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            // From cell to scroll
            public readonly Subject<ArenaRankCell> OnClickAvatarInfo = new Subject<ArenaRankCell>();
            public readonly Subject<ArenaRankCell> OnClickChallenge = new Subject<ArenaRankCell>();
            // From scroll to cell
            public readonly Subject<bool> UpdateConditionalStateOfChallengeButtons = new Subject<bool>();

            public override void Dispose()
            {
                OnClickAvatarInfo?.Dispose();
                OnClickChallenge?.Dispose();
                UpdateConditionalStateOfChallengeButtons?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => Context.OnClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => Context.OnClickChallenge;

        public Subject<bool> UpdateConditionalStateOfChallengeButtons =>
            Context.UpdateConditionalStateOfChallengeButtons;
    }
}
