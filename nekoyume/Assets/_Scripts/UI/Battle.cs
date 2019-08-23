using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;

namespace Nekoyume.UI
{
    public class Battle : Widget
    {
        public StageTitle stageTitle;
        public BottomMenu bottomMenu;
        public DropItemInventoryVFX InventoryVfx;

        protected override void Awake()
        {
            base.Awake();
            Game.Event.OnGetItem.AddListener(OnGetItem);
            InventoryVfx.Stop();
        }

        public override void Initialize()
        {
            base.Initialize();
            var status = Find<Status>();
            bottomMenu.inventoryButton.onClick.AddListener(status.ToggleInventory);
            bottomMenu.questButton.onClick.AddListener(status.ToggleQuest);
            bottomMenu.infoAndEquipButton.onClick.AddListener(status.ToggleStatus);
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
            InventoryVfx.Play();
        }

        public override void OnCompleteOfCloseAnimation()
        {
            base.OnCompleteOfCloseAnimation();
            stageTitle?.Close();
        }
    }
}
