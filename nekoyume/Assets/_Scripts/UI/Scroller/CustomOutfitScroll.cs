using System;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class CustomOutfitScroll : GridScroll<
        CustomOutfit,
        CustomOutfitScroll.ContextModel,
        CustomOutfitScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Subject<CustomOutfit> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<CustomOutfit, ContextModel>
        {
        }

        [SerializeField]
        private CustomOutfitCell cellTemplate;

        public IObservable<CustomOutfit> OnClick => Context.OnClick;

        protected override FancyCell<CustomOutfit, ContextModel> CellTemplate => cellTemplate;
    }
}
