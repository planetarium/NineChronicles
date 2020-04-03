using Assets.SimpleLocalization;
using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        public readonly ReactiveProperty<UI.Shop.StateType> State = new ReactiveProperty<UI.Shop.StateType>();

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup =
            new ReactiveProperty<ItemCountAndPricePopup>(new ItemCountAndPricePopup());

        public void Dispose()
        {
            State.Dispose();
            ItemCountAndPricePopup.DisposeAll();
        }
    }
}
