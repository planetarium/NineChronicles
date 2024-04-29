using System;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    public class Battle : Widget
    {
        [SerializeField]
        private TextMeshProUGUI stageText;

        [SerializeField]
        private GuidedQuest guidedQuest;

        [SerializeField]
        private BossStatus bossStatus;

        [SerializeField]
        private RectTransform accelerationToggleRect;

        [SerializeField]
        private Button accelerationToggleLockButton;

        [SerializeField]
        private Toggle accelerationToggle;

        [SerializeField]
        private Toggle exitToggle;

        [SerializeField]
        private HelpButton helpButton;

        [SerializeField]
        private BossStatus enemyPlayerStatus;

        [SerializeField]
        private StageProgressBar stageProgressBar;

        [SerializeField]
        private ComboText comboText;

        private StageType _stageType;

        public BossStatus BossStatus => bossStatus;
        public BossStatus EnemyPlayerStatus => enemyPlayerStatus;
        public StageProgressBar StageProgressBar => stageProgressBar;
        public ComboText ComboText => comboText;
        public const int RequiredStageForExitButton = 10;
        public const int RequiredStageForAccelButton = 3;
        public const int RequiredStageForHeaderMenu = 3;
        private const string BattleAccelToggleValueKey = "Battle_Animation_Is_On";

        protected override void Awake()
        {
            base.Awake();

            exitToggle.onValueChanged.AddListener(value =>
            {
                var stage = Game.Game.instance.Stage;
                stage.IsExitReserved = value;
                if (value)
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_BATTLE_EXIT_RESERVATION_TITLE"),
                        NotificationCell.NotificationType.Information);
                }
            });

            accelerationToggle.onValueChanged.AddListener(SetAccelToggle);
            accelerationToggleLockButton.onClick.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_ACCEL_UNLOCK_CONDITION"),
                    NotificationCell.NotificationType.Information);
            });

            Game.Event.OnGetItem.AddListener(_ =>
            {
                var headerMenu = Find<HeaderMenuStatic>();
                if (!headerMenu)
                {
                    throw new WidgetNotFoundException<HeaderMenuStatic>();
                }

                var target = headerMenu.GetToggle(HeaderMenuStatic.ToggleType.AvatarInfo);
                VFXController.instance.CreateAndChase<DropItemInventoryVFX>(target, Vector3.zero);
            });
            CloseWidget = null;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            guidedQuest.Hide(ignoreCloseAnimation);
            var stage = Game.Game.instance.Stage;
            stage.AnimationTimeScaleWeight = Stage.DefaultAnimationTimeScaleWeight;
            stage.UpdateTimeScale();
            accelerationToggle.gameObject.SetActive(false);
            enemyPlayerStatus.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();
            stageProgressBar.Close();
        }

        public void Show(
            StageType stageType,
            int stageId,
            bool isExitReserved,
            bool isTutorial)
        {
            Find<EventBanner>().Close(true);
            _stageType = stageType;

            var canAccel =
                States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(
                    RequiredStageForAccelButton);
            accelerationToggleLockButton.gameObject.SetActive(!canAccel);
            accelerationToggle.gameObject.SetActive(canAccel);
            accelerationToggle.interactable = canAccel;
            accelerationToggle.isOn = canAccel && GetAccelToggleIsOn();
            SetAccelToggle(accelerationToggle.isOn);

            var canExit = States.Instance.CurrentAvatarState.worldInformation
                .IsStageCleared(RequiredStageForExitButton);
            exitToggle.gameObject.SetActive(canExit);

            // tutorial code for accel button
            if (stageId == RequiredStageForAccelButton + 1 &&
                !States.Instance.CurrentAvatarState.worldInformation
                    .IsStageCleared(stageId))
            {
                accelerationToggle.isOn = true;
                SetAccelToggle(true);
                Find<Tutorial>().PlayOnlyGuideArrow(
                    GuideType.Circle,
                    accelerationToggleRect,
                    // arrow position offset for hiding
                    arrowPositionOffset: new Vector2(0f, -10000));
            }

            if (isTutorial)
            {
                ShowForTutorial(false, stageId);
                return;
            }

            guidedQuest.Hide(true);
            base.Show();
            guidedQuest.Show(States.Instance.CurrentAvatarState, () =>
            {
                switch (_stageType)
                {
                    case StageType.HackAndSlash:
                    case StageType.Mimisbrunnr:
                        guidedQuest.SetWorldQuestToInProgress(stageId);
                        break;
                    case StageType.EventDungeon:
                        guidedQuest.SetEventDungeonStageToInProgress(stageId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
                }
            });

            stageText.text =
                $"STAGE {StageInformation.GetStageIdString(_stageType, stageId, true)}";
            stageText.gameObject.SetActive(true);
            stageProgressBar.Show();
            bossStatus.Close();
            enemyPlayerStatus.Close();
            comboText.Close();

            exitToggle.isOn = isExitReserved;
            helpButton.gameObject.SetActive(true);
        }

        public void ClearStage(int stageId, System.Action<bool> onComplete)
        {
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
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
                    break;
                case StageType.EventDungeon:
                    guidedQuest.ClearEventDungeonStage(stageId, cleared =>
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ShowComboText(bool attacked)
        {
            comboText.StopAllCoroutines();
            comboText.Show(attacked);
        }

        #region tutorial
        public void ShowForTutorial(bool isPrologue, int stageId = 0)
        {
            if (isPrologue)
            {
                stageProgressBar.Close();
            }
            else
            {
                stageText.text =
                    $"STAGE {StageInformation.GetStageIdString(_stageType, stageId, true)}";
                stageText.gameObject.SetActive(true);
                stageProgressBar.Show();
            }

            guidedQuest.gameObject.SetActive(false);
            bossStatus.gameObject.SetActive(false);
            helpButton.gameObject.SetActive(false);
            bossStatus.gameObject.SetActive(false);
            comboText.gameObject.SetActive(false);
            enemyPlayerStatus.gameObject.SetActive(false);
            comboText.comboMax = 5;
            gameObject.SetActive(true);
            Find<HeaderMenuStatic>().Close(true);
        }
        #endregion

        private void SetAccelToggle(bool value)
        {
            var stage = Game.Game.instance.Stage;
            stage.AnimationTimeScaleWeight = value
                ? Stage.AcceleratedAnimationTimeScaleWeight
                : Stage.DefaultAnimationTimeScaleWeight;
            stage.UpdateTimeScale();
            SetAccelToggleIsOn(value);
        }

        private bool GetAccelToggleIsOn()
        {
            return PlayerPrefs.GetInt(BattleAccelToggleValueKey, 0) != 0;
        }

        private void SetAccelToggleIsOn(bool isOn)
        {
            PlayerPrefs.SetInt(BattleAccelToggleValueKey, isOn ? 1 : 0);
        }
    }
}
