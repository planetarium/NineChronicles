using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentInventoryViewScroll : GridScroll<
        EquipmentInventoryViewModel,
        EquipmentInventoryViewScroll.ContextModel,
        EquipmentInventoryViewScroll.CellCellGroup>
    {
    public class ContextModel : GridScrollDefaultContext
    {
        public readonly Subject<EquipmentInventoryViewModel> OnClick = new Subject<EquipmentInventoryViewModel>();

        public override void Dispose()
        {
            OnClick?.Dispose();
            base.Dispose();
        }
    }

    public class CellCellGroup : GridCellGroup<EquipmentInventoryViewModel, ContextModel>
    {
    }

    [SerializeField]
    private EquipmentInventoryViewCell cellTemplate = null;

    public IObservable<EquipmentInventoryViewModel> OnClick => Context.OnClick;

    protected override FancyCell<EquipmentInventoryViewModel, ContextModel> CellTemplate => cellTemplate;
    }
}
