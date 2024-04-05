using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CollectionEffectPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private CollectionEffect collectionEffect;

        [SerializeField]
        private Button goToCollectionButton;

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
    }
}
