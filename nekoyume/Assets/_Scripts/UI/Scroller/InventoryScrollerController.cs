using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public EnhancedScroller scroller;
        public InventoryCellView cellViewPrefab;
        public int numberOfInnerItemPerCell = 1;

        /// <summary>
        /// `_scroller.Delegate`를 할당할 때, `_scroller` 내부에서 `_reloadData = true`가 된다.
        /// 이때문에 `SetData()`를 통해 `_dataList`를 할당하기 전에 `GetNumberOfCells()` 등과 같은
        /// `EnhancedScroller`의 `LifeCycle` 함수가 호출되면서 `null` 참조 문제가 발생한다.
        /// 그 상황을 피하기 위해서 빈 리스트를 할당한다.
        /// </summary>
        private ReactiveCollection<Model.Inventory.Item> _dataList = new ReactiveCollection<Model.Inventory.Item>();
        private float _cellViewHeight = 100f;
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(scroller, null) ||
                ReferenceEquals(cellViewPrefab, null))
            {
                throw new SerializeFieldNullException();
            }

            scroller.Delegate = this;

            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        #region IEnhancedScrollerDelegate

        public int GetNumberOfCells(EnhancedScroller scr)
        {
            return Mathf.CeilToInt((float) _dataList.Count / numberOfInnerItemPerCell);
        }

        public float GetCellViewSize(EnhancedScroller scr, int dataIndex)
        {
            return _cellViewHeight;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scr, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as InventoryCellView;
            if (ReferenceEquals(cellView, null))
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            var di = dataIndex * numberOfInnerItemPerCell;

            cellView.name = $"Cell {di} to {di + numberOfInnerItemPerCell - 1}";
            cellView.SetData(_dataList, di);

            return cellView;
        }

        #endregion

        public void SetData(ReactiveCollection<Model.Inventory.Item> dataList)
        {
            _disposables.ForEach(d => d.Dispose());
            
            if (ReferenceEquals(dataList, null))
            {
                dataList = new ReactiveCollection<Model.Inventory.Item>();
            }

            _dataList = dataList;
            _dataList.ObserveAdd().Subscribe(OnDataAdd).AddTo(_disposables);
            _dataList.ObserveRemove().Subscribe(OnDataRemove).AddTo(_disposables);
            
            scroller.ReloadData();
        }

        private void OnDataAdd(CollectionAddEvent<Model.Inventory.Item> e)
        {
            scroller.ReloadData();
        }

        private void OnDataRemove(CollectionRemoveEvent<Model.Inventory.Item> e)
        {
            scroller.ReloadData();
        }
    }
}
