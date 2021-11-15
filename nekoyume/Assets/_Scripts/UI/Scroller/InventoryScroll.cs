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
            public readonly Subject<InventoryCell> OnClick = new Subject<InventoryCell>();
            public readonly Subject<InventoryCell> OnDoubleClick = new Subject<InventoryCell>();

            public InventoryCell FirstCell;

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

        public IObservable<InventoryCell> OnClick => Context.OnClick;

        public IObservable<InventoryCell> OnDoubleClick => Context.OnDoubleClick;

        public bool TryGetFirstCell(out InventoryCell cell)
        {
            cell = Context.FirstCell;

            return cell != null;
        }

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
