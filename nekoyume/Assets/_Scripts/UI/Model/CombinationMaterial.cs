using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationMaterial : CountEditableItem
    {
        public readonly ReactiveProperty<bool> opened = new ReactiveProperty<bool>();
        
        public CombinationMaterial(ItemBase item, int count, int minCount, int maxCount, bool opened) : base(item, count, minCount, maxCount)
        {
            this.opened.Value = opened;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            opened.Dispose();
        }
    }
}
