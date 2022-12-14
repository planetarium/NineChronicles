using System;
using System.Reactive.Subjects;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class RuneListScroll: GridScroll<
        RuneListItem,
        RuneListScroll.ContextModel,
        RuneListScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
            public RuneListItem FirstItem;
            public readonly Subject<RuneItem> OnClick = new();

            public override void Dispose()
            {
                OnClick?.Dispose();
                base.Dispose();
            }
        }

        public class CellCellGroup : GridCellGroup<RuneListItem, ContextModel>
        {
        }

        [SerializeField]
        private RuneListCell cellTemplate = null;

        public IObservable<RuneItem> OnClick => Context.OnClick;

        public bool TryGetFirstItem(out RuneListItem cell)
        {
            cell = Context.FirstItem;
            return cell != null;
        }

        protected override FancyCell<RuneListItem, ContextModel> CellTemplate => cellTemplate;
    }
}
