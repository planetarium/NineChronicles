using EnhancedUI.EnhancedScroller;
using Nekoyume.UI.ItemView;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class InventoryCellView : EnhancedScrollerCellView
    {
        [SerializeField] public InventoryItemView[] items;
        
        public void SetData(ReactiveCollection<Model.Inventory.Item> dataList, int firstIndex)
        {
            var dataCount = dataList.Count;
            for (int i = 0; i < items.Length; i++)
            {
                var index = firstIndex + i;
                var item = items[i];
                if (index < dataCount)
                {
                    item.SetData(dataList[index]);
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }
    }
}
