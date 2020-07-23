using System;
using FancyScrollView;
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
        public class ContextModel : GridScrollDefaultContext, IDisposable
        {
            public readonly Subject<InventoryCell> OnClick = new Subject<InventoryCell>();
            public readonly Subject<InventoryCell> OnDoubleClick = new Subject<InventoryCell>();

            public void Dispose()
            {
                OnClick?.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<InventoryItem, ContextModel>
        {
        }

        [SerializeField]
        private InventoryCell cellTemplate = null;

        public IObservable<InventoryCell> OnClick => Context.OnClick;

        public IObservable<InventoryCell> OnDoubleClick => Context.OnDoubleClick;

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
