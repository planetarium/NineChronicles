using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Battle : Widget
    {
        public StageTitle stageTitle;
        public BottomMenu bottomMenu;

        protected override void Awake()
        {
            base.Awake();
            Game.Event.OnGetItem.AddListener(OnGetItem);
        }

        public override void Initialize()
        {
            base.Initialize();
            var status = Find<Status>();
            bottomMenu.inventoryButton.onClick.AddListener(status.ToggleInventory);
            bottomMenu.questButton.onClick.AddListener(status.ToggleQuest);
            bottomMenu.avatarStatusButton.onClick.AddListener(status.ToggleStatus);
        }

        public void Show(int stage)
        {
            base.Show();
            stageTitle.Show(stage);
        }

        public override void Close()
        {
            Find<Inventory>()?.Close();
            Find<StatusDetail>()?.Close();
            Find<Quest>()?.Close();

            base.Close();
        }

        private void OnGetItem(DropItem dropItem)
        {
            VFXController.instance.Create<DropItemInventoryVFX>(bottomMenu.inventoryButton.transform, Vector3.zero);
        }

        public override void OnCompleteOfCloseAnimation()
        {
            base.OnCompleteOfCloseAnimation();
            stageTitle?.Close();
        }
    }
}
