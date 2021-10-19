using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class BuyItemInformationPopup : IDisposable
    {
        public readonly Subject<BuyItemInformationPopup> OnClickSubmit = new Subject<BuyItemInformationPopup>();
        public readonly ReactiveProperty<ItemInformation> itemInformation = new ReactiveProperty<ItemInformation>();
        public bool isSuccess;
        public ICollection<CombinationMaterial> materialItems;

        public BuyItemInformationPopup(CountableItem countableItem = null)
        {
            itemInformation.Value = new ItemInformation(countableItem);
        }

        public void Dispose()
        {
            OnClickSubmit.Dispose();
            itemInformation.Dispose();
        }
    }
}
