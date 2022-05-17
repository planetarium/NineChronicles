using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.UI.Module.Arena;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaJoin : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaJoinSO _so;
#endif

        [SerializeField]
        private ArenaJoinSeasonScroll _scroll;

        [SerializeField]
        private ArenaJoinSeasonBarScroll _barScroll;

        [SerializeField]
        private int _barPointCount;

        [SerializeField]
        private ArenaJoinSeasonInfo _info;

        [SerializeField]
        private ArenaJoinBottomButtons _buttons;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        
        protected override void Awake()
        {
            base.Awake();
            
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            InitializeScrolls(_disposables);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private IList<ArenaJoinSeasonItemData> GetScrollData()
        {
            IList<ArenaJoinSeasonItemData> scrollData = null;
#if UNITY_EDITOR
            scrollData = _useSo && _so
                ? _so.ScrollData
                : GetScrollDataFromChain();
#else
            scrollData = GetScrollDataFromChain();
#endif

            return scrollData;
        }

        private void InitializeScrolls(IList<IDisposable> disposables)
        {
            var scrollData = GetScrollData();
            var selectedIndex = Random.Range(0, 2);
            _scroll.SetData(scrollData, selectedIndex);
            var barIndexOffset = (int)math.ceil(_barPointCount / 2f) - 1;
            _barScroll.SetData(
                GetBarScrollData(barIndexOffset),
                ReverseScrollIndex(selectedIndex));
            
            // NOTE: Scroll events should subscribe after set data. 
            _scroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(index =>
                    _barScroll.SelectCell(index, false))
                .AddTo(disposables);
            _barScroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(index =>
                    _scroll.SelectCell(index, false))
                .AddTo(disposables);
        }

        private IList<ArenaJoinSeasonItemData> GetScrollDataFromChain()
        {
            return new List<ArenaJoinSeasonItemData>();
        }

        private IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barIndexOffset)
        {
            var cellCount = _barPointCount;
            return Enumerable.Range(0, cellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset,
                })
                .ToList();
        }

        private int ReverseScrollIndex(int scrollIndex) =>
            _barPointCount - scrollIndex - 1;
    }
}
