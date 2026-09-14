using Nekoyume.Game.Avatar;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    /// <summary>
    /// 로비 캐릭터의 아우라 VFX 를 바로 갈아 끼워 보는 미리보기 도구.
    /// </summary>
    /// <remarks>
    /// 아이템을 실제로 장착하지 않고 <c>VFX_Aura.asset</c> 의 id → 프리팹 매핑만 태운다.
    /// 그래서 시트에 해당 등급 행이 없어도, 인벤토리에 아이템이 없어도 확인할 수 있다.
    /// 등급 색 변형 프리팹을 만들었을 때 G8 과 나란히 비교하는 용도다.
    /// <para>
    /// 캐릭터 외형이 다시 갱신되면(장비 변경 등) 미리보기는 원래 아우라로 돌아간다.
    /// </para>
    /// </remarks>
    public static class AuraPreviewMenu
    {
        private const string Root = "Tools/아우라 미리보기/";

        // 아우라 id 는 106{등급}000{변형} 이다 — 10680000~10680003 이 G8 4종.
        // 기획서의 `{부위}{등급}{변형}000` 규칙은 5부위 장비(10180000 등)용이고 아우라엔 안 맞는다.
        //
        // 아래 변형 번호는 **G8/G9 기준**이다. 하위 등급은 배치가 다르다 —
        // 예를 들어 G4 는 변형 4가 ice 이고, G5 는 같은 계열이 여러 번 나온다.
        // G7 이하를 이 메뉴에 추가하려면 그 등급의 실제 배치를 확인해야 한다.
        private const int Ymir = 0;
        private const int Beast = 1;
        private const int Aegis = 2;
        private const int Barrage = 3;

        private static int Id(int grade, int variation) => 10600000 + grade * 10000 + variation;

        [MenuItem(Root + "G9/Ymir", priority = 0)]
        private static void G9Ymir() => Apply(Id(9, Ymir));

        [MenuItem(Root + "G9/Beast", priority = 1)]
        private static void G9Beast() => Apply(Id(9, Beast));

        [MenuItem(Root + "G9/Aegis", priority = 2)]
        private static void G9Aegis() => Apply(Id(9, Aegis));

        [MenuItem(Root + "G9/Barrage", priority = 3)]
        private static void G9Barrage() => Apply(Id(9, Barrage));

        [MenuItem(Root + "G8 (비교용)/Ymir", priority = 20)]
        private static void G8Ymir() => Apply(Id(8, Ymir));

        [MenuItem(Root + "G8 (비교용)/Beast", priority = 21)]
        private static void G8Beast() => Apply(Id(8, Beast));

        [MenuItem(Root + "G8 (비교용)/Aegis", priority = 22)]
        private static void G8Aegis() => Apply(Id(8, Aegis));

        [MenuItem(Root + "G8 (비교용)/Barrage", priority = 23)]
        private static void G8Barrage() => Apply(Id(8, Barrage));

        [MenuItem(Root + "끄기", priority = 100)]
        private static void Clear()
        {
            if (!TryGetSpineController(out var controller))
            {
                return;
            }

            controller.UpdateAura(null);
            Debug.Log("[아우라 미리보기] 껐습니다.");
        }

        [MenuItem(Root + "G9/Ymir", true)]
        [MenuItem(Root + "G9/Beast", true)]
        [MenuItem(Root + "G9/Aegis", true)]
        [MenuItem(Root + "G9/Barrage", true)]
        [MenuItem(Root + "G8 (비교용)/Ymir", true)]
        [MenuItem(Root + "G8 (비교용)/Beast", true)]
        [MenuItem(Root + "G8 (비교용)/Aegis", true)]
        [MenuItem(Root + "G8 (비교용)/Barrage", true)]
        [MenuItem(Root + "끄기", true)]
        private static bool ValidatePlaying() => Application.isPlaying;

        private static void Apply(int id)
        {
            if (!TryGetSpineController(out var controller))
            {
                return;
            }

            // level 0 = 첫 단계 연출. 강화 단계별 연출은 이 도구의 관심사가 아니다.
            var prefab = ResourcesHelper.GetAuraPrefab(id, 0);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[아우라 미리보기] VFX_Aura.asset 에 id {id} 엔트리가 없습니다." +
                    " 등록하지 않으면 인게임에서도 아우라가 조용히 안 보입니다.");
                return;
            }

            controller.UpdateAura(prefab);
            Debug.Log($"[아우라 미리보기] id {id} → {prefab.name}");
        }

        private static bool TryGetSpineController(out AvatarSpineController controller)
        {
            controller = null;

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[아우라 미리보기] 플레이 모드에서만 동작합니다.");
                return false;
            }

            var character = Nekoyume.Game.Game.instance?.Lobby?.Character;
            if (character == null)
            {
                Debug.LogWarning("[아우라 미리보기] 로비 캐릭터가 없습니다. 로비에 들어간 뒤 실행하세요.");
                return false;
            }

            // LobbyCharacter.appearance 는 private 이라 컴포넌트로 찾는다.
            var appearance = character.GetComponentInChildren<CharacterAppearance>(true);
            if (appearance == null || appearance.SpineController == null)
            {
                Debug.LogWarning("[아우라 미리보기] 캐릭터 외형이 아직 준비되지 않았습니다.");
                return false;
            }

            controller = appearance.SpineController;
            return true;
        }
    }
}
