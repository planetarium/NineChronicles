using System;
using System.Collections.Generic;
using UnityEngine.UI.Extensions.EasingCore;
using UnityEngine.UI.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    // NOTE: GridScroll과 겹치는 로직이 있습니다. 이는 상속이 아닌 구성으로 일반화해야 하는데, 적당한 확장 함수를 구현하면 좋겠습니다.
    public abstract class RectScroll<TItemData, TContext> : FancyScrollRect<TItemData, TContext>
        where TItemData : class
        where TContext : RectScrollDefaultContext, IDisposable, new()
    {
        [SerializeField]
        protected GameObject cellPrefab = null;

        [SerializeField]
        private float cellSize = default;

        /// <summary>
        /// `FancyScrollRect`는 `Scroller.ScrollSensitivity`를 강제로 바꿔버립니다.
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

        protected override GameObject CellPrefab => cellPrefab;

        protected override float CellSize => cellSize;

        #region FancyScrollRect

        private float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;

        private float ViewportLength => ScrollLength - reuseCellMarginCount * 2f;

        private float PaddingHeadLength => (paddingHead - spacing * 0.5f) / (CellSize + spacing);

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

            if (fillWithNullToEmptyViewport)
            {
                var cellCount = (int) (Scroller.ViewportSize / CellSize);
                var addCount = math.max(0, cellCount - itemsSource.Count);
                for (var i = 0; i < addCount; i++)
                {
                    itemsSource.Add(null);
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
            var itemIndex = ItemsSource.IndexOf(itemData);
            if (itemIndex < 0)
            {
                return;
            }

            JumpTo(itemIndex, alignment);
        }

        public void ScrollTo(
            TItemData itemData,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            var itemIndex = ItemsSource.IndexOf(itemData);
            if (itemIndex < 0)
            {
                return;
            }

            ScrollTo(itemIndex, duration, ease, onComplete);
        }

        public void ScrollTo(
            TItemData itemData,
            float alignment,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            var itemIndex = ItemsSource.IndexOf(itemData);
            if (itemIndex < 0)
            {
                return;
            }

            ScrollTo(itemIndex, alignment, duration, ease, onComplete);
        }

        public void ScrollTo(
            int index,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            ScrollTo(
                index,
                GetAlignmentToIncludeWithinViewport(index),
                duration,
                ease,
                onComplete);
        }

        public void ScrollTo(
            int index,
            float alignment,
            float duration = .1f,
            Ease ease = Ease.Linear,
            System.Action onComplete = null)
        {
            ScrollTo(index, duration, ease, alignment, onComplete);
        }

        #endregion

        #region Getter

        private bool TryGetCellIndex(TItemData itemData, out int itemIndex)
        {
            for (var i = 0; i < ItemsSource.Count; i++)
            {
                var itemSource = ItemsSource[i];
                if (!itemSource.Equals(itemData))
                {
                    continue;
                }

                itemIndex = i;
                return true;
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

        protected override void UpdateContents(IList<TItemData> items)
        {
            base.UpdateContents(items);
            Scroller.ScrollSensitivity = forcedScrollSensitivity;
        }

        // Polymorphic impletmentation of the base.AdjustCellIntervalAndScrollOffset() method.
        protected void AdjustCellIntervalAndScrollOffset(float viewportSize)
        {
            var totalSize = viewportSize + (CellSize + spacing) * (1f + reuseCellMarginCount * 2f);
            cellInterval = (CellSize + spacing) / totalSize;
            scrollOffset = cellInterval * (1f + reuseCellMarginCount);
        }

        #endregion

        private float GetAlignmentToIncludeWithinViewport(TItemData itemData)
        {
            return TryGetCellIndex(itemData, out var cellIndex)
                ? GetAlignmentToIncludeWithinViewport(cellIndex)
                : 0f;
        }

        private float GetAlignmentToIncludeWithinViewport(int index)
        {
            if (index < currentPosition)
            {
                return 0f;
            }

            var length = ViewportLength - PaddingHeadLength - 1f;

            if (index > currentPosition + length)
            {
                return 1f;
            }

            return (index - currentPosition) / length;
        }
    }
}
