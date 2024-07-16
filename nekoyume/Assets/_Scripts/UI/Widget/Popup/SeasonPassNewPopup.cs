using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.ApiClient;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SeasonPassNewPopup : PopupWidget
    {
        private const string LastReadingDayKey = "SEASON_PASS_NEW_POPUP_LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        private string LastReadingDayBySeasonId
        {
            get
            {
                var seasonId = string.Empty;
                if (ApiClients.Instance.SeasonPassServiceManager.CurrentSeasonPassData != null)
                {
                    seasonId = ApiClients.Instance.SeasonPassServiceManager.CurrentSeasonPassData.Id.ToString();
                }

                return $"{LastReadingDayKey}{seasonId}";
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            PlayerPrefs.SetString(LastReadingDayBySeasonId, DateTime.Today.ToString(DateTimeFormat));
        }

        public bool HasUnread => !PlayerPrefs.HasKey(LastReadingDayBySeasonId);

        public void OnSeasonPassBtnClick()
        {
            if (ApiClients.Instance.SeasonPassServiceManager.CurrentSeasonPassData == null)
            {
                return;
            }

            if (ApiClients.Instance.SeasonPassServiceManager.AvatarInfo.Value == null)
            {
                OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_CONNECT_FAIL"), NotificationCell.NotificationType.Notification);
                return;
            }

            base.Close();
            Find<SeasonPass>().Show();
        }
    }
}
