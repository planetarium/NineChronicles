using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Game.Notice;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class EventReleaseNotePopup : PopupWidget
    {
        [Header("For event banner & view")]
        [SerializeField]
        private List<GameObject> objectsForEvent;

        [SerializeField]
        private EventView eventView;

        [SerializeField]
        private Transform eventScrollViewport;

        [SerializeField]
        private CategoryTabButton eventTabButton;

        [Header("For notice & release note view")]
        [SerializeField]
        private List<GameObject> objectsForNotice;

        [SerializeField]
        private NoticeView noticeView;

        [SerializeField]
        private Transform noticeScrollViewport;

        [SerializeField]
        private CategoryTabButton noticeTabButton;

        [Header("Others")]
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private EventBannerItem originEventNoticeItem;

        [SerializeField]
        private NoticeItem originNoticeItem;

        private readonly Dictionary<string, EventBannerItem> _eventBannerItems = new();
        private EventBannerItem _selectedEventBannerItem;
        private NoticeItem _selectedNoticeItem;
        private readonly ToggleGroup _tabGroup = new();

        private const string LastReadingDayKey = "LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public override void Initialize()
        {
            base.Initialize();
            _tabGroup.RegisterToggleable(eventTabButton);
            _tabGroup.RegisterToggleable(noticeTabButton);
            _tabGroup.OnToggledOn.Subscribe(toggle =>
            {
                if (eventTabButton.Equals(toggle))
                {
                    RenderNotice(_selectedEventBannerItem.Data);
                    objectsForEvent.ForEach(go => go.SetActive(true));
                    objectsForNotice.ForEach(go => go.SetActive(false));
                }
                else
                {
                    RenderNotice(_selectedNoticeItem.Data);
                    objectsForNotice.ForEach(go => go.SetActive(true));
                    objectsForEvent.ForEach(go => go.SetActive(false));
                }
            }).AddTo(gameObject);
            _tabGroup.SetToggledOn(eventTabButton);
            closeButton.onClick.AddListener(() => Close());

            var noticeManager = NoticeManager.instance;
            eventTabButton.HasNotification.SetValueAndForceNotify(noticeManager.HasUnreadEvent);
            noticeTabButton.HasNotification.SetValueAndForceNotify(noticeManager.HasUnreadNotice);
            noticeManager.ObservableHasUnreadEvent
                .SubscribeTo(eventTabButton.HasNotification)
                .AddTo(gameObject);
            noticeManager.ObservableHasUnreadNotice
                .SubscribeTo(noticeTabButton.HasNotification)
                .AddTo(gameObject);
            var eventData = noticeManager.BannerData;
            foreach (var notice in eventData)
            {
                var item = Instantiate(originEventNoticeItem, eventScrollViewport);
                item.Set(notice,
                    !noticeManager.IsAlreadyReadNotice(notice.Description),
                    OnClickEventNoticeItem);
                _eventBannerItems.Add(notice.Description, item);
                if (_selectedEventBannerItem == null)
                {
                    _selectedEventBannerItem = item;
                    _selectedEventBannerItem.Select();
                }
            }

            var noticeData = noticeManager.NoticeData.ToList();
            foreach (var notice in noticeData)
            {
                var item = Instantiate(originNoticeItem, noticeScrollViewport);
                item.Set(notice,
                    !noticeManager.IsAlreadyReadNotice(notice.Header),
                    OnClickNoticeItem);
                if (_selectedNoticeItem == null)
                {
                    _selectedNoticeItem = item;
                    _selectedNoticeItem.Select();
                }
            }

            RenderNotice(_selectedEventBannerItem.Data);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (!Game.Game.instance.Stage.TutorialController.IsCompleted)
            {
                return;
            }

            var hasUnreadContents = NoticeManager.instance.HasUnreadEvent ||
                                    NoticeManager.instance.HasUnreadNotice;
            var notReadAtToday = true;
            if (PlayerPrefs.HasKey(LastReadingDayKey) &&
                DateTime.TryParseExact(PlayerPrefs.GetString(LastReadingDayKey),
                    DateTimeFormat,
                    null,
                    DateTimeStyles.None,
                    out var result))
            {
                notReadAtToday = DateTime.Today != result.Date;
            }

            if (hasUnreadContents || notReadAtToday)
            {
                base.Show(ignoreShowAnimation);
                _tabGroup.SetToggledOn(eventTabButton);
                PlayerPrefs.SetString(LastReadingDayKey, DateTime.Today.ToString(DateTimeFormat));
            }
        }

        public void Show(EventNoticeData eventNotice, bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
            if (!eventTabButton.IsToggledOn)
            {
                _tabGroup.SetToggledOn(eventTabButton);
                _tabGroup.OnToggledOn.OnNext(eventTabButton);
            }

            OnClickEventNoticeItem(_eventBannerItems[eventNotice.Description]);
        }

        private void OnClickEventNoticeItem(EventBannerItem item)
        {
            if (_selectedEventBannerItem == item)
            {
                return;
            }

            _selectedEventBannerItem.DeSelect();
            _selectedEventBannerItem = item;
            _selectedEventBannerItem.Select();
            RenderNotice(item.Data);
        }

        private void OnClickNoticeItem(NoticeItem item)
        {
            if (_selectedNoticeItem == item)
            {
                return;
            }

            _selectedNoticeItem.DeSelect();
            _selectedNoticeItem = item;
            _selectedNoticeItem.Select();
            RenderNotice(item.Data);
        }

        private void RenderNotice(NoticeData data)
        {
            noticeView.Set(data);
            NoticeManager.instance.AddToCheckedList(data.Header);
        }

        private void RenderNotice(EventNoticeData data)
        {
            eventView.Set(data.PopupImage, data.Url, data.UseAgentAddress);
            NoticeManager.instance.AddToCheckedList(data.Description);
        }
    }
}
