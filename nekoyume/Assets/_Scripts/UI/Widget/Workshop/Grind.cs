using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Grind : Widget
    {
        [SerializeField]
        private GrindModule grindModule;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Inventory inventory;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });

            CloseWidget = () =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            grindModule.Show();
        }

        public void UpdateInventory()
        {
            inventory.UpdateEquipped();
        }
    }
}
