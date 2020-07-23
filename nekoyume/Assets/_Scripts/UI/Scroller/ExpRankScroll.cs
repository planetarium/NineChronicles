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
    }
}
