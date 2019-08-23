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

        public void Show(int stage)
        {
            base.Show();
            bottomMenu.Show();
            stageTitle.Show(stage);
        }

        private void OnGetItem(DropItem dropItem)
        {
            InventoryVfx.Play();
        }

        public override void OnCompleteOfCloseAnimation()
        {
            base.OnCompleteOfCloseAnimation();
            stageTitle?.Close();
            bottomMenu?.Close();
        }
    }
}