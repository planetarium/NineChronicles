using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;
    public class AdventureBossOpenInfoPopup : PopupWidget
    {
        [SerializeField] private ConditionalButton goToAdventureBossButton;

        protected override void Awake()
        {
            base.Awake();
            goToAdventureBossButton.OnClickSubject.Subscribe(_ => OnClickGoToAdventureBoss()).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            base.Show(ignoreShowAnimation);

            AudioController.instance.PlaySfx(AudioController.SfxCode.AdventureBossPopUp);
            goToAdventureBossButton.gameObject.SetActive(!BattleRenderer.Instance.IsOnBattle && !Widget.Find<LoadingScreen>().isActiveAndEnabled);
        }

        public void OnClickGoToAdventureBoss()
        {
            if (BattleRenderer.Instance.IsOnBattle)
            {
                OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_ADVENTUREBOSS_OPEN_FAILED_WHILE_BATTLE"),
                        NotificationCell.NotificationType.Alert);
                return;
            }

            var e = Widget.Find<AdventureBoss>();
            e.CloseWithOtherWidgets();
            Find<WorldMap>().Show(true);
            e.Show();
            AudioController.PlayClick();
        }
    }
}
