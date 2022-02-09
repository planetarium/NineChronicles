using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryViewScroll : GridScroll<
        InventoryItemViewModel,
        InventoryViewScroll.ContextModel,
        InventoryViewScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public InventoryItemViewModel FirstItem;
            public readonly Subject<InventoryItemViewModel> OnClick = new Subject<InventoryItemViewModel>();
            public readonly Subject<InventoryItemViewModel> OnDoubleClick = new Subject<InventoryItemViewModel>();

            public override void Dispose()
            {
                OnClick?.Dispose();
                OnDoubleClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<InventoryItemViewModel, ContextModel>
        {
        }

        [SerializeField]
        private InventoryViewCell cellTemplate = null;

        public IObservable<InventoryItemViewModel> OnClick => Context.OnClick;
        public IObservable<InventoryItemViewModel> OnDoubleClick => Context.OnDoubleClick;

        public bool TryGetFirstItem(out InventoryItemViewModel cell)
        {
            cell = Context.FirstItem;

            return cell != null;
        }

        protected override FancyCell<InventoryItemViewModel, ContextModel> CellTemplate => cellTemplate;
    }
}
