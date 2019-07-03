using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformationTooltip : Tooltip
    {
        public readonly ReactiveProperty<string> titleText = new ReactiveProperty<string>();
        public readonly ReactiveProperty<ItemInformation> itemInformation = new ReactiveProperty<ItemInformation>();   
        public readonly ReactiveProperty<string> closeButtonText = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> submitButtonText = new ReactiveProperty<string>();

        public ItemInformationTooltip()
        {
            itemInformation.Value = new ItemInformation();

            itemInformation.Value.item.Subscribe(item =>
            {
                if (item is null)
                {
                    return;
                }
                
                item.countEnabledFunc.Value = item2 => false;
            });
        }
    }
}
