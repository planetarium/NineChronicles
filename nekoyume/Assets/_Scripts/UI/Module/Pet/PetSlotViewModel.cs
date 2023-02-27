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
        public readonly ReactiveProperty<bool> HasNotification;

        public PetSlotViewModel(PetSheet.Row petRow = null, bool hasNotification = false)
        {
            PetRow = petRow;
            Empty = new ReactiveProperty<bool>(petRow is null);
            EquippedIcon = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(hasNotification);
        }
    }
}
