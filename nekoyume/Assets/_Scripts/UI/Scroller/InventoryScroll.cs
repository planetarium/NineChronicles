using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryScroll : GridScroll<
        InventoryItem,
        InventoryScroll.ContextModel,
        InventoryScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public InventoryItem FirstItem;
            public readonly Subject<InventoryItem> OnClick = new Subject<InventoryItem>();
            public readonly Subject<InventoryItem> OnDoubleClick = new Subject<InventoryItem>();

            public override void Dispose()
            {
                OnClick?.Dispose();
                OnDoubleClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<InventoryItem, ContextModel>
        {
        }

        [SerializeField]
        private InventoryCell cellTemplate = null;

        public IObservable<InventoryItem> OnClick => Context.OnClick;
        public IObservable<InventoryItem> OnDoubleClick => Context.OnDoubleClick;

        public bool TryGetFirstItem(out InventoryItem cell)
        {
            cell = Context.FirstItem;

            return cell != null;
        }

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
