using System;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class CollectionScroll : RectScroll<Collection.Model, CollectionScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<Collection.Model> OnClickActiveButton = new Subject<Collection.Model>();
            public readonly Subject<CollectionMaterial> OnClickMaterial = new Subject<CollectionMaterial>();

            public override void Dispose()
            {
                OnClickActiveButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<Collection.Model> OnClickActiveButton => Context.OnClickActiveButton;
        public IObservable<CollectionMaterial> OnClickMaterial => Context.OnClickMaterial;
    }
}
