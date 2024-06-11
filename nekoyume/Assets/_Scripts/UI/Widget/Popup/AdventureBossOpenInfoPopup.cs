using Codice.Utils;
using Cysharp.Threading.Tasks.Triggers;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Nekoyume.Game.LiveAsset.GameConfig;

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
            base.Show(ignoreShowAnimation);

            goToAdventureBossButton.gameObject.SetActive(!BattleRenderer.Instance.IsOnBattle);
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
            e.Show();
            AudioController.PlayClick();
        }
    }
}
