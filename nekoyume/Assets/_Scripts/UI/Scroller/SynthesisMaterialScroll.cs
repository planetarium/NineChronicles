using System;
using System.Reactive.Subjects;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class SynthesisMaterialScroll : GridScroll<
        InventoryItem,
        SynthesisMaterialScroll.ContextModel,
        SynthesisMaterialScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Subject<InventoryItem> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<InventoryItem, ContextModel>
        {
        }

        [SerializeField] private SynthesisMaterialCell cellTemplate;

        public IObservable<InventoryItem> OnClick => Context.OnClick;

        protected override FancyCell<InventoryItem, ContextModel> CellTemplate => cellTemplate;

        protected override void Initialize()
        {
            // ClearContents();
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(
                cellSize.x, spacing,
                cellContainer.GetComponent<RectTransform>().rect.width +
                Util.GridScrollerAdjustCellCount);
        }

        private void ClearContents()
        {
            foreach (Transform child in cellContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
