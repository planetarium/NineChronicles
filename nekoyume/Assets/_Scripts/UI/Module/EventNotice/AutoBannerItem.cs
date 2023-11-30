using System;
using Nekoyume.L10n;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AutoBannerItem : EventBannerItem
    {
        [Serializable]
        public class LocalizedBannerItem
        {
            public Sprite bannerSprite;
            public Sprite popupSprite;
            public string url;
        }

        [SerializeField]
        private LocalizedBannerItem defaultItem;

        [SerializeField]
        private LocalizedBannerItem krItem;

        [SerializeField]
        private LocalizedBannerItem jpItem;
        private void Start()
        {
            var selectedData = L10nManager.CurrentLanguage switch
            {
                LanguageType.Korean => krItem,
                LanguageType.Japanese => jpItem,
                _ => defaultItem
            };
            Set(selectedData.bannerSprite, _ =>
            {
                var eventPopup = Widget.Find<EventPopup>();
                eventPopup.EventView.Set(selectedData.popupSprite, selectedData.url, false);
                eventPopup.Show();
            });
        }
    }
}
