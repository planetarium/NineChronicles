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
        public BossStatus enemyPlayerStatus;
        public StageProgressBar stageProgressBar;
        
        protected override void Awake()
        {
            base.Awake();
            repeatButton.SetToggleListener(this);
            Game.Event.OnGetItem.AddListener(OnGetItem);

            CloseWidget = null;
        }

        public void Show(int stageId, bool isRepeat, bool isExitReserved)
        {
            base.Show();
            stageTitle.Show(stageId);
            stageProgressBar.Show();
            bossStatus.Close();
            enemyPlayerStatus.Close();

            if (isRepeat)
            {
                repeatButton.SetToggledOn();
            }
            else
            {
                repeatButton.SetToggledOff();
            }

            if (stageId > GameConfig.RequireStage.UIBottomMenuInBattle)
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.Show(
                    UINavigator.NavigationType.Exit,
                    SubscribeOnExitButtonClick,
                    false,
                    BottomMenu.ToggleableType.Mail,
                    BottomMenu.ToggleableType.Quest,
                    BottomMenu.ToggleableType.Chat,
                    BottomMenu.ToggleableType.IllustratedBook,
                    BottomMenu.ToggleableType.Character,
                    BottomMenu.ToggleableType.Inventory);

                bottomMenu.exitButton.SetToggleListener(this);
                bottomMenu.exitButton.SharedModel.IsEnabled.Value = isExitReserved;
            }
        }

        public void SubscribeOnExitButtonClick(BottomMenu bottomMenu)
        {
            var stage = Game.Game.instance.Stage;
            if (stage.isExitReserved)
            {
                stage.isExitReserved = false;
                bottomMenu.exitButton.IsToggleable = false;
                bottomMenu.exitButton.IsWidgetControllable = false;
                bottomMenu.exitButton.SharedModel.IsEnabled.Value = false;
                bottomMenu.exitButton.SetToggledOff();
            }
            else
            {
                bottomMenu.exitButton.IsToggleable = true;
                bottomMenu.exitButton.IsWidgetControllable = true;

                var confirm = Find<Confirm>();
                confirm.Show("UI_BATTLE_EXIT_RESERVATION_TITLE", "UI_BATTLE_EXIT_RESERVATION_CONTENT");
                confirm.CloseCallback = result =>
                {
                    if (result == ConfirmResult.Yes)
                    {
                        stage.isExitReserved = true;
                        bottomMenu.exitButton.SharedModel.IsEnabled.Value = true;
                        bottomMenu.exitButton.SetToggledOn();
                        repeatButton.SetToggledOff();
                    }
                };
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>()?.Close(ignoreCloseAnimation);
            stageProgressBar.Close();
            enemyPlayerStatus.Close();
            base.Close(ignoreCloseAnimation);
        }

        private void SetExitButtonToggledOff()
        {
            Game.Game.instance.Stage.isExitReserved = false;
            Find<BottomMenu>().exitButton.SharedModel.IsEnabled.Value = false;
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

        #region IToggleListener for repeatButton.

        public void OnToggle(IToggleable toggleable)
        {
            if (toggleable.IsToggledOn)
            {
                RequestToggledOff(toggleable);
            }
            else
            {
                RequestToggledOn(toggleable);
            }
        }

        public void RequestToggledOff(IToggleable toggleable)
        {
            toggleable.SetToggledOff();
            if ((ToggleableButton) toggleable == repeatButton)
            {
                Game.Game.instance.Stage.repeatStage = false;
            }
        }

        public void RequestToggledOn(IToggleable toggleable)
        {
            toggleable.SetToggledOn();
            if ((ToggleableButton)toggleable == repeatButton)
            {
                Game.Game.instance.Stage.repeatStage = true;
                SetExitButtonToggledOff();
            }
        }

        #endregion
    }
}
