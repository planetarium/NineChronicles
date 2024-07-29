using System;
using Nekoyume.Helper;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Pet;
using UniRx;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class PetSlotScroll : GridScroll<
        PetSlotViewModel,
        PetSlotScroll.ContextModel,
        PetSlotScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public PetSlotViewModel FirstItem;
            public readonly Subject<PetSlotViewModel> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<PetSlotViewModel, ContextModel>
        {
        }

        [SerializeField]
        private PetSlotCell cellTemplate;

        public IObservable<PetSlotViewModel> OnClick => Context.OnClick;

        public bool TryGetFirstItem(out PetSlotViewModel cell)
        {
            cell = Context.FirstItem;
            return cell != null;
        }

        protected override FancyCell<PetSlotViewModel, ContextModel> CellTemplate => cellTemplate;

        protected override void Initialize()
        {
            base.Initialize();
            startAxisCellCount = Util.GetGridItemCount(cellSize.x, spacing, cellContainer.GetComponent<RectTransform>().rect.width);
        }
    }
}
