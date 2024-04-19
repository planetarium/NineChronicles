using System;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatsBonusPopup : PopupWidget
    {
        [Serializable]
        private struct RuneLevelBonus
        {
            public TextMeshProUGUI bonusText;
            public TextMeshProUGUI rewardText;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private CollectionEffect collectionEffect;

        [SerializeField]
        private Button goToCollectionButton;

        [SerializeField]
        private RuneLevelBonus runeLevelBonus;

        [SerializeField]
        private Button goToRuneButton;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
            };

            goToCollectionButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                GoToCollection();
            });

            goToRuneButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                GoToRune();
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            collectionEffect.Set(
                collectionState.Ids.Count,
                collectionSheet.Count,
                collectionState.GetEffects(collectionSheet));

            var bonus = RuneFrontHelper.CalculateRuneLevelBonus(
                States.Instance.AllRuneState,
                Game.Game.instance.TableSheets.RuneListSheet);
            var reward = RuneFrontHelper.CalculateRuneLevelBonusReward(
                bonus,
                Game.Game.instance.TableSheets.RuneLevelBonusSheet);
            runeLevelBonus.bonusText.text = bonus.ToString();
            runeLevelBonus.rewardText.text = $"+{reward / 1000m:0.###}%";
        }

        private void GoToCollection()
        {
            var clearedStageId = States.Instance.CurrentAvatarState
                .worldInformation.TryGetLastClearedStageId(out var id) ? id : 1;
            const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.TutorialEnd;
            if (clearedStageId < requiredStage)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_STAGE_LOCK_FORMAT", requiredStage),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            if (BattleRenderer.Instance.IsOnBattle)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            CloseWithOtherWidgets();
            Find<Collection>().Show();
        }

        private void GoToRune()
        {
            var clearedStageId = States.Instance.CurrentAvatarState
                .worldInformation.TryGetLastClearedStageId(out var id) ? id : 1;
            const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.Rune;
            if (clearedStageId < requiredStage)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_STAGE_LOCK_FORMAT", requiredStage),
                    NotificationCell.NotificationType.UnlockCondition);
                return;
            }

            if (BattleRenderer.Instance.IsOnBattle)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_BLOCK_EXIT"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            CloseWithOtherWidgets();
            Find<Rune>().Show();
        }
    }
}
