using Nekoyume.TableData.Pet;
using UniRx;

namespace Nekoyume.UI.Module.Pet
{
    public class PetSlotViewModel
    {
        public PetSheet.Row PetRow { get; }
        public readonly ReactiveProperty<bool> Empty;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<bool> Selected;

        public PetSlotViewModel(PetSheet.Row petRow = null, bool hasNotification = false)
        {
            PetRow = petRow;
            Empty = new ReactiveProperty<bool>(petRow is null);
            HasNotification = new ReactiveProperty<bool>(hasNotification);
            Selected = new ReactiveProperty<bool>(false);
        }
    }
}
