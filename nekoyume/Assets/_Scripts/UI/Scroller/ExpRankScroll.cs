using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ExpRankScroll : RectScroll<ExpRankCell.ViewModel, ExpRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<ExpRankCell> OnClick = new Subject<ExpRankCell>();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<ExpRankCell> OnClick => Context.OnClick;
    }
}
