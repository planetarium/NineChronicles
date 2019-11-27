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

            Find<BottomMenu>()?.Show(
                UINavigator.NavigationType.None,
                null,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character,
                BottomMenu.ToggleableType.Inventory);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>()?.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        private void OnGetItem(DropItem dropItem)
        {
            var bottomMenu = Find<BottomMenu>();
            if (!bottomMenu)
            {
                throw new WidgetNotFoundException<BottomMenu>();
            }
            VFXController.instance.Create<DropItemInventoryVFX>(bottomMenu.inventoryButton.button.transform, Vector3.zero);
        }

        protected override void OnCompleteOfCloseAnimation()
        {
            base.OnCompleteOfCloseAnimation();
            stageTitle.Close();
        }
    }
}
