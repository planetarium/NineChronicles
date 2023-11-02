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

        protected override void Awake()
        {

        }

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

            Find<SeasonPass>().Show();
        }
    }
}
