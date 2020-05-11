using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public RectTransform scrollRectTransform;
        public EnhancedScroller scroller;
        public InventoryCellView cellViewPrefab;
        public int numberOfInnerItemPerCell = 1;
        public int capacity = 48;

        /// <summary>
        /// `_scroller.Delegate`를 할당할 때, `_scroller` 내부에서 `_reloadData = true`가 된다.
        /// 이때문에 `SetData()`를 통해 `_dataList`를 할당하기 전에 `GetNumberOfCells()` 등과 같은
        /// `EnhancedScroller`의 `LifeCycle` 함수가 호출되면서 `null` 참조 문제가 발생한다.
        /// 그 상황을 피하기 위해서 빈 리스트를 할당한다.
        /// </summary>
        private ReactiveCollection<Model.InventoryItem> _dataList = new ReactiveCollection<Model.InventoryItem>();
        private float _cellViewHeight = 100f;

        private readonly List<IDisposable> _disposablesAtSetData = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            scroller.Delegate = this;

            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        #region IEnhancedScrollerDelegate

        public int GetNumberOfCells(EnhancedScroller scr)
        {
            return Mathf.Max(
                Mathf.CeilToInt((float) capacity / numberOfInnerItemPerCell),
                Mathf.CeilToInt((float) _dataList.Count / numberOfInnerItemPerCell)
                );
        }

        public float GetCellViewSize(EnhancedScroller scr, int dataIndex)
        {
            return _cellViewHeight;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scr, int dataIndex, int cellIndex)
        {
            if (!(scroller.GetCellView(cellViewPrefab) is InventoryCellView cellView))
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            var di = dataIndex * numberOfInnerItemPerCell;

            cellView.name = $"Cell {di} to {di + numberOfInnerItemPerCell - 1}";
            cellView.SetData(scroller, _dataList, di);

            return cellView;
        }

        #endregion

        public void SetData(ReactiveCollection<Model.InventoryItem> dataList)
        {
            Debug.LogWarning("Scroller SetData() called.");
            if (dataList is null)
            {
                dataList = new ReactiveCollection<Model.InventoryItem>();
            }

            _disposablesAtSetData.DisposeAllAndClear();
            _dataList = dataList;
            _dataList.ObserveAdd().Subscribe(_ =>
            {
                scroller.ReloadData();
                Debug.LogWarning("Scroller ReloadData() called from ObserveAdd().");
            }).AddTo(_disposablesAtSetData);
            _dataList.ObserveRemove().Subscribe(_ =>
            {
                scroller.ReloadData();
                Debug.LogWarning("Scroller ReloadData() called from ObserveRemove().");
            }).AddTo(_disposablesAtSetData);

            scroller.ReloadData();
        }

        public void DisposeAddedAtSetData()
        {
            _disposablesAtSetData.DisposeAllAndClear();
        }

        public InventoryItemView GetByIndex(int index)
        {
            for (var i = scroller.StartDataIndex ; i <= scroller.EndDataIndex; ++i)
            {
                var cellView = (InventoryCellView) scroller.GetCellViewAtDataIndex(i);
                var itemIndex = i * numberOfInnerItemPerCell;
                foreach (var item in cellView.items)
                {
                    if (itemIndex == index)
                    {
                        return item;
                    }
                    itemIndex++;
                }
            }
            return null;
        }
    }
}
