using System.Collections.Generic;
using Nekoyume.ApiClient;

namespace Nekoyume.UI.Module.Ncu
{
    // (NCU) 인게임 팝업 "무엇이 언제 보이는지" 규칙 — 와이어프레임(b9a17fa5)의 상태/분기를 코드로.
    //   포탈 by-address 응답(NcuLinkStatusResponse)을 팝업 표시 모델로 변환한다.
    //   프리팹/스프라이트/치수는 에디터 소관, 여기서는 상태·분기·매핑만 결정한다.

    // 서버 연결 상태 — 와이어프레임 "탭 점 표기" 및 서버 블록 상태.
    public enum NcuConnectionState
    {
        Unlinked, // 미연동 — 빈 원
        Linked,   // 연동됨 — 채움
        Querying, // 조회 중 — 점선 점멸
        Failed,   // 조회 실패 — 사선
    }

    // 빌드/노출 프로파일. K버전(#3, 사행성 게이트)은 별도 논의 → 지금은 Global만 사용.
    public readonly struct NcuBuildProfile
    {
        public readonly bool ShowLinkElements;   // 연동상태/식별자/뱃지/탭점/연동버튼 노출 (K버전=false)
        public readonly bool IncludeTokenServer; // 토큰(web3) 서버 노출 (K버전=false)
        public readonly bool RbTabVisible;       // RB 탭 자체 노출

        public NcuBuildProfile(bool showLinkElements, bool includeTokenServer, bool rbTabVisible)
        {
            ShowLinkElements = showLinkElements;
            IncludeTokenServer = includeTokenServer;
            RbTabVisible = rbTabVisible;
        }

        // 글로벌(PC·모바일): 모든 요소 노출.
        public static NcuBuildProfile Global => new NcuBuildProfile(true, true, true);

        // K버전: 연동요소 전면 숨김 + 토큰서버 제외. restrictAllExternal이면 RB 탭 자체 제거.
        //   ⚠️ 실제 빌드 감지/적용은 별도 논의(#3) 전까지 사용하지 않음 — 규칙만 여기 박제.
        public static NcuBuildProfile KVersion(bool restrictAllExternal)
        {
            return new NcuBuildProfile(false, false, !restrictAllExternal);
        }
    }

    // 우측 상세의 서버 블록 1개.
    public class NcuServerBlockView
    {
        public string ConnectionKey;    // ragnarok_wallet / abstract_wallet / petpop
        public NcuConnectionState State;
        public string NameL10nKey;      // "카드 결제 서버" 등 (서버 라벨)
        public string DescL10nKey;      // "카드로 결제" 등 (없으면 null)
        public bool ShowServerLabel;    // 행 제목 노출. 시안 B에서는 항상 켠다(행=이름+연동여부)
        public string GameId;           // RB-1A2B33 — 포탈 API #2 전까지 빈 값(있으면 표시)
        public string WalletAddress;    // 0x… 전문(표시 축약은 컴포넌트가) — 없으면 숨김
        public bool ShowLinkElements;   // 식별자/뱃지/연동버튼 노출(K버전=false)
        public string LinkUrl;          // "연동하러 가기"(포탈) — 미연동 && CanLink일 때
        public bool CanLink;            // 연동 버튼 노출(미연동 && ShowLinkElements && 포탈연동 지원)
    }

    // 좌측 탭(프로젝트) 1개 + 우측 상세 구성.
    public class NcuProjectView
    {
        public string ProjectId;
        public bool ShowNotice; // "두 서버는 계정과 진행 상황이 공유되지 않습니다"(서버 2개↑)
        public readonly List<NcuServerBlockView> Servers = new List<NcuServerBlockView>();
    }

    public static class NcuLinkStatusView
    {
        // 프로젝트/연결 키 — 포탈 응답 및 [[project_portal_ncu_link_status]]와 동일.
        public const string ProjectRagnarok = "ragnarok_breaker";
        public const string ProjectPetpop = "petpop";
        public const string ConnRagnarokWallet = "ragnarok_wallet"; // 카드 결제 서버(web2)
        public const string ConnAbstractWallet = "abstract_wallet"; // 토큰 결제 서버(web3)
        // RB는 web2(V8)/web3(Abstract)를 각각 1탭으로 분리 — 탭(뷰) id.
        //   응답 projectId는 여전히 ProjectRagnarok(연결 조회용). 이건 좌측 탭 매핑용.
        public const string ProjectRbWeb2 = "ragnarok_breaker_web2";
        public const string ProjectRbWeb3 = "ragnarok_breaker_web3";

