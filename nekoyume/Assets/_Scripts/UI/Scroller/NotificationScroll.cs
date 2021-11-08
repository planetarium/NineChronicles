using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions.EasingCore;
using UnityEngine.UI.Extensions;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class NotificationScroll : FancyScrollView<
        NotificationCell.ViewModel,
        NotificationScroll.DefaultContext>
    {
        public class DefaultContext : IDisposable
        {
            #region From cell

            public readonly Subject<NotificationCell> OnCompleteOfAddAnimation =
                new Subject<NotificationCell>();

            public readonly Subject<NotificationCell> OnCompleteOfRemoveAnimation =
                new Subject<NotificationCell>();

            #endregion

            #region From scroll

            public readonly Subject<(int index, NotificationCell.ViewModel viewModel)>
                PlayRemoveAnimation = new Subject<(int, NotificationCell.ViewModel)>();

            #endregion

            public Func<float> CalculateScrollSize { get; set; }

            public void Dispose()
            {
                OnCompleteOfAddAnimation?.Dispose();
                OnCompleteOfRemoveAnimation?.Dispose();
                PlayRemoveAnimation?.Dispose();
            }
        }

        [SerializeField]
        private UnityEngine.UI.Extensions.Scroller scroller = null;

        [SerializeField]
        private GameObject cellPrefab = null;

        [SerializeField]
        private float cellSize = default;

        protected override GameObject CellPrefab => cellPrefab;

        public IObservable<NotificationCell> OnCompleteOfAddAnimation =>
            Context.OnCompleteOfAddAnimation;

        public IObservable<NotificationCell> OnCompleteOfRemoveAnimation =>
            Context.OnCompleteOfRemoveAnimation;

        #region FancyScrollRect

        private float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;

        private float MaxScrollPosition => ItemsSource.Count - ScrollLength;

        #endregion

        #region Lifecycle

        protected override void Initialize()
        {
            base.Initialize();

            Context.CalculateScrollSize = () =>
            {
                var interval = cellSize;
                return scroller.ViewportSize + interval;
            };
            Context.OnCompleteOfRemoveAnimation
                .Subscribe(_ => UpdateContents(ItemsSource))
                .AddTo(gameObject);

            AdjustCellIntervalAndScrollOffset();
            scroller.OnValueChanged(UpdatePosition);
        }

        private void OnDestroy()
        {
            Context.Dispose();
        }

        #endregion

        #region Control

        public void UpdateData(
            IReadOnlyList<NotificationCell.ViewModel> items,
            bool jumpToFirst = default)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            var itemsSource = new List<NotificationCell.ViewModel>(items);

            UpdateContents(itemsSource);
            scroller.SetTotalCount(itemsSource.Count);

            if (jumpToFirst &&
                items.Any())
            {
                scroller.JumpTo(0);
            }
        }

        public void ClearData()
        {
            UpdateContents(new List<NotificationCell.ViewModel>());
        }

        public void ScrollTo(
            NotificationCell.ViewModel itemData,
            float duration,
            Ease easing,
            System.Action onComplete = null)
        {
            if (!TryGetCellIndex(itemData, out var index))
            {
                return;
            }

            ScrollTo(index, duration, easing, onComplete);
        }

        public void ScrollTo(
            float index,
            float duration,
            Ease easing,
            System.Action onComplete = null)
        {
            scroller.ScrollTo(index, duration, easing, onComplete);
        }

        public IObservable<NotificationScroll> PlayCellRemoveAnimation(int index)
        {
            if (index < 0 ||
                index >= ItemsSource.Count)
            {
                return Observable.Empty(this);
            }

            var observable = Context.OnCompleteOfRemoveAnimation
                .Where(cell => cell.Index == index)
                .First()
                .Select(cell => this);
            Context.PlayRemoveAnimation.OnNext((index, ItemsSource[index]));
            return observable;
        }

        #endregion

        #region Getter

        private bool TryGetCellIndex(NotificationCell.ViewModel itemData, out int itemIndex)
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
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Refresh();
        }

        protected override void Relayout()
        {
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Relayout();
        }

        #endregion

        #region FancyScrollRect

        private void RefreshScroller()
        {
            scroller.Position = ToScrollerPosition(currentPosition);
        }

        private float ToScrollerPosition(float position)
        {
            return position / MaxScrollPosition * Mathf.Max(ItemsSource.Count - 1, 1);
        }

        private void AdjustCellIntervalAndScrollOffset()
        {
            var totalSize = scroller.ViewportSize + cellSize;
            cellInterval = cellSize / totalSize;
            scrollOffset = cellInterval;
        }

        #endregion
    }
}
