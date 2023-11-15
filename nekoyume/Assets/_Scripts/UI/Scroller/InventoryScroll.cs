using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using Nekoyume.Helper;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.UI.Scroller
{
    public class InventoryScroll : GridScroll<
        InventoryItem,
        InventoryScroll.ContextModel,
        InventoryScroll.CellCellGroup>
    {
        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(cellSize.x, spacing, cellContainer.GetComponent<RectTransform>().rect.width + 40);
        }

        public class ContextModel : GridScrollDefaultContext
        {
            public Dictionary<int, InventoryCell> CellDictionary = new();
            public InventoryItem FirstItem;
            public readonly Subject<InventoryItem> OnClick = new();
            public readonly Subject<InventoryItem> OnDoubleClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                OnDoubleClick?.Dispose();
                CellDictionary.Clear();
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

        public bool TryGetCellByIndex(int index, out InventoryCell cell)
        {
            return Context.CellDictionary.TryGetValue(index, out cell);
        }

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
