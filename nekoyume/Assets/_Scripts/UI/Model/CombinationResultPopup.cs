using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationResultPopup : CountableItem
    {
        public bool isSuccess;
        public ICollection<CombinationMaterial> materialItems;

        public readonly Subject<CombinationResultPopup> onClickSubmit = new Subject<CombinationResultPopup>();
        
        public CombinationResultPopup(ItemBase value) : base(value, 1)
        {
        }
        
        public override void Dispose()
        {   
            base.Dispose();
            onClickSubmit.Dispose();
        }
    }
}
