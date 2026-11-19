using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    /// <summary>
    /// 다음에 열릴 하드 월드 위에 늘 떠 있는 개방 조건 말풍선. 조건 진행도와 열릴 때까지 남은 기간을 보여 준다.
    /// </summary>
    /// <remarks>
    /// 개방 행은 조건을 못 채워도 최대 대기일에 열리고, 조건이 확정되면 그보다 일찍 열린다. 그래서 기간 줄은 늘
    /// <see cref="WorldOpenStatus.OpenBlockIndex"/> 까지 남은 블록이고, 이름으로 구분한다("최대 대기" / "확정 대기" / "개방 확정").
    /// 체인에서 열린 뒤 아바타가 아직 해금하지 않았으면 두 줄 모두 달성으로 두고 제목 자리에 "눌러서 열기"를 띄운다 — 말풍선은 월드 버튼의 자식이라 누르면 버튼의
    /// 기존 해금(UnlockWorld) 흐름을 탄다.
    /// 프리팹은 <c>Tools/WorldMap/개방 조건 말풍선 붙이기</c> 메뉴가 WorldButton 에 붙인다.
    /// </remarks>
    public class WorldOpenStatusBubble : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        /// 편집기 미리보기 메뉴가 말풍선을 띄워 둔 동안 true. 월드맵의 블록마다 갱신이 미리보기를 덮어쓰지 않게 한다.
        /// </summary>
        public static bool PreviewActive;

        // 도메인 리로드를 끈 편집기에서도 플레이를 새로 시작하면 미리보기를 끈다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPreview() => PreviewActive = false;
#endif

        [SerializeField][Tooltip("말풍선 바탕. \"눌러서 열기\"일 때만 눌림을 받는다 — 평소에는 아래 지역의 클릭을 가로채지 않게.")]
        private Image background;

        [SerializeField][Tooltip("글자 영역. 꼬리 방향에 따라 꼬리를 뺀 몸통 쪽으로 옮긴다.")]
        private RectTransform body;

        [SerializeField][Tooltip("켜면 말풍선이 버튼 아래쪽에 꼬리를 위로 하고 뜬다 — 지역 위에 자리가 없을 때(니플헤임 등). " +
            "위치(Anchored Position)는 그 기준에서의 오프셋이다.")]
        private bool tailUp;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private GameObject conditionRow;

        [SerializeField]
        private TMP_Text conditionLabelText;

        [SerializeField]
        private TMP_Text conditionValueText;

        [SerializeField]
        private TMP_Text periodLabelText;

        [SerializeField]
        private TMP_Text periodValueText;

        [SerializeField]
        private Color titleColor = new(0.94f, 0.89f, 0.80f);

        [SerializeField]
        private Color tapToOpenColor = new(1f, 0.85f, 0.54f);

        [SerializeField]
        private Color valueColor = new(0.94f, 0.89f, 0.80f);

        [SerializeField]
        private Color doneColor = new(0.62f, 0.83f, 0.53f);

        private void Awake()
        {
            ApplyTail();
        }

#if UNITY_EDITOR
        // OnValidate 안에서 RectTransform 을 바꾸면 경고가 나고 프리팹 덮어쓰기가 생긴다. 다음 편집기 틱으로 미룬다.
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyTail();
                }
            };
        }
