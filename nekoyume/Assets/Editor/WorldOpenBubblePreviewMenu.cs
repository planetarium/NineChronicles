using System.Collections.Generic;
using System.Linq;
using Nekoyume;
using Nekoyume.Helper;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    /// <summary>
    /// 체인에 열리지 않은 구간이 없어도 하드월드 개방 조건 말풍선의 모양·위치를 확인한다(플레이 중).
    /// </summary>
    /// <remarks>
    /// 계층 창에서 월드 버튼(또는 그 자식)을 고르면 그 버튼에, 아무것도 고르지 않았으면 하드 월드 버튼 전부에 예시 숫자로
    /// 말풍선을 띄운다. 하드 모드는 일반 월드 버튼 객체를 자리 순서대로 다시 쓰므로(10월드 = YggdrasilWorldButton,
    /// 11월드 = AlfheimButton, 12월드 = SvartalfheimButton …), 시트에 하드 월드가 없어 꺼져 있는 자리도 잠시 켜서 배치를
    /// 볼 수 있게 한다. 이름이 Hard 로 시작하는 버튼은 지금 화면에 쓰이지 않는 옛 배치라 건드리지 않는다.
    /// 띄워 둔 동안은 월드맵의 블록마다 갱신을 멈춘다 — '끄기'로 되돌린다.
    /// 위치를 손볼 때는 플레이를 멈추고 UI_WorldMap 의 그 버튼 아래 OpenStatusBubble 을 옮긴다(플레이 중 수정은 남지 않는다).
    /// </remarks>
    public static class WorldOpenBubblePreviewMenu
    {
        private const string MenuRoot = "Tools/WorldMap/개방 조건 말풍선 미리보기/";

        /// <summary>미리보기 때문에 켠 하드 월드 자리. '끄기'로 다시 끈다.</summary>
        private static readonly List<WorldButton> Activated = new();

        [MenuItem(MenuRoot + "대기 (742 ／ 1,000)")]
        private static void Waiting() =>
            Apply(button => button.ShowOpenStatus(Status(button, false, 742, 1000, 172_800)));

        [MenuItem(MenuRoot + "기준 도달, 확정 대기")]
        private static void Pending() =>
            Apply(button => button.ShowOpenStatus(Status(button, false, 1000, 1000, 172_800)));

        [MenuItem(MenuRoot + "조건 달성, 열림 대기")]
        private static void ConditionMet() =>
            Apply(button => button.ShowOpenStatus(Status(button, true, 1000, 1000, 25_920)));

        [MenuItem(MenuRoot + "눌러서 열기")]
        private static void TapToOpen() => Apply(button => button.ShowOpenStatusTapToOpen());

        [MenuItem(MenuRoot + "새 지역 개방 팝업")]
        private static void OpenedPopup()
        {
            var selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<WorldButton>()
                : null;
            WorldMap.ShowWorldOpenedPopup(selected != null && selected.Id > 0 ? selected.Id : 12);
        }

        [MenuItem(MenuRoot + "끄기")]
        private static void Off()
        {
            foreach (var button in Targets(all: true))
            {
                button.HideOpenStatus();
            }

            foreach (var button in Activated.Where(button => button != null))
            {
                button.gameObject.SetActive(false);
            }

            Activated.Clear();
            WorldOpenStatusBubble.PreviewActive = false;
        }

        [MenuItem(MenuRoot + "대기 (742 ／ 1,000)", true)]
        [MenuItem(MenuRoot + "기준 도달, 확정 대기", true)]
        [MenuItem(MenuRoot + "조건 달성, 열림 대기", true)]
        [MenuItem(MenuRoot + "눌러서 열기", true)]
        [MenuItem(MenuRoot + "새 지역 개방 팝업", true)]
        [MenuItem(MenuRoot + "끄기", true)]
        private static bool IsPlaying() => Application.isPlaying;

        private static void Apply(System.Action<WorldButton> show)
        {
            var targets = Targets(all: false).ToList();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[WorldOpen] 보이는 월드 버튼이 없다. 월드맵을 하드 모드로 연 뒤 실행한다.");
                return;
            }

            WorldOpenStatusBubble.PreviewActive = true;
            foreach (var button in targets)
            {
                show(button);
            }

            LogState();
        }

        /// <summary>월드 버튼마다 켜짐·월드 id·말풍선 제목을 콘솔에 남긴다(배치가 이상할 때 원인 찾기용).</summary>
        private static void LogState()
        {
            var lines = Object.FindObjectsOfType<WorldButton>(true)
                .OrderBy(button => button.transform.GetSiblingIndex())
                .Select(button =>
                {
                    var title = button.GetComponentInChildren<WorldOpenStatusBubble>(true)?
                        .transform.Find("Body/Title")?.GetComponent<TMPro.TMP_Text>();
                    return $"{button.transform.parent?.name}/{button.name} self={button.gameObject.activeSelf} " +
                        $"hierarchy={button.gameObject.activeInHierarchy} id={button.Id} " +
                        $"title=\"{title?.text}\" titleActive={title?.gameObject.activeInHierarchy} " +
                        $"titleAlpha={title?.color.a} titleSize={title?.fontSize} rect={((RectTransform)title?.transform)?.rect}";
                });
            Debug.Log("[WorldOpen] preview\n" + string.Join("\n", lines));
        }

        /// <summary>고른 월드 버튼, 없으면 보이는 하드 월드 버튼(10월드부터) 전부.</summary>
        private static IEnumerable<WorldButton> Targets(bool all)
        {
            var selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<WorldButton>()
                : null;
            if (!all && selected != null)
            {
                return new[] { selected };
            }

            var buttons = Object.FindObjectsOfType<WorldButton>(true);
            if (all)
            {
                return buttons;
            }

            foreach (var slot in buttons.Where(button =>
                         !button.IsShown && IsWorldSlot(button) && button.transform.parent.gameObject.activeInHierarchy))
            {
                slot.gameObject.SetActive(true);
                Activated.Add(slot);
            }

            return buttons.Where(button =>
                button.IsShown &&
                IsWorldSlot(button) &&
                (Activated.Contains(button) || (button.Id >= 10 && button.Id < GameConfig.MimisbrunnrWorldId)));
        }

        /// <summary>월드맵이 월드 자리로 쓰는 버튼(이벤트 던전·탑·옛 Hard 배치 제외).</summary>
        private static bool IsWorldSlot(WorldButton button)
        {
            var name = button.name;
            return !name.StartsWith("Hard") &&
                !name.StartsWith("EventDungeon") &&
                !name.StartsWith("InfiniteTower") &&
                !name.StartsWith("AdventureBoss") &&
                button.GetComponentInChildren<WorldOpenStatusBubble>(true) != null;
        }

        private static WorldOpenStatus Status(
            WorldButton button,
            bool conditionMet,
            long current,
            long target,
            long remainingBlocks) =>
            WorldOpenStatus.CreateForPreview(
                button.Id,
                button.StageBegin > 0 ? button.StageBegin : 551, // 시트에 없는 자리는 예시 범위
                button.StageBegin > 0 ? button.StageEnd : 575,
                conditionMet,
                current,
                target,
                remainingBlocks);
    }
}
