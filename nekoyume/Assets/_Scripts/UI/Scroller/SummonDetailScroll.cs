using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
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

        public IObservable<SummonDetailCell.Model> OnClick => Context.OnClick;

        protected override void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            base.JumpTo(itemIndex, alignment);
            Context.OnClick.OnNext(ItemsSource[itemIndex]);
        }
    }
}
