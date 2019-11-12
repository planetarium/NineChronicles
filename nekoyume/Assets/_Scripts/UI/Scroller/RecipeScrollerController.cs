using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class RecipeScrollerController : MonoBehaviour, IEnhancedScrollerDelegate, RecipeCellView.IEventListener
    {
        public EnhancedScroller scroller;
        public RecipeCellView cellViewPrefab;

        private List<RecipeInfo> _recipeList = new List<RecipeInfo>();
        private float _cellViewHeight = 90f;
        
        [CanBeNull] private RecipeCellView.IEventListener _eventListener;

        #region Mono

        private void Awake()
        {
            scroller.Delegate = this;
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = (RecipeCellView) scroller.GetCellView(cellViewPrefab);
            if (cellView is null)
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);

            cellView.name = $"Cell {dataIndex}";
            cellView.RegisterListener(this);
            cellView.SetData(_recipeList[dataIndex]);
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return _recipeList.Count;
        }

        public void SetData(List<RecipeInfo> recipeList)
        {
            _recipeList = recipeList;
            scroller.ReloadData();
        }
        
        public void RegisterListener(RecipeCellView.IEventListener eventListener)
        {
            _eventListener = eventListener;
        }

        public void OnRecipeCellViewStarClick(RecipeCellView recipeCellView)
        {
            _eventListener?.OnRecipeCellViewStarClick(recipeCellView);
        }

        public void OnRecipeCellViewSubmitClick(RecipeCellView recipeCellView)
        {
            _eventListener?.OnRecipeCellViewSubmitClick(recipeCellView);
        }
    }
}
