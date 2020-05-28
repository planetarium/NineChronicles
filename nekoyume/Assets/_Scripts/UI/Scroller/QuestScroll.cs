using System;
using System.Collections.Generic;
using FancyScrollView;
using UnityEngine;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScroll : FancyScrollRect<QuestModel, QuestScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }

        [SerializeField]
        private GameObject cellPrefab = null;

        [SerializeField]
        private float cellSize;

        protected override GameObject CellPrefab => cellPrefab;

        protected override float CellSize => cellSize;

        public void UpdateData(IList<QuestModel> items)
        {
            UpdateContents(items);
        }
    }
}
