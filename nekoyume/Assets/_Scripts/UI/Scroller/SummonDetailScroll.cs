using System;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class SummonDetailScroll : RectScroll<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public Subject<SummonDetailCell.Model> OnClick { get; } = new();
            public ReactiveProperty<SummonDetailCell.Model> Selected { get; } = new();

            public ContextModel()
            {
                OnClick.Subscribe(model => Selected.Value = model);
            }

            public override void Dispose()
            {
                OnClick?.Dispose();
                Selected?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<SummonDetailCell.Model> Selected => Context.Selected;

        protected override void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            base.JumpTo(itemIndex, alignment);
            Context.OnClick.OnNext(ItemsSource[itemIndex]);
        }
    }
}
