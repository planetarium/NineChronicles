using System;
using System.Collections.Generic;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationResultPopup : IDisposable
    {
        public readonly Subject<CombinationResultPopup> OnClickSubmit = new Subject<CombinationResultPopup>();
        public readonly ReactiveProperty<ItemInformation> itemInformation = new ReactiveProperty<ItemInformation>();
        public bool isSuccess;
        public ICollection<CombinationMaterial> materialItems;
        
        public CombinationResultPopup(CountableItem countableItem = null)
        {
            itemInformation.Value = new ItemInformation(countableItem);
        }
        
        public void Dispose()
        {   
            OnClickSubmit.Dispose();
        }
    }
}
