using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Battle : Widget, IToggleListener
    {
        public StageTitle stageTitle;
        public BossStatus bossStatus;
        public ToggleableButton repeatButton;
        
        protected override void Awake()
        {
            base.Awake();
            repeatButton.SetToggleListener(this);
            Game.Event.OnGetItem.AddListener(OnGetItem);
        }

        public void Show(int stageId, bool isRepeat)
        {
            base.Show();
            stageTitle.Show(stageId);
            bossStatus.Close();

            if (isRepeat)
            {
                repeatButton.SetToggledOn();
            }
            else
            {
                repeatButton.SetToggledOff();
            }

            var bottomMenu = Find<BottomMenu>();
            bottomMenu.leaveBattleButton.SharedModel.IsEnabled.Value = false;
            bottomMenu?.Show(
                UINavigator.NavigationType.Battle,
                SubscribeOnExitButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character,
                BottomMenu.ToggleableType.Inventory);
        }

        public void SubscribeOnExitButtonClick(BottomMenu bottomMenu)
        {
            var stage = Game.Game.instance.stage;
            if (stage.isExitReserved)
            {
                stage.isExitReserved = false;
                bottomMenu.leaveBattleButton.SharedModel.IsEnabled.Value = false;
                return;
            }
            else
            {
                var confirm = Find<Confirm>();
                confirm.CloseCallback = result =>
                {
                    if (result == ConfirmResult.Yes)
                    {
                        stage.isExitReserved = true;
                        bottomMenu.leaveBattleButton.SharedModel.IsEnabled.Value = true;
                    }
                };
                confirm?.Show("UI_BATTLE_EXIT_RESERVATION_TITLE", "UI_BATTLE_EXIT_RESERVATION_CONTENT");
            }
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

        #region IToggleListener

        public void OnToggle(IToggleable toggleable)
        {
            if (toggleable.IsToggledOn)
            {
                toggleable.SetToggledOff();
                if ((ToggleableButton) toggleable == repeatButton)
                {
                    Game.Game.instance.stage.repeatStage = false;
                }
            }
            else
            {
                toggleable.SetToggledOn();
                if ((ToggleableButton) toggleable == repeatButton)
                {
                    Game.Game.instance.stage.repeatStage = true;
                }
            }
        }

        public void RequestToggledOff(IToggleable toggleable)
        {
            toggleable.SetToggledOff();
            if ((ToggleableButton) toggleable == repeatButton)
            {
                Game.Game.instance.stage.repeatStage = false;
            }
        }

        #endregion
    }
}
