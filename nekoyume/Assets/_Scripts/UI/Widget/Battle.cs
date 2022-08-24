using System;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

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
        private Toggle repeatToggle;

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

        [SerializeField]
        private GameObject boostEffectObject;

        [SerializeField]
        private TMP_Text boostCountText;

        private StageType _stageType;

        public BossStatus BossStatus => bossStatus;
        public BossStatus EnemyPlayerStatus => enemyPlayerStatus;
        public StageProgressBar StageProgressBar => stageProgressBar;
        public ComboText ComboText => comboText;
        public const int RequiredStageForExitButton = 3;

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
            enemyPlayerStatus.Close(ignoreCloseAnimation);
            Find<HeaderMenuStatic>().Close();
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
            bool isTutorial,
            int boostCost)
        {
            _stageType = stageType;
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
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    var cost = TableSheets.Instance.StageSheet.OrderedList
                        .First(i => i.Id == stageId)
                        .CostAP;
                    boostCountText.text = $"<sprite name=UI_main_icon_star><size=75%>{boostCost}</size>";
                    break;
                }
                case StageType.EventDungeon:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            boostEffectObject.SetActive(false);
            exitToggle.gameObject.SetActive(true);
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
            repeatToggle.gameObject.SetActive(false);
            helpButton.gameObject.SetActive(false);
            bossStatus.gameObject.SetActive(false);
            comboText.gameObject.SetActive(false);
            enemyPlayerStatus.gameObject.SetActive(false);
            exitToggle.gameObject.SetActive(false);
            comboText.comboMax = 5;
            gameObject.SetActive(true);
            Find<HeaderMenuStatic>().Close(true);
        }
        #endregion
    }
}
