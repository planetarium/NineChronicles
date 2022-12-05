using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Notice;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
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

        public override void Initialize()
        {
            _tabGroup.RegisterToggleable(eventTabButton);
            _tabGroup.RegisterToggleable(noticeTabButton);
            _tabGroup.OnToggledOn.Subscribe(toggle =>
            {
                if (eventTabButton.Equals(toggle))
                {
                    objectsForEvent.ForEach(go => go.SetActive(true));
                    objectsForNotice.ForEach(go => go.SetActive(false));
                }
                else
                {
                    objectsForNotice.ForEach(go => go.SetActive(true));
                    objectsForEvent.ForEach(go => go.SetActive(false));
                }
            }).AddTo(gameObject);
            _tabGroup.SetToggledOn(eventTabButton);
            closeButton.onClick.AddListener(() => Close());
            base.Initialize();
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => NoticeManager.instance.IsInitialized);

            var eventData = NoticeManager.instance.BannerData.ToList();
            foreach (var notice in eventData)
            {
                var item = Instantiate(originEventNoticeItem, eventScrollViewport);
                item.Set(notice, OnClickEventNoticeItem);
                _eventBannerItems.Add(notice.Description, item);
                if (_selectedEventBannerItem == null)
                {
                    _selectedEventBannerItem = item;
                    _selectedEventBannerItem.Select();
                }
            }

            var noticeData = NoticeManager.instance.NoticeData.ToList();
            foreach (var notice in noticeData)
            {
                var item = Instantiate(originNoticeItem, noticeScrollViewport);
                item.Set(notice, OnClickNoticeItem);
                if (_selectedNoticeItem == null)
                {
                    _selectedNoticeItem = item;
                    _selectedNoticeItem.Select();
                }
            }

            RenderNotice(_selectedEventBannerItem.Data);
            RenderNotice(_selectedNoticeItem.Data);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            if (_selectedEventBannerItem)
            {
                Show(_selectedEventBannerItem.Data, ignoreShowAnimation);
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
        }

        private void RenderNotice(EventNoticeData data)
        {
            eventView.Set(data.PopupImage, data.Url, data.UseAgentAddress);
        }
    }
}
