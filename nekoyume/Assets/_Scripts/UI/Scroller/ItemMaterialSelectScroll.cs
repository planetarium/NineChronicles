using System;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class ItemMaterialSelectScroll : RectScroll<ItemMaterialSelectScroll.Model,
        ItemMaterialSelectScroll.ContextModel>
    {
        public class Model
        {
            public CountableItem Item { get; }
            public readonly ReactiveProperty<int> SelectedCount = new(0);
            public Model(CountableItem item, int selectedCount = 0)
            {
                Item = item;
                SelectedCount.Value = selectedCount;
            }
        }

        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<(Model, int)> OnChangeCount = new();

            public override void Dispose()
            {
                OnChangeCount?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<(Model, int)> OnChangeCount => Context.OnChangeCount;
    }
}
