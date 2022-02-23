using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

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

        public override void Dispose()
        {
            OnClick?.Dispose();
            base.Dispose();
        }
    }

    public class CellCellGroup : GridCellGroup<EnhancementInventoryItem, ContextModel>
    {
    }

    [SerializeField]
    private EnhancementInventoryCell cellTemplate = null;

    public IObservable<EnhancementInventoryItem> OnClick => Context.OnClick;

    protected override FancyCell<EnhancementInventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
