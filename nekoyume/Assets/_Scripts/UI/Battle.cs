using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
namespace Nekoyume.UI
{
    public class Battle : Widget, IToggleListener
    {
        public StageTitle stageTitle;

        [SerializeField]
        private GuidedQuest guidedQuest = null;

        public BossStatus bossStatus;
        public ToggleableButton repeatButton;
        public BossStatus enemyPlayerStatus;
        public StageProgressBar stageProgressBar;
        public ComboText comboText;

        protected override void Awake()
        {
            base.Awake();
            repeatButton.SetToggleListener(this);
            Game.Event.OnGetItem.AddListener(OnGetItem);

            CloseWidget = null;
        }

        public void Show(int stageId, bool isRepeat, bool isExitReserved)
        {
            guidedQuest.Hide(true);
            base.Show();
            stageTitle.Show(stageId);
            guidedQuest.Show(States.Instance.CurrentAvatarState);
            stageProgressBar.Show();
            bossStatus.Close();
            enemyPlayerStatus.Close();
            comboText.Close();

            if (isRepeat)
            {
                repeatButton.SetToggledOn();
            }
            else
            {
                repeatButton.SetToggledOff();
            }

            if (States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                out var world) &&
                world.StageClearedId >= GameConfig.RequireClearedStageLevel.UIBottomMenuInBattle)
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
                    BottomMenu.ToggleableType.Character);

                bottomMenu.exitButton.SetToggleListener(this);
                bottomMenu.exitButton.SharedModel.IsEnabled.Value = isExitReserved;
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            guidedQuest.Hide(ignoreCloseAnimation);
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            enemyPlayerStatus.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        public void ClearStage(int stageId, System.Action<bool> onComplete)
        {
            guidedQuest.ClearWorldQuest(stageId, cleared =>
            {
                if (!cleared)
                {
                    onComplete(false);
                    return;
                }

                guidedQuest.UpdateList(
                    States.Instance.CurrentAvatarState,
                    () => onComplete(true));
            });
        }

        private void SubscribeOnExitButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            var stage = Game.Game.instance.Stage;
            if (stage.isExitReserved)
            {
                stage.isExitReserved = false;
                bottomMenu.exitButton.Toggleable = false;
                bottomMenu.exitButton.IsWidgetControllable = false;
                bottomMenu.exitButton.SharedModel.IsEnabled.Value = false;
                bottomMenu.exitButton.SetToggledOff();
            }
            else
            {
                bottomMenu.exitButton.Toggleable = true;
                bottomMenu.exitButton.IsWidgetControllable = true;

                var confirm = Find<Confirm>();
                confirm.Show("UI_BATTLE_EXIT_RESERVATION_TITLE", "UI_BATTLE_EXIT_RESERVATION_CONTENT");
                confirm.CloseCallback = result =>
                {
                    if (result == ConfirmResult.Yes)
                    {
                        stage.isExitReserved = true;
                        bottomMenu.exitButton.SharedModel.IsEnabled.Value = true;
                        repeatButton.SetToggledOff();
                    }
                };
            }
        }

        private static void SetExitButtonToggledOff()
        {
            Game.Game.instance.Stage.isExitReserved = false;
            Find<BottomMenu>().exitButton.SharedModel.IsEnabled.Value = false;
        }

        public void ShowComboText(bool attacked)
        {
            comboText.StopAllCoroutines();
            comboText.Show(attacked);
        }

        private void OnGetItem(DropItem dropItem)
        {
            var bottomMenu = Find<BottomMenu>();
            if (!bottomMenu)
            {
                throw new WidgetNotFoundException<BottomMenu>();
            }
            VFXController.instance.Create<DropItemInventoryVFX>(bottomMenu.characterButton.transform, Vector3.zero);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();
            stageTitle.Close();
            stageProgressBar.Close();
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
