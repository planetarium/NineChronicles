using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationResultPopup : IDisposable
    {
        public readonly ReactiveProperty<ItemInformation> itemInformation = new ReactiveProperty<ItemInformation>();
        public bool isSuccess;
        public ICollection<CombinationMaterial> materialItems;

        public readonly Subject<CombinationResultPopup> onClickSubmit = new Subject<CombinationResultPopup>();
        
        public CombinationResultPopup(CountableItem countableItem = null)
        {
            itemInformation.Value = new ItemInformation(countableItem);
        }
        
        public void Dispose()
        {   
            onClickSubmit.Dispose();
        }
    }
}
