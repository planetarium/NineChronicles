using System;
using UnityEngine.UI.Extensions;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentInventoryScroll : GridScroll<
        EquipmentInventoryItem,
        EquipmentInventoryScroll.ContextModel,
        EquipmentInventoryScroll.CellCellGroup>
    {
    public class ContextModel : GridScrollDefaultContext
    {
        public readonly Subject<EquipmentInventoryItem> OnClick = new Subject<EquipmentInventoryItem>();

        public override void Dispose()
        {
            OnClick?.Dispose();
            base.Dispose();
        }
    }

    public class CellCellGroup : GridCellGroup<EquipmentInventoryItem, ContextModel>
    {
    }

    [SerializeField]
    private EquipmentInventoryCell cellTemplate = null;

    public IObservable<EquipmentInventoryItem> OnClick => Context.OnClick;

    protected override FancyCell<EquipmentInventoryItem, ContextModel> CellTemplate => cellTemplate;
    }
}
