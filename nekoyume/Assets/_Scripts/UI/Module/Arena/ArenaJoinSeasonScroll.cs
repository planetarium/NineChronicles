using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace Nekoyume.UI.Module.Arena
{
    using UniRx;

    public class ArenaJoinSeasonScroll :
        FancyScrollView<ArenaJoinSeasonItemData, ArenaJoinSeasonScrollContext>
    {
        [SerializeField]
        private UnityEngine.UI.Extensions.Scroller _scroller;

        [SerializeField]
        private GameObject _cellPrefab;

        protected override GameObject CellPrefab => _cellPrefab;

        private readonly Subject<int> _onSelectionChanged = new Subject<int>();
        public IObservable<int> OnSelectionChanged => _onSelectionChanged;

        public void SetData(IList<ArenaJoinSeasonItemData> data, int? index = null)
        {
            UpdateContents(data);
            _scroller.SetTotalCount(data.Count);

            if (index.HasValue)
            {
                if (index.Value >= data.Count)
                {
                    Debug.LogError($"Index out of range: {index.Value} >= {data.Count}");
                    return;
                }

                UpdateSelection(index.Value);
                _scroller.JumpTo(index.Value);
            }
        }

        public void SelectCell(int index)
        {
            if (index < 0 ||
                index >= ItemsSource.Count ||
                index == Context.SelectedIndex)
            {
                return;
            }

            UpdateSelection(index);
            _scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }

        protected override void Initialize()
        {
            base.Initialize();

            Context.OnCellClicked = SelectCell;
            _scroller.OnValueChanged(UpdatePosition);
            _scroller.OnSelectionChanged(UpdateSelection);
        }

        private void UpdateSelection(int index)
        {
            if (index == Context.SelectedIndex)
            {
                return;
            }

            Context.SelectedIndex = index;
            Refresh();

            _onSelectionChanged.OnNext(Context.SelectedIndex);
        }
    }
}
