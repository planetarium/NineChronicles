using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationMaterial : CountEditableItem
    {
        public CombinationMaterial(ItemBase item, int count, int minCount, int maxCount) : base(item, count, minCount, maxCount)
        {
        }
    }
}
