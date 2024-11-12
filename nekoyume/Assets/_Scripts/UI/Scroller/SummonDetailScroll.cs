using System;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class SummonDetailScroll : RectScroll<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public Subject<SummonDetailCell.Model> OnClick { get; } = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<SummonDetailCell.Model> OnClickDetailButton =>
            Context.OnClick;
    }
}
