using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions.EasingCore;
using UnityEngine.UI.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    // NOTE: RectScroll과 겹치는 로직이 있습니다. 이는 상속이 아닌 구성으로 일반화해야 하는데, 적당한 확장 함수를 구현하면 좋겠습니다.
    public abstract class GridScroll<TItemData, TContext, TCellGroup> :
        FancyGridView<TItemData, TContext>
        where TItemData : class
        where TContext : GridScrollDefaultContext, IDisposable, new()
        where TCellGroup : GridCellGroup<TItemData, TContext>
    {
        /// <summary>
        /// `FancyGridView`는 `Scroller.ScrollSensitivity`를 강제로 바꿔버립니다.
        /// 그렇기 때문에 사용자가 설정한 스크롤 감도가 무시됩니다.
        /// 이를 강제로 할당하기 윈해서 각 스크롤에서 원하는 감도를 받습니다.
        /// </summary>
        [SerializeField]
        private float forcedScrollSensitivity = 20f;

        /// <summary>
        /// Viewport 영역이 다 채워지지 않는 수의 아이템을 표시할 경우에 기본적으로 null을 채워서 Cell 쪽에서 처리할 수 있도록 합니다.
        /// </summary>
        [SerializeField]
        private bool fillWithNullToEmptyViewport = default;

        [SerializeField]
        private bool fillWithNullToEmptyCellGroup;

        [SerializeField]
        private int fillWithNullToMinimumCount = 0;

        protected abstract FancyCell<TItemData, TContext> CellTemplate { get; }

        protected override void SetupCellTemplate() => Setup<TCellGroup>(CellTemplate);

        #region FancyScrollRect

        private float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;

        private float ViewportLength => ScrollLength - reuseCellMarginCount * 2f;

        private float PaddingHeadLength => (paddingHead - spacing * 0.5f) / (CellSize + spacing);

        private float MaxScrollPosition =>
            ItemsSource.Count
            - ScrollLength
            + reuseCellMarginCount * 2f
            + (paddingHead + paddingTail - spacing) / (CellSize + spacing);

        #endregion

        #region MonoBehaviour

        private void Reset()
        {
            Scroller.Elasticity = .2f;
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        private void OnDestroy()
        {
            Context.Dispose();
        }

        #endregion

        #region Control

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(IEnumerable<TItemData> items, bool jumpToFirst = default)
        {
            UpdateData(items, jumpToFirst);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateData(IEnumerable<TItemData> items, bool jumpToFirst = default)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            var itemsSource = new List<TItemData>(items);

            if(fillWithNullToMinimumCount > itemsSource.Count)
            {
                var addCount = math.max(0, fillWithNullToMinimumCount - itemsSource.Count);
                for (var i = 0; i < addCount; i++)
                {
                    itemsSource.Add(null);
                }
            }

            if (fillWithNullToEmptyViewport)
            {
                var cellGroupCount = (int) (Scroller.ViewportSize / CellSize);
                var cellCountInGroup = Context.GetGroupCount();
                var cellCount = cellGroupCount * cellCountInGroup;
                var addCount = math.max(0, cellCount - itemsSource.Count);
                for (var i = 0; i < addCount; i++)
                {
                    itemsSource.Add(null);
                }
            }

            if (fillWithNullToEmptyCellGroup)
            {
                var cellCountInGroup = Context.GetGroupCount();
                var remain = itemsSource.Count % cellCountInGroup;
                if (remain != 0)
                {
                    var addCount = cellCountInGroup - remain;
                    for (var i = 0; i < addCount; i++)
                    {
                        itemsSource.Add(null);
                    }
                }
            }

            UpdateContents(itemsSource);

            if (!jumpToFirst)
            {
                return;
            }

            JumpTo(0);
        }

        public void ClearData()
        {
            UpdateContents(new List<TItemData>());
        }

        public void JumpTo(TItemData itemData)
        {
            JumpTo(itemData, GetAlignmentToIncludeWithinViewport(itemData));
        }

        public void JumpTo(TItemData itemData, float alignment)
        {
            if (!TryGetCellIndex(itemData, out var itemIndex))
            {
                return;
            }

            JumpTo(itemIndex, alignment);
        }

        public void RawJumpTo(int itemIndex, float alignment = 0.5f)
        {
            JumpTo(itemIndex, alignment);
        }

        public void ScrollTo(
            TItemData itemData,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            ScrollTo(
                itemData,
                GetAlignmentToIncludeWithinViewport(itemData),
                duration,
                ease,
                onComplete);
        }

        public void ScrollTo(
            TItemData itemData,
            float alignment,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            if (!TryGetCellIndex(itemData, out var itemIndex))
            {
                return;
            }

            base.ScrollTo(itemIndex, duration, ease, alignment, onComplete);
        }

        #endregion

        #region Getter

        private bool TryGetCellGroupIndex(TItemData itemData, out int cellGroupIndex)
        {
            for (var i = 0; i < ItemsSource.Count; i++)
            {
                var cellGroupData = ItemsSource[i];
                if (!cellGroupData.Contains(itemData))
                {
                    continue;
                }

                cellGroupIndex = i;
                return true;
            }

            cellGroupIndex = default;
            return false;
        }

        private bool TryGetCellIndex(TItemData itemData, out int itemIndex)
        {
            var cellCountInGroup = Context.GetGroupCount();
            for (var i = 0; i < ItemsSource.Count; i++)
            {
                var itemGroupData = ItemsSource[i];
                if (!itemGroupData.Contains(itemData))
                {
                    continue;
                }

                for (var j = 0; j < itemGroupData.Length; j++)
                {
                    if (!itemGroupData[j].Equals(itemData))
                    {
                        continue;
                    }

                    itemIndex = i * cellCountInGroup + j;
                    return true;
                }
            }

            itemIndex = default;
            return false;
        }

        #endregion

        #region Override

        protected override void Refresh()
        {
            base.Refresh();
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        protected override void Relayout()
        {
            base.Relayout();
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        protected override void UpdateContents(IList<TItemData[]> items)
        {
            base.UpdateContents(items);
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        #endregion

        private float GetAlignmentToIncludeWithinViewport(TItemData itemData)
        {
            if (!TryGetCellGroupIndex(itemData, out var cellGroupIndex))
            {
                return 0f;
            }

            if (cellGroupIndex < currentPosition)
            {
                return 0f;
            }

            var length = ViewportLength - PaddingHeadLength - 1f;

            if (cellGroupIndex > currentPosition + length)
            {
                return 1f;
            }

            return (cellGroupIndex - currentPosition) / length;
        }
    }
}
