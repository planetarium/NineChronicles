using System;
using Nekoyume.TableData.CustomEquipmentCraft;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CustomOutfit : IDisposable
    {
        public readonly ReactiveProperty<CustomEquipmentCraftIconSheet.Row> IconRow = new();
        public readonly ReactiveProperty<bool> Dimmed = new(false);
        public readonly ReactiveProperty<bool> Selected = new(false);
        public readonly ReactiveProperty<bool> ActiveSelf = new(true);

        public readonly Subject<CustomOutfit> OnClick = new();

        public CustomOutfit(CustomEquipmentCraftIconSheet.Row row)
        {
            IconRow.Value = row;
        }

        public virtual void Dispose()
        {
            Dimmed.Dispose();
            ActiveSelf.Dispose();
            Selected.Dispose();
            OnClick.Dispose();
        }
    }
}
