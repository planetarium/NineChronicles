using System;
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
        public readonly Subject<EnhancementInventoryItem> OnClick = new Subject<EnhancementInventoryItem>();
        public readonly Subject<EnhancementInventoryItem> OnDoubleClick = new Subject<EnhancementInventoryItem>();

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

    protected override FancyCell<EnhancementInventoryItem, ContextModel> CellTemplate => cellTemplate;
        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(cellSize.x, spacing, cellContainer.GetComponent<RectTransform>().rect.width);
        }
    }
}
