using System;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class CollectionScroll : RectScroll<CollectionModel, CollectionScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<CollectionModel> OnClickActiveButton = new Subject<CollectionModel>();
            public readonly Subject<CollectionMaterial> OnClickMaterial = new Subject<CollectionMaterial>();

            public override void Dispose()
            {
                OnClickActiveButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<CollectionModel> OnClickActiveButton => Context.OnClickActiveButton;
        public IObservable<CollectionMaterial> OnClickMaterial => Context.OnClickMaterial;
    }
}
