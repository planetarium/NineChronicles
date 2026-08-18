using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Ncu
{
    // (NCU) 좌측 리스트에서 선택된 탭 아래에 붙는 서버 행 1개.
    //   행 = 상태점 + 서버 이름 + 연동여부 + 액션 버튼 하나(Wallet / Link / Retry).
    //   NcuServerBlockView로부터 렌더하며, 프리팹 ref는 부분 배선 가능 — 전부 null-guard.
    public class NcuServerBlock : MonoBehaviour
    {
        [Header("Label")]
        [SerializeField] private GameObject serverLabelRoot; // 서버 1개(PETPOP)면 비활성
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Link elements (K버전에선 전면 숨김)")]
        [SerializeField] private GameObject linkElementsRoot;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private GameObject idsRoot;
        [SerializeField] private GameObject hintRoot;

        [Header("Row (시안 B)")]
        [SerializeField] private Image stateDot;        // 연동=채움 / 미연동=빈 원
        [SerializeField] private Sprite dotFilledSprite;
        [SerializeField] private Sprite dotHollowSprite;
        [SerializeField] private Color dotLinkedColor = new Color(0.85f, 0.70f, 0.30f);
        [SerializeField] private Color dotUnlinkedColor = new Color(0.55f, 0.57f, 0.60f);
        [SerializeField] private Color dotFailedColor = new Color(0.85f, 0.35f, 0.18f);

        // 단일 액션 버튼 — 라벨/동작이 상태에 따라 바뀐다(Wallet ↔ Link ↔ Retry).
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionLabel;

        [Header("State overlays")]
        [SerializeField] private GameObject skeletonRoot; // 조회 중

        private string _walletFull;

        public void Set(NcuServerBlockView view, System.Action onRetry)
        {
            if (view == null)
            {
                return;
            }

            // --- 서버 라벨 ---
            //   (시안 B) 행은 "이름 + 연동여부" 두 줄이다. 설명("카드로 결제")은 넣을 자리가 없고
            //   넣으면 44px 행에서 세 줄이 되어 서로 포개진다. 이름이 상황을 이미 말한다.
            SetActiveSafe(serverLabelRoot, view.ShowServerLabel);
            SetText(nameText, Localize(view.NameL10nKey));

            var querying = view.State == NcuConnectionState.Querying;
            var failed = view.State == NcuConnectionState.Failed;

            // --- 연동 요소(뱃지/식별자/힌트) — K버전은 통째로 숨김 ---
            SetActiveSafe(linkElementsRoot, view.ShowLinkElements);
            if (view.ShowLinkElements)
            {
                SetActiveSafe(badgeRoot, true);
                SetText(badgeText, Localize(NcuLinkStatusView.BadgeKey(view.State)));

                // (시안 B) 행에는 "연동됐는지 아닌지"만 남긴다.
                //   게임ID·지갑주소·복사·힌트는 좁은 좌측 행에서 읽히지도 않고 자리도 없어
                //   Wallet 버튼 뒤(다음 화면)로 넘긴다. 값 자체는 계속 들고 있는다.
                _walletFull = view.WalletAddress;
                SetActiveSafe(idsRoot, false);
                SetActiveSafe(hintRoot, false);
            }
            else
            {
                // K버전 등 연동요소 전면 숨김 — 계층 가정에 기대지 않고 명시적으로 끈다.
                //   badgeRoot가 배선되지 않은 프리팹도 있으므로 텍스트 오브젝트도 직접 끈다.
                //   (안 끄면 이전 값이나 프리팹 기본값이 그대로 남아 화면에 보인다)
                SetActiveSafe(badgeRoot, false);
                SetActiveSafe(badgeText != null ? badgeText.gameObject : null, false);
                SetActiveSafe(idsRoot, false);
                SetActiveSafe(hintRoot, false);
            }

            // --- 조회 중 스켈레톤 ---
            SetActiveSafe(skeletonRoot, querying);

            // --- 상태 점 ---
            if (stateDot != null)
            {
                stateDot.gameObject.SetActive(view.ShowLinkElements);
                var linked = view.State == NcuConnectionState.Linked;
                if (dotFilledSprite != null && dotHollowSprite != null)
                {
                    stateDot.sprite = linked ? dotFilledSprite : dotHollowSprite;
                }
                stateDot.color = failed
                    ? dotFailedColor
                    : linked ? dotLinkedColor : dotUnlinkedColor;
            }

            // --- 단일 액션 버튼 ---
            //   시안은 버튼이 하나뿐이고 상태에 따라 라벨만 바뀐다.
            //   연동됨=Wallet / 미연동=Link / 실패=Retry / 조회중=숨김.
            if (actionButton != null)
            {
                string labelKey = null;
                System.Action onClick = null;

                if (failed)
                {
                    labelKey = NcuLinkStatusView.KeyRetry;
                    onClick = () => onRetry?.Invoke();
                }
                else if (view.CanLink && view.State == NcuConnectionState.Unlinked)
                {
                    labelKey = NcuLinkStatusView.KeyLink;
                    onClick = () => OpenUrl(view.LinkUrl);
                }
                else if (view.State == NcuConnectionState.Linked && view.ShowLinkElements)
                {
                    // TODO: 시안의 "다음 화면"이 생기면 그쪽으로. 그전까지는 주소 복사.
                    labelKey = NcuLinkStatusView.KeyWallet;
                    onClick = CopyWallet;
                }

                var showAction = labelKey != null && !querying;
                SetActiveSafe(actionButton.gameObject, showAction);
                if (showAction)
                {
                    SetText(actionLabel, Localize(labelKey));
                    Wire(actionButton, () => onClick?.Invoke());
                }
            }
        }

        private void CopyWallet()
        {
            if (string.IsNullOrEmpty(_walletFull))
            {
                return;
            }
            Helper.ClipboardHelper.CopyToClipboard(_walletFull);
            OneLineSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_COPIED"),
                NotificationCell.NotificationType.Notification);
        }

        private static void OpenUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Helper.Util.OpenURL(url);
            }
        }


        private static string Localize(string key)
        {
            return string.IsNullOrEmpty(key) ? null : L10nManager.Localize(key);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction onClick)
        {
            if (button == null)
            {
                return;
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text == null)
            {
                return;
            }
            text.text = value ?? string.Empty;
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }
}
