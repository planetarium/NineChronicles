using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class CollectionScroll : RectScroll<CollectionCell.ViewModel, CollectionScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<CollectionCell.ViewModel> OnClickActiveButton = new Subject<CollectionCell.ViewModel>();

            public override void Dispose()
            {
                OnClickActiveButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<CollectionCell.ViewModel> OnClickActiveButton => Context.OnClickActiveButton;
    }
}
