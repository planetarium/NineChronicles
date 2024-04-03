using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

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

        private List<ArenaBoardPlayerItemData> _data;

        public IReadOnlyList<ArenaBoardPlayerItemData> Data => _data;

        public ArenaBoardPlayerItemData SelectedItemData => _data[Context.selectedIndex];

        private readonly Subject<int> _onSelectionChanged = new Subject<int>();

        public IObservable<int> OnSelectionChanged => _onSelectionChanged;

        private readonly Subject<int> _onClickCharacterView = new Subject<int>();

        public IObservable<int> OnClickCharacterView => _onClickCharacterView;

        private readonly Subject<int> _onClickChoice = new Subject<int>();

        public IObservable<int> OnClickChoice => _onClickChoice;

        public void SetData(List<ArenaBoardPlayerItemData> data, int? index = null)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            _data = data;
            UpdateContents(_data);
            if (_data.Count == 0)
            {
                return;
            }

            if (index.HasValue)
            {
                if (index.Value >= _data.Count)
                {
                    NcDebug.LogError($"Index out of range: {index.Value} >= {_data.Count}");
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

            Context.onClickCharacterView = _onClickCharacterView.OnNext;
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
