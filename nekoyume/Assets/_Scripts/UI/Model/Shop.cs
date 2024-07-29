using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        public const int MinimumPrice = 1;

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup = new(new ItemCountAndPricePopup());

        public readonly ReactiveProperty<ItemCountableAndPricePopup> ItemCountableAndPricePopup = new(new ItemCountableAndPricePopup());

        public void Dispose()
        {
            ItemCountAndPricePopup.DisposeAll();
            ItemCountableAndPricePopup.DisposeAll();
        }
    }
}
