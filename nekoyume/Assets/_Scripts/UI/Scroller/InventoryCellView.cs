using EnhancedUI.EnhancedScroller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class InventoryCellView : EnhancedScrollerCellView
    {
        public InventoryItemView[] items;

        #region Mono

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(EnhancedScroller scroller, ReactiveCollection<Model.InventoryItem> dataList, int firstIndex)
        {
            if (dataList is null)
            {
                Clear();
                return;
            }

            var dataCount = dataList.Count;
            for (var i = 0; i < items.Length; i++)
            {
                var index = firstIndex + i;
                var item = items[i];

                item.SetData(index < dataCount
                    ? dataList[index]
                    : null);
                item.gameObject.SetActive(true);
            }
        }

        public void Clear()
        {
            foreach (var item in items)
            {
                item.Clear();
            }
        }
    }
}
