using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace Nekoyume.UI.Module.Arena.Board
{
    using UniRx;

    public class ArenaBoardPlayerScroll
        : FancyScrollRect<ArenaBoardPlayerItemData, ArenaBoardPlayerScrollContext>
    {
        [SerializeField]
        private UnityEngine.UI.Extensions.Scroller _scroller;

        [SerializeField]
        private GameObject _cellPrefab;

        protected override GameObject CellPrefab => _cellPrefab;

        [SerializeField]
        private float _cellSize;

        protected override float CellSize => _cellSize;

        private IList<ArenaBoardPlayerItemData> _data;

        public ArenaBoardPlayerItemData SelectedItemData => _data[Context.selectedIndex];

        private readonly Subject<int> _onSelectionChanged = new Subject<int>();

        public IObservable<int> OnSelectionChanged => _onSelectionChanged;

        private readonly Subject<int> _onClickChoice = new Subject<int>();

        public IObservable<int> OnClickChoice => _onClickChoice;

        public void SetData(IList<ArenaBoardPlayerItemData> data, int? index = null)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            _data = data;
            UpdateContents(_data);

            if (index.HasValue)
            {
                if (index.Value >= _data.Count)
                {
                    Debug.LogError($"Index out of range: {index.Value} >= {_data.Count}");
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
        }

        protected override void Initialize()
        {
            base.Initialize();

            Context.onClickChoice = _onClickChoice.OnNext;
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
