using System;
using System.Numerics;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformationTooltip : Tooltip
    {
        public readonly ItemInformation ItemInformation;

        public readonly ReactiveProperty<string> TitleText = new ReactiveProperty<string>();

        public readonly ReactiveProperty<Func<CountableItem, bool>> SubmitButtonEnabledFunc =
            new ReactiveProperty<Func<CountableItem, bool>>();

        public readonly ReactiveProperty<bool> SubmitButtonEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<string> SubmitButtonText = new ReactiveProperty<string>(null);

        public readonly ReactiveProperty<bool> PriceEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<BigInteger> Price = new ReactiveProperty<BigInteger>(0);

        public readonly Subject<UI.ItemInformationTooltip> OnSubmitClick = new Subject<UI.ItemInformationTooltip>();
        public readonly Subject<UI.ItemInformationTooltip> OnCloseClick = new Subject<UI.ItemInformationTooltip>();

        public readonly ReadOnlyReactiveProperty<bool> FooterRootActive;

        public ItemInformationTooltip(CountableItem countableItem = null)
        {
            ItemInformation = new ItemInformation(countableItem);
            ItemInformation.item.Subscribe(item =>
            {
                if (item is null)
                {
                    TitleText.Value = "";

                    return;
                }

                TitleText.Value = item.ItemBase.Value.GetLocalizedName();

                if (!(item is ShopItem shopItem))
                {
                    PriceEnabled.Value = false;

                    return;
                }

                PriceEnabled.Value = true;
                Price.Value = shopItem.Price.Value;
            });

            SubmitButtonEnabledFunc.Value = SubmitButtonEnabledFuncDefault;
            SubmitButtonEnabledFunc.Subscribe(func =>
            {
                if (func == null)
                {
                    SubmitButtonEnabledFunc.Value = SubmitButtonEnabledFuncDefault;
                }

                SubmitButtonEnabled.Value = SubmitButtonEnabledFunc.Value(ItemInformation.item.Value);
            });

            FooterRootActive = Observable.CombineLatest(SubmitButtonEnabled, PriceEnabled)
                .Select(_ => _[0] || _[1]).ToReadOnlyReactiveProperty();
        }

        public override void Dispose()
        {
            TitleText.Dispose();
            SubmitButtonEnabledFunc.Dispose();
            SubmitButtonEnabled.Dispose();
            SubmitButtonText.Dispose();
            PriceEnabled.Dispose();
            Price.Dispose();

            OnSubmitClick.Dispose();
            OnCloseClick.Dispose();

            FooterRootActive.Dispose();

            base.Dispose();
        }

        private static bool SubmitButtonEnabledFuncDefault(CountableItem model)
        {
            return false;
        }
    }
}
