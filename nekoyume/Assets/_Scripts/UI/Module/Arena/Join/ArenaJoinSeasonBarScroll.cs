using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace Nekoyume.UI.Module.Arena.Join
{
    using UniRx;

    public class ArenaJoinSeasonBarScroll :
        FancyScrollView<ArenaJoinSeasonBarItemData, ArenaJoinSeasonBarScrollContext>
    {
        [SerializeField] private UnityEngine.UI.Extensions.Scroller _scroller;

        [SerializeField] private GameObject _cellPrefab;

        [SerializeField] private GameObject[] scrollBarParents;

        protected override GameObject CellPrefab => _cellPrefab;

        private readonly Subject<int> _onSelectionChanged = new Subject<int>();
        public IObservable<int> OnSelectionChanged => _onSelectionChanged;

        public void SetData(IList<ArenaJoinSeasonBarItemData> data, int? index = null)
        {
            cellInterval = 1f / data.Count;
            UpdateContents(data);
            _scroller.SetTotalCount(data.Count);
            if (data.Count == 0)
            {
                return;
            }

            var scrollBarIndex = Math.Clamp(data.Count, 6, 8) - 6;
            for (int i = 0; i < scrollBarParents.Length; i++)
            {
                scrollBarParents[i].SetActive(i == scrollBarIndex);
            }

            if (index.HasValue)
            {
                if (index.Value >= data.Count)
                {
                    NcDebug.LogError($"Index out of range: {index.Value} >= {data.Count}");
                    return;
                }

                UpdateSelection(index.Value, true);
                _scroller.JumpTo(index.Value);
            }
        }

        public void SelectCell(int index, bool invokeEvents)
        {
            if (index < 0 ||
                index >= ItemsSource.Count ||
                index == Context.selectedIndex)
            {
                return;
            }

            UpdateSelection(index, invokeEvents);
            _scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }

        protected override void Initialize()
        {
            base.Initialize();

            _scroller.OnValueChanged(UpdatePosition);
            _scroller.OnSelectionChanged(index => UpdateSelection(index, true));
        }

        private void UpdateSelection(int index, bool invokeEvents)
        {
            if (index == Context.selectedIndex)
            {
                return;
            }

            Context.selectedIndex = index;
            Refresh();

            if (invokeEvents)
            {
                _onSelectionChanged.OnNext(Context.selectedIndex);
            }
        }
    }
}