        // L10n 키(신규는 handoff에서 CSV 추가). 배지는 기존 키 재활용.
        // 서버 이름은 결제수단(카드/토큰)이 아니라 서버 이름 자체로 부른다.
        //   web2 = V8(Verse8), web3 = Abstract.
        public const string KeyServerV8 = "UI_NCU_SERVER_V8";
        public const string KeyServerAbstract = "UI_NCU_SERVER_ABSTRACT";
        public const string KeyServerCardDesc = "UI_NCU_SERVER_CARD_DESC";
        public const string KeyServerTokenDesc = "UI_NCU_SERVER_TOKEN_DESC";
        public const string KeyNoticeNotShared = "UI_NCU_NOTICE_NOT_SHARED";
        public const string KeyHintLinkReward = "UI_NCU_HINT_LINK_REWARD";
        public const string KeyPlay = "UI_NCU_PLAY";
        // 버튼 문구도 연동 여부에 따라 갈라 쓴다. 배지와 같은 말을 반복하지 않으면서
        //   "처음 가는 것"과 "이어서 하는 것"의 어감이 언어마다 다르기 때문에 키를 나눈다.
        public const string KeyPlayLinked = "UI_NCU_PLAY_LINKED";
        public const string KeyPlayUnlinked = "UI_NCU_PLAY_UNLINKED";
        public const string KeyLink = "UI_NCU_LINK";
        public const string KeyRetry = "UI_NCU_RETRY";
        public const string KeyLoadFailed = "UI_NCU_LOAD_FAILED";
        public const string KeyPetpop = "UI_NCU_PROJECT_PETPOP";
        // 배지 — NCU 전용 키. 예전엔 공용 UI_CONNECTED/UI_NOT_CONNECTED를 재활용했는데,
        //   그 키는 다른 화면도 쓰기 때문에 문구(Linked/Not linked)를 NCU에 맞추면 그쪽까지 바뀐다.
        public const string KeyBadgeLinked = "UI_NCU_BADGE_LINKED";
        public const string KeyBadgeQuerying = "UI_NCU_BADGE_QUERYING";
        public const string KeyBadgeUnlinked = "UI_NCU_BADGE_UNLINKED";

        // TODO(CMS/서버상수): 포탈 versions[]와 동일한 하드코딩 placeholder. 상수화/CMS화 대상.
        private const string PortalLinkUrl = "https://nine-chronicles.com/ncu"; // 연동하러 가기(포탈)

        // 응답+프로파일 → 프로젝트별 표시 모델. isQuerying=true면 모든 상태를 조회중으로,
        //   isFailed=true면 모두 조회 실패로(재시도 버튼). 둘 다 false면 응답을 그대로 읽는다.
        public static List<NcuProjectView> Build(
            NcuServiceManager.NcuLinkStatusResponse response,
            NcuBuildProfile profile,
            bool isQuerying,
            bool isFailed = false)
        {
            var result = new List<NcuProjectView>();

            // K버전은 연동 요소를 전부 숨긴다. 그러면 행에 서버 이름만 남아 아무 정보도 주지 못하므로
            //   행 자체를 만들지 않는다(레일도 자동으로 숨는다 — 서버 수가 0이면 NcuPopup이 끈다).
            if (!profile.ShowLinkElements)
            {
                return result;
            }

            // --- PETPOP (서버 1개, 라벨/고지 생략, 연동은 펫팝 위임) ---
            var petpopConn = FindConnection(response, ProjectPetpop, ProjectPetpop);
            var petpopView = new NcuProjectView { ProjectId = ProjectPetpop, ShowNotice = false };
            var petpopBlock = new NcuServerBlockView
            {
                ConnectionKey = ProjectPetpop,
                State = DeriveState(petpopConn, isQuerying, isFailed),
                NameL10nKey = KeyPetpop,
                DescL10nKey = null,
                // (시안 B) 행은 이름 + 연동여부 두 줄이다. 예전엔 서버가 1개인 프로젝트의
                //   서버 라벨을 생략했지만, 지금은 모든 탭이 서버 1개라 그 규칙이 의미가 없다.
                //   이름을 숨기면 화면에 상태만 남아 무엇의 상태인지 알 수 없다.
                ShowServerLabel = true,
                GameId = petpopConn != null ? petpopConn.Nickname : null,
                WalletAddress = petpopConn != null ? petpopConn.KaiaWalletAddr : null,
                ShowLinkElements = profile.ShowLinkElements,
                LinkUrl = null,
                CanLink = false, // 연동 생성은 펫팝 소관 — 포탈 "연동하러 가기" 없음
            };
            petpopView.Servers.Add(petpopBlock);
            result.Add(petpopView);

            // --- RAGNAROK BREAKER: web2(V8)/web3(Abstract)를 각각 1탭·1블록으로 분리 ---
            //   (기존 1탭 2블록 → 2탭 각 1블록. 각 탭당 서버 1개라 "미공유" 고지 불필요.)
            if (profile.RbTabVisible)
            {
                result.Add(BuildRbTab(response, ProjectRbWeb2, ConnRagnarokWallet, profile, isQuerying, isFailed));
                // 토큰(web3) 탭은 IncludeTokenServer일 때만(K버전 제외).
                if (profile.IncludeTokenServer)
                {
                    result.Add(BuildRbTab(response, ProjectRbWeb3, ConnAbstractWallet, profile, isQuerying, isFailed));
                }
            }

            return result;
        }

