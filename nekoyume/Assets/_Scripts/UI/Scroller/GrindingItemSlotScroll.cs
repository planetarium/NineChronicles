using System;
using System.Reactive.Subjects;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class GrindingItemSlotScroll : GridScroll<
        InventoryItem,
        GrindingItemSlotScroll.ContextModel,
        GrindingItemSlotScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Subject<InventoryItem> OnClick = new Subject<InventoryItem>();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<InventoryItem, ContextModel>
        {
        }

        [SerializeField] private GrindingItemSlotCell cellTemplate;

        public IObservable<InventoryItem> OnClick => Context.OnClick;

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;

        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(
                cellSize.x, spacing,
                cellContainer.GetComponent<RectTransform>().rect.width +
                Util.GridScrollerAdjustCellCount);
        }
    }
}
