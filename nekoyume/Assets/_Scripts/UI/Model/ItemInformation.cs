using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformation : IDisposable
    {
        public readonly ReactiveProperty<CountableItem> item = new ReactiveProperty<CountableItem>();

        public ItemInformation(CountableItem countableItem = null)
        {
            item.Value = countableItem;
        }

        public void Dispose()
        {
            item.Dispose();
        }
    }
}
