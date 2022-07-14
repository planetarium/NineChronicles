using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRankScroll
        : FancyScrollRect<WorldBossRankItemData, WorldBossRankScrollContext>
    {
        [SerializeField]
        private UnityEngine.UI.Extensions.Scroller scroller;

        [SerializeField]
        private GameObject cellPrefab;

        [SerializeField]
        private float cellSize;

        private List<WorldBossRankItemData> _data;

        protected override GameObject CellPrefab => cellPrefab;
        protected override float CellSize => cellSize;
        protected override void Initialize()
        {
            base.Initialize();
            scroller.OnSelectionChanged(UpdateSelection);
        }

        public void SetData(List<WorldBossRankItemData> data, int? index = null)
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
                    Debug.LogError($"Index out of range: {index.Value} >= {_data.Count}");
                    return;
                }

                UpdateSelection(index.Value);
                scroller.JumpTo(index.Value);
            }
        }

        private void UpdateSelection(int index)
        {
            if (index == Context.selectedIndex)
            {
                return;
            }

            Context.selectedIndex = index;
            Refresh();
        }
    }
}
