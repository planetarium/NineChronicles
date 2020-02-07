using UnityEngine;
﻿using System;
using EnhancedUI.EnhancedScroller;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScrollerController : MonoBehaviour, IEnhancedScrollerDelegate, IScrollHandler
    {
        [Serializable]
        public struct Margin
        {
            public float top;
            public float bottom;
            public float left;
            public float right;
        }
        
        public EnhancedScroller scroller;
        public QuestCellView cellViewPrefab;
        public RectTransform marginRect; 
        public Margin margin;
        
        private List<QuestModel> _data;
        private float _cellViewHeight = 40f;

        #region Mono

        private void Awake()
        {
            scroller.Delegate = this;
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;

            marginRect.offsetMax += new Vector2(margin.right, margin.top);
            marginRect.offsetMin -= new Vector2(margin.left, margin.bottom);
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as QuestCellView;
            if (cellView is null)   
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            cellView.name = $"Cell {dataIndex}";
            cellView.onClickSubmitButton = RefreshScroll;
            cellView.SetData(_data[dataIndex]);
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return _data.Count;
        }

        public void SetData(List<QuestModel> dataList)
        {
            _data = dataList;

            scroller.ReloadData();
        }

        public void RefreshScroll()
        {
            _data.Sort(new QuestOrderComparer());
            SetData(_data);
        }

        public void OnScroll(PointerEventData eventData)
        {
            scroller.ScrollRect.OnScroll(eventData);
        }
    }
}
