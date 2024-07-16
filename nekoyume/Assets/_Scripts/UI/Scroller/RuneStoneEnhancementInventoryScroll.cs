using Nekoyume.Helper;
using Nekoyume.UI.Model;
using System;
using System.Reactive.Subjects;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class RuneStoneEnhancementInventoryScroll : GridScroll<
        RuneStoneEnhancementInventoryItem,
        RuneStoneEnhancementInventoryScroll.ContextModel,
        RuneStoneEnhancementInventoryScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public RuneStoneEnhancementInventoryItem FirstItem;
            public readonly Subject<RuneStoneEnhancementInventoryItem> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<RuneStoneEnhancementInventoryItem, ContextModel>
        {
        }

        [SerializeField]
        private RuneStoneEnhancementInventoryCell cellTemplate = null;

        public IObservable<RuneStoneEnhancementInventoryItem> OnClick => Context.OnClick;

        protected override FancyCell<RuneStoneEnhancementInventoryItem, ContextModel> CellTemplate => cellTemplate;

        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(cellSize.x, spacing, cellContainer.GetComponent<RectTransform>().rect.width + Util.GridScrollerAdjustCellCount);
        }
    }
}
