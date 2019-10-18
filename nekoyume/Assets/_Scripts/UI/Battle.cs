using Nekoyume.Game.Item;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class Battle : Widget
    {
        public StageTitle stageTitle;
        public BossStatus bossStatus;
        public bool IsBossAlive => bossStatus.hpBar.fillAmount > 0f;

        protected override void Awake()
        {
            base.Awake();
            Game.Event.OnGetItem.AddListener(OnGetItem);
        }

        public void Show(int stage)
        {
            base.Show();
            stageTitle.Show(stage);

            Find<BottomMenu>()?.Show(UINavigator.NavigationType.None, null, true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>()?.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        private void OnGetItem(DropItem dropItem)
        {
            // todo: 인벤토리 아이콘 위치 논의 후 활성화.
//            VFXController.instance.Create<DropItemInventoryVFX>(bottomMenu.inventoryButton.image.transform, Vector3.zero);
        }

        public override void OnCompleteOfCloseAnimation()
        {
            base.OnCompleteOfCloseAnimation();
            stageTitle.Close();
        }
    }
}
