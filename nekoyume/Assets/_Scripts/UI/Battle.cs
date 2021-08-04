using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Battle : Widget
    {
        [SerializeField] private StageTitle stageTitle = null;
        [SerializeField] private GuidedQuest guidedQuest = null;
        [SerializeField] private BossStatus bossStatus = null;
        [SerializeField] private Toggle repeatToggle = null;
        [SerializeField] private Toggle exitToggle = null;
        [SerializeField] private HelpButton helpButton = null;
        [SerializeField] private BossStatus enemyPlayerStatus = null;
        [SerializeField] private StageProgressBar stageProgressBar = null;
        [SerializeField] private ComboText comboText = null;

        public BossStatus BossStatus => bossStatus;
        public BossStatus EnemyPlayerStatus => enemyPlayerStatus;
        public StageProgressBar StageProgressBar => stageProgressBar;
        public ComboText ComboText => comboText;
        public const int RequiredStageForExitButton = 4;

        protected override void Awake()
        {
            base.Awake();

            repeatToggle.onValueChanged.AddListener(value =>
            {
                var stage = Game.Game.instance.Stage;
                stage.IsRepeatStage = value;
                if (value)
                {
                    stage.IsExitReserved = false;
                }
            });

            exitToggle.onValueChanged.AddListener(value =>
            {
                var stage = Game.Game.instance.Stage;
                stage.IsExitReserved = value;
                if (value)
                {
                    OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_BATTLE_EXIT_RESERVATION_TITLE"));
                    stage.IsRepeatStage = false;
                }
            });

            Game.Event.OnGetItem.AddListener(_ =>
            {
                var headerMenu = Find<HeaderMenu>();
                if (!headerMenu)
                {
                    throw new WidgetNotFoundException<HeaderMenu>();
                }

                var target = headerMenu.GetToggle(HeaderMenu.ToggleType.AvatarInfo);
                VFXController.instance.CreateAndChase<DropItemInventoryVFX>(target, Vector3.zero);
            });
            CloseWidget = null;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            guidedQuest.Hide(ignoreCloseAnimation);
            enemyPlayerStatus.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();
            stageTitle.Close();
            stageProgressBar.Close();
        }

        public void ShowInArena(bool ignoreShowAnimation = false)
        {
            stageTitle.Close();
            comboText.Close();
            stageProgressBar.Close();
            guidedQuest.Hide(true);
            repeatToggle.gameObject.SetActive(false);
            exitToggle.gameObject.SetActive(false);
            helpButton.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        public void Show(int stageId, bool isRepeat, bool isExitReserved, bool isTutorial)
        {
            stageTitle.Show(stageId);
            if (isTutorial)
            {
                ShowForTutorial(false);
                return;
            }

            guidedQuest.Hide(true);
            base.Show();
            guidedQuest.Show(States.Instance.CurrentAvatarState, () =>
            {
                guidedQuest.SetWorldQuestToInProgress(stageId);
            });
            stageProgressBar.Show();
            bossStatus.Close();
            enemyPlayerStatus.Close();
            comboText.Close();

            exitToggle.isOn = isExitReserved;
            repeatToggle.isOn = isExitReserved ? false : isRepeat;
            helpButton.gameObject.SetActive(true);
            repeatToggle.gameObject.SetActive(true);
            exitToggle.gameObject.SetActive(true);
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

        public void ShowComboText(bool attacked)
        {
            comboText.StopAllCoroutines();
            comboText.Show(attacked);
        }

        #region tutorial
        public void ShowForTutorial(bool isPrologue)
        {
            if (isPrologue)
            {
                stageProgressBar.Close();
                stageTitle.gameObject.SetActive(false);
            }
            else
            {
                stageProgressBar.Show();
            }

            guidedQuest.gameObject.SetActive(false);
            bossStatus.gameObject.SetActive(false);
            repeatToggle.gameObject.SetActive(false);
            helpButton.gameObject.SetActive(false);
            bossStatus.gameObject.SetActive(false);
            comboText.gameObject.SetActive(false);
            enemyPlayerStatus.gameObject.SetActive(false);
            exitToggle.gameObject.SetActive(false);
            comboText.comboMax = 5;
            gameObject.SetActive(true);
            Find<HeaderMenu>().Close(true);
        }
        #endregion
    }
}
