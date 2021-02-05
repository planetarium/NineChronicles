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
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Subject<InventoryCell> OnClick = new Subject<InventoryCell>();
            public readonly Subject<InventoryCell> OnDoubleClick = new Subject<InventoryCell>();

            public readonly Subject<int> RequestCellViewByIndex = new Subject<int>();

            public readonly Subject<InventoryCell> ResponseCellViewByIndex =
                new Subject<InventoryCell>();

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

        public IObservable<InventoryCell> GetCellViewByIndex(int index)
        {
            return Context.ResponseCellViewByIndex
                .Where(cell => cell.Index == index)
                .First()
                .DoOnSubscribe(() => Context.RequestCellViewByIndex.OnNext(index))
                .TimeoutFrame(1);
        }

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
