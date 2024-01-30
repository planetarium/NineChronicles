using System;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class CollectionScroll : RectScroll<Collection.Model, CollectionScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<Collection.Model> OnClickActiveButton = new Subject<Collection.Model>();

            public override void Dispose()
            {
                OnClickActiveButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<Collection.Model> OnClickActiveButton => Context.OnClickActiveButton;
    }
}
