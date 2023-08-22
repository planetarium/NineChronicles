using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class EnhancementSelectedMaterialItemScroll : GridScroll<
        EnhancementInventoryItem,
        EnhancementSelectedMaterialItemScroll.ContextModel,
        EnhancementSelectedMaterialItemScroll.CellCellGroup>
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
    private EnhancementSelectedMaterialItemCell cellTemplate = null;

    public IObservable<EnhancementInventoryItem> OnClick => Context.OnClick;
    public IObservable<EnhancementInventoryItem> OnDoubleClick => Context.OnDoubleClick;

    protected override FancyCell<EnhancementInventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
