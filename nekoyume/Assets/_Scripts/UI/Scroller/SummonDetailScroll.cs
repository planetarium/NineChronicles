using System;
using System.Collections.Generic;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class SummonDetailScroll : RectScroll<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public Subject<SummonDetailCell.Model> OnClick { get; } = new();
            public HashSet<CostType> ContainedCost { get; } = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                ContainedCost?.Clear();
                base.Dispose();
            }
        }

        public IObservable<SummonDetailCell.Model> OnClickDetailButton =>
            Context.OnClick;

        public HashSet<CostType> ContainedCostType => Context.ContainedCost;
    }
}
