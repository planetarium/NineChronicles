using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        public const int MinimumPrice = 10;

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup =
            new ReactiveProperty<ItemCountAndPricePopup>(new ItemCountAndPricePopup());


        public readonly ReactiveProperty<ItemCountableAndPricePopup> ItemCountableAndPricePopup =
            new ReactiveProperty<ItemCountableAndPricePopup>(new ItemCountableAndPricePopup());

        public void Dispose()
        {
            ItemCountAndPricePopup.DisposeAll();
            ItemCountableAndPricePopup.DisposeAll();
        }
    }
}
