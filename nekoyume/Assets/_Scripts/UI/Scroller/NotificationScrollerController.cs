using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class NotificationScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public readonly Subject<int> onRequestToRemoveModelByIndex = new Subject<int>();

        public ScrollRect scrollRect;
        public EnhancedScroller enhancedScroller;
        public NotificationCellView cellViewPrefab;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private RectTransform _scrollRectTransform;
        private float _cellViewHeight;
        private float _layoutGroupPadding;
        private float _layoutGroupSpacing;


        public IReadOnlyReactiveCollection<NotificationCellView.Model> SharedModel { get; private set; }

        private void Awake()
        {
            enhancedScroller.Delegate = this;

            _scrollRectTransform = scrollRect.GetComponent<RectTransform>();
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        private void Start()
        {
            var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
            var layoutGroupPadding = layoutGroup.padding;
            _layoutGroupPadding = layoutGroupPadding.top + layoutGroupPadding.bottom;
            _layoutGroupSpacing = layoutGroup.spacing;
            scrollRect.content.SetAnchor(AnchorPresetType.HorStretchBottom);
            scrollRect.content.SetPivot(PivotPresetType.BottomCenter);
            var contentSizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void SetModel(IReadOnlyReactiveCollection<NotificationCellView.Model> model)
        {
            _disposablesForModel.DisposeAllAndClear();
            SharedModel = model;
            SharedModel.ObserveAdd()
                .Subscribe(_ =>
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Notification);
                    ReloadDataWithFactor(true);
                })
                .AddTo(_disposablesForModel);
            SharedModel.ObserveRemove()
                .Subscribe(_ => ReloadDataWithFactor(false))
                .AddTo(_disposablesForModel);
            SharedModel.ObserveReset()
                .Subscribe(_ => ReloadDataWithFactor(true))
                .AddTo(_disposablesForModel);

            enhancedScroller.ReloadData();
        }

        #region IEnhancedScrollerDelegate

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return SharedModel.Count;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            if (!(scroller.GetCellViewAtDataIndex(dataIndex) is NotificationCellView cellView))
            {
                return _cellViewHeight;
            }

            return cellView.RectTransform.rect.height;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            if (dataIndex >= SharedModel.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(dataIndex));
            }

            if (!(scroller.GetCellView(cellViewPrefab) is NotificationCellView cellView))
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            cellView.name = $"Cell {cellIndex}";
            cellView.onClickSubmitButton = SubscribeOnClick;
            cellView.SetModel(SharedModel[dataIndex]);

            return cellView;
        }

        #endregion
        
        private void ReloadDataWithFactor(bool moveToLast)
        {
            var count = SharedModel.Count;
            if (count == 0)
            {
                enhancedScroller.ReloadData();

                return;
            }

            var contentHeight = (count - 1) * (_cellViewHeight + _layoutGroupSpacing)
                                + _cellViewHeight + _layoutGroupPadding;
            var scrollSize = contentHeight - _scrollRectTransform.rect.height;
            if (scrollSize <= 0f)
            {
                enhancedScroller.ReloadData();

                return;
            }

            if (moveToLast)
            {
                enhancedScroller.ReloadData(1f);

                return;
            }
            
            var scrollItemPositionFactor = (_cellViewHeight + _layoutGroupSpacing) / scrollSize;
            var scrollPositionFactor = 1f - (1f - (enhancedScroller.ScrollPosition / scrollSize));
            enhancedScroller.ReloadData(scrollPositionFactor - scrollItemPositionFactor);
        }

        private void SubscribeOnClick(NotificationCellView view)
        {
            onRequestToRemoveModelByIndex.OnNext(view.dataIndex);
            view.SharedModel.submitAction?.Invoke();
        }
    }
}
