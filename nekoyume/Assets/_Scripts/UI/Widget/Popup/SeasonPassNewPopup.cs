using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SeasonPassNewPopup : PopupWidget
    {
        private const string LastReadingDayKey = "SEASON_PASS_NEW_POPUP_LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            PlayerPrefs.SetString(LastReadingDayKey, DateTime.Today.ToString(DateTimeFormat));
        }

        public bool HasUnread
        {
            get
            {
                return PlayerPrefs.HasKey(LastReadingDayKey);
            }
        }

        public void OnSeasonPassBtnClick()
        {
            if (Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData == null)
            {
                return;
            }
            if (Game.Game.instance.SeasonPassServiceManager.AvatarInfo.Value == null)
            {
                OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_CONNECT_FAIL"), NotificationCell.NotificationType.Notification);
                return;
            }
            Find<SeasonPass>().Show();
        }
    }
}
