using System;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using Nekoyume.Helper;

namespace Nekoyume.UI.Scroller
{
    public class EnhancementInventoryScroll : GridScroll<
        EnhancementInventoryItem,
        EnhancementInventoryScroll.ContextModel,
        EnhancementInventoryScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Dictionary<int, EnhancementInventoryCell> CellDictionary = new();
            public readonly Subject<EnhancementInventoryItem> OnClick = new();
            public readonly Subject<EnhancementInventoryItem> OnDoubleClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                OnDoubleClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<EnhancementInventoryItem, ContextModel>
        {
        }

        [SerializeField]
        private EnhancementInventoryCell cellTemplate = null;

        public IObservable<EnhancementInventoryItem> OnClick => Context.OnClick;
        public IObservable<EnhancementInventoryItem> OnDoubleClick => Context.OnDoubleClick;

        public bool TryGetCellByIndex(int index, out EnhancementInventoryCell cell)
        {
            return Context.CellDictionary.TryGetValue(index, out cell);
        }

        protected override FancyCell<EnhancementInventoryItem, ContextModel> CellTemplate => cellTemplate;

        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(cellSize.x, spacing, cellContainer.GetComponent<RectTransform>().rect.width + Util.GridScrollerAdjustCellCount);
        }
    }
}