#endif

        /// <summary>
        /// 바탕 그림은 꼬리가 아래(17%)에 있다. 꼬리를 위로 하려면 말풍선을 버튼 아래쪽에 붙이고, 바탕만 위아래로 뒤집고,
        /// 글자 영역을 몸통 쪽으로 옮긴다.
        /// </summary>
        private void ApplyTail()
        {
            // 기준점: 꼬리 아래면 버튼 위쪽 가운데에 말풍선 아래 가운데, 꼬리 위면 버튼 아래쪽 가운데에 말풍선 위 가운데.
            // 값이 같으면 쓰지 않는다 — 편집기에서 프리팹 인스턴스에 덮어쓰기가 괜히 생기지 않게.
            var rect = (RectTransform)transform;
            var anchor = new Vector2(0.5f, tailUp ? 0f : 1f);
            if (rect.anchorMin != anchor || rect.anchorMax != anchor)
            {
                rect.anchorMin = rect.anchorMax = anchor;
            }

            var pivot = new Vector2(0.5f, tailUp ? 1f : 0f);
            if (rect.pivot != pivot)
            {
                rect.pivot = pivot;
            }

            if (background != null)
            {
                var scale = background.rectTransform.localScale;
                var y = tailUp ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
                if (!Mathf.Approximately(scale.y, y))
                {
                    background.rectTransform.localScale = new Vector3(scale.x, y, scale.z);
                }
            }

            if (body != null)
            {
                var min = new Vector2(body.anchorMin.x, tailUp ? 0.04f : 0.21f);
                var max = new Vector2(body.anchorMax.x, tailUp ? 0.79f : 0.96f);
                if (body.anchorMin != min || body.anchorMax != max)
                {
                    body.anchorMin = min;
                    body.anchorMax = max;
                }
            }
        }

        /// <summary>아직 열리지 않은 구간의 조건과 남은 기간을 그린다. <paramref name="status"/> 가 null 이면 숨긴다.</summary>
        public void Show(WorldOpenStatus status)
        {
            if (status is null)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            SetClickable(false);
            titleText.text = L10nManager.Localize(
                "UI_WORLD_OPEN_CONDITION_TITLE",
                status.StageBegin,
                status.StageEnd);
            titleText.color = titleColor;

            // 조건 없는 개방(최대 대기일에만 열림)은 조건 줄을 뺀다.
            var hasCondition = status.Target > 0;
            // 기준치에 닿았어도 체인 확정은 다음 개방 시트 패치 때 된다. 그 사이는 "확정 대기"로, 남은 기간은 아직 최대 대기일 기준이다.
            var pending = hasCondition && !status.ConditionMet && status.Current >= status.Target;
            conditionRow.SetActive(hasCondition);
            if (hasCondition)
            {
                conditionLabelText.text = L10nManager.Localize("UI_WORLD_OPEN_FIRST_CLEAR");
                SetValue(
                    conditionValueText,
                    status.ConditionMet || pending,
                    $"{status.Current:N0} / {status.Target:N0}");
            }

            // 같은 남은 기간이라도 조건 확정 전에는 최대 대기일, 확정 뒤에는 앞당겨진 개방일이다. 어느 쪽인지 이름으로 구분한다.
            periodLabelText.text = L10nManager.Localize(
                !hasCondition ? "UI_WORLD_OPEN_PERIOD"
                : status.ConditionMet ? "UI_WORLD_OPEN_PERIOD_CONFIRMED"
                : pending ? "UI_WORLD_OPEN_PERIOD_PENDING"
                : "UI_WORLD_OPEN_PERIOD_FALLBACK");
            SetValue(
                periodValueText,
                false,
                L10nManager.Localize(
                    "UI_WORLD_OPEN_REMAINING",
                    status.RemainingBlocks.ToString("N0"),
                    status.RemainingBlocks.BlockRangeToTimeSpanString(true)));
        }

        /// <summary>체인에서 열렸고 아바타가 아직 해금하지 않았다. 두 줄 모두 달성, "눌러서 열기".</summary>
        public void ShowTapToOpen()
        {
            gameObject.SetActive(true);
            SetClickable(true);
            titleText.text = L10nManager.Localize("UI_WORLD_OPEN_TAP");
            titleText.color = tapToOpenColor;
            conditionRow.SetActive(true);
            conditionLabelText.text = L10nManager.Localize("UI_WORLD_OPEN_FIRST_CLEAR");
            SetValue(conditionValueText, true, null);
            periodLabelText.text = L10nManager.Localize("UI_WORLD_OPEN_PERIOD");
            SetValue(periodValueText, true, null);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetClickable(bool clickable)
        {
            if (background != null)
            {
                background.raycastTarget = clickable;
            }
        }

        private void SetValue(TMP_Text text, bool done, string value)
        {
            text.text = done ? L10nManager.Localize("UI_WORLD_OPEN_DONE") : value;
            text.color = done ? doneColor : valueColor;
        }
    }
}
