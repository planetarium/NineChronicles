using System;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.State;

namespace Nekoyume.UI.Model
{
    using Nekoyume.Model.Item;
    using UniRx;

    public class ItemInformationTooltip : Tooltip
    {
        public readonly ItemInformation ItemInformation;

        public readonly ReactiveProperty<string> TitleText = new ReactiveProperty<string>();

        public readonly ReactiveProperty<Func<CountableItem, bool>> SubmitButtonEnabledFunc =
            new ReactiveProperty<Func<CountableItem, bool>>();

        public readonly ReactiveProperty<bool> SubmitButtonEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<string> SubmitButtonText = new ReactiveProperty<string>(null);

        public readonly ReactiveProperty<FungibleAssetValue> Price;
        public readonly ReactiveProperty<long> ExpiredBlockIndex = new ReactiveProperty<long>();

        public readonly Subject<UI.ItemInformationTooltip> OnSubmitClick = new Subject<UI.ItemInformationTooltip>();
        public readonly Subject<UI.ItemInformationTooltip> OnCloseClick = new Subject<UI.ItemInformationTooltip>();

        public ItemInformationTooltip(CountableItem countableItem = null)
        {
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            Price = new ReactiveProperty<FungibleAssetValue>(new FungibleAssetValue(currency));

            ItemInformation = new ItemInformation(countableItem);
            ItemInformation.item.Subscribe(item =>
            {
                if (item is null)
                {
                    TitleText.Value = string.Empty;
                    return;
                }

                TitleText.Value = item.ItemBase.Value.GetLocalizedName(false);

                if (item is ShopItem shopItem)
                {
                    Price.Value = shopItem.Price.Value;
                    ExpiredBlockIndex.Value = shopItem.ExpiredBlockIndex.Value;
                }
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
        }

        public override void Dispose()
        {
            TitleText.Dispose();
            SubmitButtonEnabledFunc.Dispose();
            SubmitButtonEnabled.Dispose();
            SubmitButtonText.Dispose();
            Price.Dispose();
            ExpiredBlockIndex.Dispose();
            OnSubmitClick.Dispose();
            OnCloseClick.Dispose();
            base.Dispose();
        }

        private static bool SubmitButtonEnabledFuncDefault(CountableItem model)
        {
            return false;
        }
    }
}