        // RB 단일 서버 탭(web2 or web3) — 탭당 서버 1개(최대 메뉴 1개).
        private static NcuProjectView BuildRbTab(
            NcuServiceManager.NcuLinkStatusResponse response,
            string projectViewId,
            string connKey,
            NcuBuildProfile profile,
            bool isQuerying,
            bool isFailed)
        {
            var view = new NcuProjectView { ProjectId = projectViewId, ShowNotice = false };
            var block = BuildRbBlock(response, connKey, profile, isQuerying, isFailed);
            view.Servers.Add(block);
            return view;
        }

        private static NcuServerBlockView BuildRbBlock(
            NcuServiceManager.NcuLinkStatusResponse response,
            string connKey,
            NcuBuildProfile profile,
            bool isQuerying,
            bool isFailed)
        {
            var conn = FindConnection(response, ProjectRagnarok, connKey);
            var state = DeriveState(conn, isQuerying, isFailed);
            var isCard = connKey == ConnRagnarokWallet;
            return new NcuServerBlockView
            {
                ConnectionKey = connKey,
                State = state,
                NameL10nKey = isCard ? KeyServerV8 : KeyServerAbstract,
                DescL10nKey = isCard ? KeyServerCardDesc : KeyServerTokenDesc,
                ShowServerLabel = true,
                GameId = conn != null ? conn.Nickname : null, // API #2 전까지 빈 값
                WalletAddress = conn != null ? conn.Address : null,
                ShowLinkElements = profile.ShowLinkElements,
                LinkUrl = PortalLinkUrl,
                CanLink = profile.ShowLinkElements && state == NcuConnectionState.Unlinked,
            };
        }

        private static NcuConnectionState DeriveState(
            NcuServiceManager.NcuConnection conn, bool isQuerying, bool isFailed)
        {
            if (isQuerying)
            {
                return NcuConnectionState.Querying;
            }
            // 응답 자체가 없는 경우. conn == null 로 흘려보내면 "미연동"이 되어버린다.
            if (isFailed)
            {
                return NcuConnectionState.Failed;
            }
            if (conn == null)
            {
                return NcuConnectionState.Unlinked;
            }
            if (conn.Error)
            {
                return NcuConnectionState.Failed;
            }
            return conn.Linked ? NcuConnectionState.Linked : NcuConnectionState.Unlinked;
        }

        private static NcuServiceManager.NcuConnection FindConnection(
            NcuServiceManager.NcuLinkStatusResponse response, string projectId, string connKey)
        {
            if (response == null || response.Projects == null)
            {
                return null;
            }
            foreach (var project in response.Projects)
            {
                if (project == null || project.ProjectId != projectId || project.Connections == null)
                {
                    continue;
                }
                foreach (var conn in project.Connections)
                {
                    if (conn != null && conn.Key == connKey)
                    {
                        return conn;
                    }
                }
            }
            return null;
        }

        // 버튼 라벨 키(상태별). 조회 중/실패는 연동 여부를 아직 모르므로 중립 문구로 둔다.
        public static string PlayLabelKey(NcuConnectionState state)
        {
            switch (state)
            {
                case NcuConnectionState.Linked: return KeyPlayLinked;
                case NcuConnectionState.Unlinked: return KeyPlayUnlinked;
                default: return KeyPlay;
            }
        }

        // 배지 라벨 키(상태별). Failed는 신규 키.
        public static string BadgeKey(NcuConnectionState state)
        {
            switch (state)
            {
                case NcuConnectionState.Linked: return KeyBadgeLinked;
                case NcuConnectionState.Querying: return KeyBadgeQuerying;
                case NcuConnectionState.Failed: return KeyLoadFailed;
                default: return KeyBadgeUnlinked;
            }
        }

        // 좌측 배너(EventNoticeData)를 프로젝트 id로 매핑(best-effort).
        //   ⚠️ 콘텐츠 컨벤션 확정 필요(handoff #1). Description/Url 부분일치로 임시 매핑.
        public static string ResolveProjectId(string descriptionOrUrl)
        {
            if (string.IsNullOrEmpty(descriptionOrUrl))
            {
                return null;
            }
            var s = descriptionOrUrl.ToLowerInvariant();
            if (s.Contains("petpop"))
            {
                return ProjectPetpop;
            }
            // RB web2/web3 2탭 구분 — 더 구체적인 web3(abstract) 먼저.
            if (s.Contains("abstract") || s.Contains("web3"))
            {
                return ProjectRbWeb3;
            }
            if (s.Contains("verse8") || s.Contains("web2") || s.Contains("v8"))
            {
                return ProjectRbWeb2;
            }
            // web2/web3 미구분 RB 배너는 web2 탭 기본.
            //   ⚠️ 2탭이 다 뜨려면 콘텐츠(LiveAsset)에 web2/web3 구분되는 RB 배너 2개 필요.
            if (s.Contains("ragnarok") || s.Contains("breaker"))
            {
                return ProjectRbWeb2;
            }
            return null;
        }
    }
}
