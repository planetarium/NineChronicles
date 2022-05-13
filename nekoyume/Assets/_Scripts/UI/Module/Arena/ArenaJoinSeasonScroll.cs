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

        public void SetData(IList<ArenaJoinSeasonItemData> itemsSource)
        {
            UpdateContents(itemsSource);
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

        public void SelectNextCell() => SelectCell(Context.SelectedIndex + 1);

        public void SelectPrevCell() => SelectCell(Context.SelectedIndex - 1);

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
