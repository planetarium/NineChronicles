using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.LiveAsset;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Ncu;
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

        [Header("NCU link-status (와이어프레임 b9a17fa5)")]
        [SerializeField]
        private NcuServerBlock[] ncuServerBlocks; // 고정 슬롯(프로젝트당 최대 2). 프리팹에 미리 배치.

        [SerializeField]
        private GameObject ncuNoticeObject; // "두 서버는 계정과 진행 상황이 공유되지 않습니다"

        private readonly Dictionary<string, EventBannerItem> _eventBannerItems = new();
        private EventBannerItem _selectedEventBannerItem;
        private NoticeItem _selectedNoticeItem;
        private bool _isInitialized;

        // 배너(탭) 생성은 Initialize / InitializeWithImageLoaded 두 경로가 있고 둘 다 Instantiate한다.
        //   _isInitialized 만으로는 못 막는다(이미지 대기로 빠지는 경로가 그 플래그를 안 세우고 나갔다).
        //   생성 여부를 따로 들고 어느 경로로 들어와도 한 번만 만든다.
        private bool _bannerItemsBuilt;


        private System.Action _onClose;

        // (NCU) 팝업 오픈 시 포탈 by-address 조회 결과 + 파생 표시 모델.
        private ApiClient.NcuServiceManager.NcuLinkStatusResponse _ncuLinkStatus;
        private List<NcuProjectView> _ncuProjectViews;

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
                // NCU 이미지 로딩이 완료될 때까지 대기
                var eventData = liveAssetManager.NcuData;
                if (eventData.Any(data => data.BannerImage == null || data.PopupImage == null))
                {
                    NcDebug.LogWarning($"[{nameof(NcuPopup)}] Some NCU images are still loading, waiting...");
                    // 대기로 넘길 때도 초기화된 것으로 표시한다. 안 그러면 그 사이 OnEnable→InitializeAsync가
                    //   가드를 통과해 탭을 한 벌 더 만들고, 대기가 끝난 InitializeWithImageLoaded가 또 만든다.
                    _isInitialized = true;
                    WaitForNcuBannerImagesAsync().Forget();
                    return;
                }

                if (_bannerItemsBuilt)
                {
                    return;
                }

                _bannerItemsBuilt = true;
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
                    _eventBannerItems.Add(notice.Description, item);
                    if (_selectedEventBannerItem == null)
                    {
                        _selectedEventBannerItem = item;
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
                    if (_selectedEventBannerItem == null)
                    {
                        _selectedEventBannerItem = item;
                    }
                }

                // 이미지 로딩이 완료된 후에 select 처리
                if (_selectedEventBannerItem != null)
                {
                    _selectedEventBannerItem.Select();
                    RenderNotice(_selectedEventBannerItem.Data);
                    // 탭이 이제야 생겼다. 연동상태 조회가 이보다 먼저 끝났으면 그때의 렌더는
                    //   선택된 탭이 없어 아무것도 못 그렸으므로 여기서 한 번 다시 그린다.
                    ApplyNcuLinkStatus(_ncuLinkStatus, isQuerying: _ncuLinkStatus == null);
                }
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

        private async UniTask WaitForNcuBannerImagesAsync()
        {
            var liveAssetManager = LiveAssetManager.instance;
            var timeout = TimeSpan.FromSeconds(10);
            var cancellationTokenSource = new CancellationTokenSource(timeout);

            try
            {
                await UniTask.WaitUntil(() =>
                    liveAssetManager.NcuData.All(data => data.BannerImage != null && data.PopupImage != null),
                    cancellationToken: cancellationTokenSource.Token);

                NcDebug.Log($"[{nameof(NcuPopup)}] All NCU banner images loaded successfully");
                InitializeWithImageLoaded();
            }
            catch (OperationCanceledException)
            {
                NcDebug.LogError($"[{nameof(NcuPopup)}] NCU banner image loading timeout after {timeout.TotalSeconds} seconds");
                // 타임아웃이 발생해도 초기화를 진행 (null 체크가 있으므로 안전)
                InitializeWithImageLoaded();
            }
        }

        private void InitializeWithImageLoaded()
        {
            var liveAssetManager = LiveAssetManager.instance;

            // 타임아웃 경로와 정상 완료 경로가 둘 다 여기로 오고, Initialize가 먼저 만들었을 수도 있다.
            //   기존 자식을 지우는 방식은 쓰지 않는다 — ServerRail이 같은 부모(eventScrollViewport)에
            //   들어와 있어서 싹 지우면 서버 블록까지 날아가고 ncuServerBlocks 참조가 끊긴다.
            if (_bannerItemsBuilt)
            {
                _isInitialized = true;
                return;
            }

            _bannerItemsBuilt = true;

            try
            {
                var eventData = liveAssetManager.NcuData;
                var requiredCount = 4 - eventData.Count;

                foreach (var notice in eventData)
                {
                    var item = Instantiate(originEventNoticeItem, eventScrollViewport);
                    item.Set(notice,
                        LiveAssetManager.instance.HasUnreadNcu,
                        OnClickEventNoticeItem);
                    _eventBannerItems.Add(notice.Description, item);
                    if (_selectedEventBannerItem == null)
                    {
                        _selectedEventBannerItem = item;
                    }
                }

                for (int i = 0; i < requiredCount; i++)
                {
                    var item = Instantiate(originEventNoticeItem, eventScrollViewport);
                    item.Set(comingSoonBannerSprite, OnClickEventNoticeItem);
                    if (_selectedEventBannerItem == null)
                    {
                        _selectedEventBannerItem = item;
                    }
                }

                // 이미지 로딩이 완료된 후에 select 처리
                if (_selectedEventBannerItem != null)
                {
                    _selectedEventBannerItem.Select();
                    RenderNotice(_selectedEventBannerItem.Data);
                    // 탭이 이제야 생겼다. 연동상태 조회가 이보다 먼저 끝났으면 그때의 렌더는
                    //   선택된 탭이 없어 아무것도 못 그렸으므로 여기서 한 번 다시 그린다.
                    ApplyNcuLinkStatus(_ncuLinkStatus, isQuerying: _ncuLinkStatus == null);
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }

            _isInitialized = true;
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
            RefreshNcuLinkStatus();
        }

        private async UniTask ShowAsync(bool ignoreShowAnimation = false)
        {
            await InitializeAsync();
            base.Show(ignoreShowAnimation);
            LiveAssetManager.instance.ReadNcu();
            RefreshNcuLinkStatus();
        }

        // (NCU) 팝업 오픈 시 포탈에 본인 연동상태 조회(9c 서명) → 탭 점 + 우측 서버 블록 렌더.
        //   빌드 프로파일은 글로벌만(K버전 #3은 별도 논의). 프리팹 ref 미배선이면 렌더는 no-op.
        private void RefreshNcuLinkStatus()
        {
            var manager = ApiClient.ApiClients.Instance.NcuServiceManager;
            if (manager == null || !manager.IsInitialized)
            {
                return;
            }

            // 조회 중 상태 먼저 반영(최초엔 탭 점 점멸 + 스켈레톤).
            ApplyNcuLinkStatus(_ncuLinkStatus, isQuerying: _ncuLinkStatus == null);
            RefreshNcuLinkStatusAsync(manager).Forget();
        }

        private async UniTask RefreshNcuLinkStatusAsync(ApiClient.NcuServiceManager manager)
        {
            var status = await manager.FetchLinkStatusAsync();
            if (status == null)
            {
                // 조회 자체가 실패한 경우. 그냥 리턴하면 조회중 상태가 그대로 남아
                //   스피너가 영구 고정되고 재시도 버튼도 안 뜬다. 실패로 렌더한다.
                NcDebug.LogWarning("[NcuPopup] NCU link-status unavailable.");
                ApplyNcuLinkStatus(null, isQuerying: false, isFailed: true);
                return;
            }

            _ncuLinkStatus = status;
            ApplyNcuLinkStatus(status, isQuerying: false); // 렌더 먼저 — 로그 NRE가 렌더를 막지 않도록.
            LogNcuLinkStatus(status);
        }

        // 응답 → 표시 모델 → 선택된 탭 아래 렌더. 렌더 진입점은 이 하나뿐이다.
        //   isFailed: 조회 자체가 실패(응답 없음). 이때 응답이 null이라 conn도 없는데,
        //     그걸 "미연동"으로 그리면 이미 연동한 사람에게 거짓말을 하고 재시도 버튼 대신
        //     연동 버튼이 뜬다. 실패는 미연동과 구분해서 넘긴다.
        private void ApplyNcuLinkStatus(
            ApiClient.NcuServiceManager.NcuLinkStatusResponse status,
            bool isQuerying,
            bool isFailed = false)
        {
            _ncuProjectViews = NcuLinkStatusView.Build(
                status, ResolveBuildProfile(), isQuerying, isFailed);
            RenderSelectedProjectServers();
        }


        // 빌드별 노출 규칙. K버전은 연동 요소(상태·Wallet/Link)와 토큰 서버를 숨긴다.
        //   판별은 기존 GameConfig.IsKoreanBuild(패키지명 기준)를 그대로 쓴다.
        //   restrictAllExternal: 외부 링크를 전면 금지해야 하면 true → RB 탭까지 숨긴다.
        //   ⚠️ 지금은 false(포탈·자사 홈페이지만 금지) 가정이다. 심사 요건이 "모든 외부 링크"로
        //     확정되면 이 값을 true로 바꾸거나 설정으로 빼야 한다.
        private const bool KVersionRestrictAllExternal = false;

        private static NcuBuildProfile ResolveBuildProfile()
        {
            return Game.LiveAsset.GameConfig.IsKoreanBuild
                ? NcuBuildProfile.KVersion(KVersionRestrictAllExternal)
                : NcuBuildProfile.Global;
        }

        private void RenderSelectedProjectServers()
        {
            if (ncuServerBlocks == null || ncuServerBlocks.Length == 0)
            {
                return;
            }

            var view = FindProjectView(ResolveProjectIdForItem(_selectedEventBannerItem));

            var count = view != null ? view.Servers.Count : 0;

            SetActiveSafe(ncuNoticeObject, view != null && view.ShowNotice);
            MoveServerRailUnderSelectedTab(count > 0);

            for (var i = 0; i < ncuServerBlocks.Length; i++)
            {
                var block = ncuServerBlocks[i];
                if (block == null)
                {
                    continue;
                }

                if (i < count)
                {
                    block.gameObject.SetActive(true);
                    block.Set(view.Servers[i], PlaySelectedBanner);
                }
                else
                {
                    block.gameObject.SetActive(false);
                }
            }

            ResizeServerRail(count);
        }

        // 행의 "플레이 하러 가기"는 우측 배너 이미지를 누른 것과 같은 동작이어야 한다.
        //   목적지·서명 URL·인게임 이동 판단이 전부 라이브에셋 데이터에 들어있으므로
        //   여기서 URL을 따로 들고 있지 않고 EventView에 그대로 위임한다.
        //   (선택된 탭 = 그 배너이므로 행이 가리키는 곳과 이미지가 가리키는 곳은 늘 같다.)
        private void PlaySelectedBanner()
        {
            if (eventView == null)
            {
                return;
            }

            eventView.InvokeButtonAction();
        }

        // 레일 높이는 "켜진 행 수"를 따라간다. 고정값을 두면 서버가 1개인 탭(RB web2/web3처럼
        //   탭당 1서버)에서 나머지 한 행 자리가 빈 칸으로 남는다.
        private void ResizeServerRail(int activeCount)
        {
            var first = ncuServerBlocks[0];
            if (first == null)
            {
                return;
            }

            var rail = ResolveServerRail();
            var le = rail != null ? rail.GetComponent<LayoutElement>() : null;
            if (le == null)
            {
                return;
            }

            if (activeCount <= 0)
            {
                le.preferredHeight = 0f;
                return;
            }

            var group = rail.GetComponent<VerticalLayoutGroup>();
            var spacing = group != null ? group.spacing : 0f;
            var padding = group != null ? group.padding.top + group.padding.bottom : 0f;
            var rowHeight = LayoutUtility.GetPreferredHeight(first.transform as RectTransform);

            le.preferredHeight = activeCount * rowHeight + (activeCount - 1) * spacing + padding;
        }


        // 서버 행을 담는 전용 컨테이너. 이름으로 확인한다 —
        //   블록의 부모를 무조건 레일로 간주하면, 프리팹 구조가 바뀌었을 때(블록이 ContensArea
        //   직속인 예전 구조 등) 엉뚱한 컨테이너를 탭 목록으로 옮겨버린다.
        private const string ServerRailName = "ServerRail";

        private Transform ResolveServerRail()
        {
            var first = ncuServerBlocks != null && ncuServerBlocks.Length > 0 ? ncuServerBlocks[0] : null;
            var parent = first != null ? first.transform.parent : null;
            return parent != null && parent.name == ServerRailName ? parent : null;
        }

        // 서버 행은 탭 목록 "맨 아래"가 아니라 선택된 탭 "바로 아래"에 끼어든다.
        //   (시안: RB 탭을 누르면 그 밑에 SERVERS → Card/Token 행이 펼쳐지고, 그 아래로 나머지 탭이 밀린다)
        //   탭은 런타임에 Instantiate되므로 위치도 런타임에 잡아야 한다.
        private void MoveServerRailUnderSelectedTab(bool visible)
        {
            var first = ncuServerBlocks[0];
            if (first == null)
            {
                return;
            }

            var rail = ResolveServerRail();
            if (rail == null)
            {
                return;
            }

            rail.gameObject.SetActive(visible);
            if (!visible || _selectedEventBannerItem == null)
            {
                return;
            }

            var tab = _selectedEventBannerItem.transform;
            if (tab.parent == null)
            {
                return;
            }

            if (rail.parent != tab.parent)
            {
                rail.SetParent(tab.parent, false);
            }

            // SetSiblingIndex는 "빼고 나서 넣는다". 레일이 탭보다 위에 있으면 빠지는 순간
            //   탭이 한 칸 당겨지므로 +1을 하면 다음 탭 뒤로 넘어간다(펫팝→RB에서 COMING SOON 아래로 가던 버그).
            //   레일이 탭보다 아래면 당겨짐이 없어 +1이 맞다.
            var tabIndex = tab.GetSiblingIndex();
            var railIndex = rail.GetSiblingIndex();
            rail.SetSiblingIndex(railIndex < tabIndex ? tabIndex : tabIndex + 1);

            // 탭 스크롤 뷰포트에는 Mask가 걸려 있다. 런타임에 부모를 그 아래로 옮기면
            //   스텐실이 자동으로 갱신되지 않아 TMP 글자가 렌더링에서 빠진다(이미지는 멀쩡히 보인다).
            //   옮긴 뒤 한 번 강제로 다시 계산해준다.
            foreach (var graphic in rail.GetComponentsInChildren<MaskableGraphic>(true))
            {
                graphic.RecalculateMasking();
            }
        }

        private NcuProjectView FindProjectView(string projectId)
        {
            if (_ncuProjectViews == null || string.IsNullOrEmpty(projectId))
            {
                return null;
            }

            foreach (var view in _ncuProjectViews)
            {
                if (view.ProjectId == projectId)
                {
                    return view;
                }
            }

            return null;
        }

        // 탭 → projectId. 탭은 라이브에셋 배너 하나당 하나이며, 배너 데이터로만 판별한다.
        //   RB의 web2/web3를 각각 탭으로 두려면 배너를 2개로 납품해야 한다
        //   (한 배너를 복제해 2탭으로 만드는 방식은 이미지·URL·상세가 같아져서 걷어냈다).
        private string ResolveProjectIdForItem(EventBannerItem item)
        {
            return item == null ? null : ResolveProjectId(item.Data);
        }

        private static string ResolveProjectId(EventNoticeData data)
        {
            if (data == null)
            {
                return null;
            }

            return NcuLinkStatusView.ResolveProjectId(data.Description)
                   ?? NcuLinkStatusView.ResolveProjectId(data.Url);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private void LogNcuLinkStatus(ApiClient.NcuServiceManager.NcuLinkStatusResponse status)
        {
            if (status.Projects == null)
            {
                return;
            }

            foreach (var project in status.Projects)
            {
                if (project == null || project.Connections == null)
                {
                    continue;
                }

                foreach (var connection in project.Connections)
                {
                    if (connection == null)
                    {
                        continue;
                    }

                    NcDebug.Log(
                        $"[NcuPopup] {project.ProjectId}/{connection.Key} linked={connection.Linked} " +
                        $"address={connection.Address} nickname={connection.Nickname} error={connection.Error}");
                }
            }
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
            RenderSelectedProjectServers();
        }

        private void RenderNotice(EventNoticeData data)
        {
            if (data is not null && data.PopupImage is not null)
            {
                eventView.Set(data.PopupImage, data.Url, data.UseAgentAddress, data.WithSign, data.ButtonType, data.InGameNavigationData);
            }
            else
            {
                NcDebug.LogWarning($"[{nameof(NcuPopup)}] PopupImage is null for {data?.Description ?? "null data"}, using coming soon sprite");
                eventView.Set(comingSoonNoticeSprite);
            }
        }
    }
}
