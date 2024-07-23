using System;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class SimpleItemScroll : GridScroll<
        Item,
        SimpleItemScroll.ContextModel,
        SimpleItemScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public readonly Subject<Item> OnClick = new();
            public readonly Subject<Item> OnDoubleClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                OnDoubleClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<Item, ContextModel>
        {
        }

        [SerializeField]
        private SimpleItemCell cellTemplate;

        public IObservable<Item> OnClick => Context.OnClick;
        public IObservable<Item> OnDoubleClick => Context.OnDoubleClick;

        protected override FancyCell<Item, ContextModel> CellTemplate => cellTemplate;
    }
}
