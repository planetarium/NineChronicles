using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class RecipeScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public EnhancedScroller scroller;
        public RecipeCellView cellViewPrefab;

        private float _cellViewHeight = 90f;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();

            scroller.Delegate = this;

            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as RecipeCellView;
            if(ReferenceEquals(cellView, null))
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            cellView.name = $"Cell {dataIndex}";
            cellView.SetData();
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return 7;
        }

        public void SetData()
        {
            scroller.ReloadData();
        }
    }
}
