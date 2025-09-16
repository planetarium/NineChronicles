using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.LiveAsset;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

namespace Nekoyume.UI
{
    using UniRx;

    public class NcuPopup : PopupWidget
    {
        [Header("For event banner & view")]
        [SerializeField]
        private List<GameObject> objectsForEvent;

        [SerializeField]
        private EventView eventView;

        [SerializeField]
        private Transform eventScrollViewport;

        [Header("Others")]
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private EventBannerItem originEventNoticeItem;

        [SerializeField]
        private Sprite comingSoonBannerSprite;

        [SerializeField]
        private Sprite comingSoonNoticeSprite;

        private readonly Dictionary<string, EventBannerItem> _eventBannerItems = new();
        private EventBannerItem _selectedEventBannerItem;
        private NoticeItem _selectedNoticeItem;
        private bool _isInitialized;

        private System.Action _onClose;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
        }

        public override void Initialize()
        {
            base.Initialize();
            var liveAssetManager = LiveAssetManager.instance;
            if (!liveAssetManager.IsInitialized || _isInitialized)
            {
                NcDebug.LogError("LiveAssetManager is not initialized or already initialized.");
                return;
            }

            try
            {
                var eventData = liveAssetManager.NcuData;

                var requiredCount = 4 - eventData.Count;
                foreach (var notice in eventData)
                {
                    var item = Instantiate(originEventNoticeItem, eventScrollViewport);

                    if (item is null)
                    {
                        NcDebug.LogError($"item is Null");
                    }

                    if (notice is null)
                    {
                        NcDebug.LogError($"notice is Null");
                    }

                    item.Set(notice,
                        LiveAssetManager.instance.HasUnreadNcu,
                        OnClickEventNoticeItem);
                    if (_selectedEventBannerItem == null)
                    {
                        _selectedEventBannerItem = item;
                        _selectedEventBannerItem.Select();
                    }
                }

                for (int i = 0; i < requiredCount; i++)
                {
                    var item = Instantiate(originEventNoticeItem, eventScrollViewport);

                    if (item is null)
                    {
                        NcDebug.LogError("item is Null");
                    }

                    item.Set(comingSoonBannerSprite, OnClickEventNoticeItem);
                }

                RenderNotice(_selectedEventBannerItem.Data);
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }
            _isInitialized = true;
        }

        private async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await UniTask.WaitUntil(() => LiveAssetManager.instance.IsInitialized);
            Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeAsync().Forget();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _onClose?.Invoke();
        }

        public void ShowNotFiltered(System.Action onClose)
        {
            _onClose = onClose;
            Show();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (!_isInitialized)
            {
                ShowAsync(ignoreShowAnimation).Forget();
                return;
            }

            base.Show(ignoreShowAnimation);
            LiveAssetManager.instance.ReadNcu();
        }

        private async UniTask ShowAsync(bool ignoreShowAnimation = false)
        {
            await InitializeAsync();
            base.Show(ignoreShowAnimation);
            LiveAssetManager.instance.ReadNcu();
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

        private void RenderNotice(EventNoticeData data)
        {
            if (data is not null)
            {
                eventView.Set(data.PopupImage, data.Url, data.UseAgentAddress, data.WithSign, data.ButtonType, data.InGameNavigationData);
            }
            else
            {
                eventView.Set(comingSoonNoticeSprite);
            }
        }
    }
}
