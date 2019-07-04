using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformationTooltip : Tooltip
    {
        public readonly ItemInformation itemInformation;
        
        public readonly ReactiveProperty<string> titleText = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> closeButtonText = new ReactiveProperty<string>("닫기");
        public readonly ReactiveProperty<string> submitButtonText = new ReactiveProperty<string>(null);

        public ItemInformationTooltip(CountableItem countableItem = null)
        {
            itemInformation = new ItemInformation(countableItem);

            itemInformation.item.Subscribe(item =>
            {
                if (item is null)
                {
                    titleText.Value = "";
                    return;
                }

                titleText.Value = item.item.Value.Data.name;
                item.countEnabledFunc.Value = item2 => false;
            });
        }
    }
}
