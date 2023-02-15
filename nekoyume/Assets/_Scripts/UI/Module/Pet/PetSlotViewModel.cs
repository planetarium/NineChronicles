using Nekoyume.Model.State;
using Nekoyume.TableData.Pet;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module.Pet
{
    public class PetSlotViewModel
    {
        public PetSheet.Row PetRow { get; }
        public readonly ReactiveProperty<bool> Empty;
        public readonly ReactiveProperty<bool> EquippedIcon;
        public readonly ReactiveProperty<int> Level;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<bool> Loading;

        public PetSlotViewModel(PetSheet.Row petRow = null)
        {
            PetRow = petRow;
            Empty = new ReactiveProperty<bool>(petRow is null);
            EquippedIcon = new ReactiveProperty<bool>(false);
            Level = new ReactiveProperty<int>(0);
            HasNotification = new ReactiveProperty<bool>(false);
            Loading = new ReactiveProperty<bool>(false);
        }
    }
}
