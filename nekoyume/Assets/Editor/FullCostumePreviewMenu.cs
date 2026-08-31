using Nekoyume.Game.Avatar;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.EditorTools
{
    /// <summary>
    /// 장착한 풀코스튬과 무관하게 지정한 코스튬으로 렌더링하도록 강제한다.
    ///
    /// 왜 필요한가: 신규 코스튬은 스파인 에셋과 Addressables 등록이 끝나도
    /// CostumeItemSheet 행이 없으면 아이템으로 존재하지 않아 장착할 수가 없다.
    /// 외형만 먼저 확인하려면 아이템 없이 스켈레톤을 붙여볼 수단이 필요하다.
    ///
    /// 어디를 가로채는가: 풀코스튬 외형은 전부
    /// <see cref="AvatarSpineController.UpdateFullCostume"/> 에서
    /// "{id}_SkeletonData" 주소로 해석되므로, 그 id 하나만 바꿔치기하면 된다.
    /// 훅과 이 메뉴 모두 UNITY_EDITOR 전용이라 플레이어 빌드에는 들어가지 않는다.
    /// </summary>
    public static class FullCostumePreviewMenu
    {
        private const string MenuRoot = "Tools/Costume/Full Costume Preview/";

        /// <summary>
        /// 미리보기 대상. 신규 추가분(40100066~40100069)을 우선 노출한다.
        /// </summary>
        private static readonly int[] Candidates = { 40100066, 40100067, 40100068, 40100069 };

        [MenuItem(MenuRoot + "40100066")]
        private static void Set66() => Apply(Candidates[0]);

        [MenuItem(MenuRoot + "40100067")]
        private static void Set67() => Apply(Candidates[1]);

        [MenuItem(MenuRoot + "40100068")]
        private static void Set68() => Apply(Candidates[2]);

        [MenuItem(MenuRoot + "40100069")]
        private static void Set69() => Apply(Candidates[3]);

        [MenuItem(MenuRoot + "Next")]
        private static void Next()
        {
            var current = AvatarSpineController.FullCostumePreviewId;
            var index = current is { } id ? System.Array.IndexOf(Candidates, id) : -1;
            Apply(Candidates[(index + 1) % Candidates.Length]);
        }

        [MenuItem(MenuRoot + "Clear (use equipped costume)")]
        private static void Clear()
        {
            AvatarSpineController.FullCostumePreviewId = null;
            Debug.Log("[FullCostumePreview] 해제 — 장착한 코스튬을 그대로 사용한다.");
        }

        private static void Apply(int id)
        {
            AvatarSpineController.FullCostumePreviewId = id;
            Debug.Log($"[FullCostumePreview] {id} 로 고정. 씬에 있는 캐릭터에 즉시 반영한다.");

            // 이미 렌더링 중인 캐릭터는 다시 장착하지 않으면 갱신되지 않으므로 직접 갱신한다.
            var controllers = Object.FindObjectsOfType<AvatarSpineController>(true);
            foreach (var controller in controllers)
            {
                controller.UpdateFullCostume(id, false);
            }

            Debug.Log($"[FullCostumePreview] 갱신한 캐릭터 {controllers.Length}개." +
                      " 0개면 플레이 모드가 아니거나 캐릭터가 아직 생성되지 않은 상태다.");
        }
    }
}
