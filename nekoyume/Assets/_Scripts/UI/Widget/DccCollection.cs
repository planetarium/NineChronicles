using Nekoyume.Game;
using Nekoyume.UI.Module.Pet;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DccCollection : Widget
    {
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Transform petSlotViewParent;

        [SerializeField]
        private PetSlotView originPetSlotView;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(() =>
            {
                Close(true);
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            foreach (var petRow in TableSheets.Instance.PetSheet.Values)
            {
                Instantiate(originPetSlotView, petSlotViewParent).Set(new PetSlotViewModel(petRow));
            }
        }
    }
}
