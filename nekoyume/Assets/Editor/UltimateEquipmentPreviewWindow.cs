using System.Reflection;
using Nekoyume.Game.Avatar;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    /// <summary>
    /// G9(울티밋) 장비 리소스를 아이템 없이 확인하는 창.
    /// </summary>
    /// <remarks>
    /// 두 가지를 본다.
    /// <list type="number">
    /// <item>
    /// 리소스 점검(플레이 불필요): 아이템 아이콘·프로필 초상화·무기 스파인 텍스처·재료 아이콘을
    /// G8 과 나란히 그린다. 파일이 없으면 인게임에서는 예외 없이 기본 아이콘으로 바뀌므로 여기서 빨갛게 표시한다.
    /// 무기 텍스처에는 임포터 피벗(= 캐릭터가 쥐는 점)을 십자로 그려 G8 과 손잡이 위치를 비교한다.
    /// </item>
    /// <item>
    /// 로비 장착(플레이 모드): 로비 캐릭터에 G9/G8 무기·갑옷을 바로 입힌다. 실제 장착 경로
    /// (<c>AvatarSpineController.UpdateBody</c>/<c>UpdateWeapon</c>)를 그대로 태우므로 갑옷 스킨 매핑과
    /// 무기 쥔 위치를 인게임과 같은 모습으로 확인할 수 있다. 장비를 다시 바꾸면 원래 외형으로 돌아간다.
    /// 풀 코스튬을 입고 있으면 실행하지 않는다 — 해제 후 귀·머리·꼬리 파츠와 HUD·충돌 영역 갱신은
    /// <c>CharacterAppearance</c> 의 private 경로라 이 도구가 재현할 수 없다.
    /// </item>
    /// </list>
    /// </remarks>
    public class UltimateEquipmentPreviewWindow : EditorWindow
    {
        private const string Title = "G9 울티밋 미리보기";
        private const string ItemIconDir = "Assets/Resources/UI/Icons/Item/";
        private const string ProfileIconDir = "Assets/Resources/UI/Icons/Profile/";
        private const string WeaponTextureDir = "Assets/Resources/Character/PlayerSpineTexture/Weapon/";

        private static readonly string[] Elements = { "Normal", "Fire", "Water", "Land", "Wind" };
        private static readonly string[] Parts = { "무기", "갑옷", "벨트", "목걸이", "반지" };

        // 강화 재료(단단한 검·옷·벨트·목걸이·반지)는 3{등급}3{부위}02, G9 신규 토큰은 800130.
        private static readonly int[] G9Materials = { 393002, 393102, 393202, 393302, 393402, 800130 };
        private static readonly int[] G8Materials = { 383002, 383102, 383202, 383302, 383402, 800129 };

        private const float IconSize = 58f;
        private const float WeaponWidth = 277f;
        private const float WeaponHeight = 121f;

        private bool _showG8 = true;
        private Vector2 _scroll;

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        // 장비 id 는 10{부위}{등급}{속성}000 — 부위 1~5 = 무기·갑옷·벨트·목걸이·반지.
        private static int EquipmentId(int part, int grade, int element) =>
            10000000 + part * 100000 + grade * 10000 + element * 1000;

        [MenuItem("Tools/Item/" + Title)]
        private static void Open() => GetWindow<UltimateEquipmentPreviewWindow>(Title);

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _showG8 = EditorGUILayout.ToggleLeft("G8 과 나란히 보기", _showG8);
            EditorGUILayout.Space();

            DrawItemIcons();
            DrawMaterials();
            DrawProfiles();
            DrawWeaponTextures();
            DrawLobbyEquip();

            EditorGUILayout.EndScrollView();
        }

        private void DrawItemIcons()
        {
            EditorGUILayout.LabelField("아이템 아이콘 (UI/Icons/Item)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var header = new GUIStyle(EditorStyles.label) { margin = new RectOffset() };
                GUILayout.Label(GUIContent.none, GUILayout.Width(56f));
                foreach (var part in Parts)
                {
                    var label = _showG8 ? $"{part} (G8 | G9)" : part;
                    GUILayout.Label(label, header, GUILayout.Width(_showG8 ? IconSize * 2 + 8f : IconSize + 4f));
                }
            }

            for (var e = 0; e < Elements.Length; e++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Elements[e], GUILayout.Width(56f));
                    for (var part = 1; part <= Parts.Length; part++)
                    {
                        if (_showG8)
                        {
                            DrawTexture(ItemIconDir, EquipmentId(part, 8, e), IconSize, IconSize);
                        }

                        DrawTexture(ItemIconDir, EquipmentId(part, 9, e), IconSize, IconSize);
                        GUILayout.Space(4f);
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawMaterials()
        {
            EditorGUILayout.LabelField("재료 아이콘 (강화 재료 5종 + 800130)", EditorStyles.boldLabel);
            if (_showG8)
            {
                DrawRow("G8", ItemIconDir, G8Materials);
            }

            DrawRow("G9", ItemIconDir, G9Materials);
            EditorGUILayout.Space();
        }

        private void DrawProfiles()
        {
            EditorGUILayout.LabelField("갑옷 프로필 초상화 (UI/Icons/Profile)", EditorStyles.boldLabel);
            for (var grade = _showG8 ? 8 : 9; grade <= 9; grade++)
            {
                var ids = new int[Elements.Length];
                for (var e = 0; e < Elements.Length; e++)
                {
                    ids[e] = EquipmentId(2, grade, e);
                }

                DrawRow($"G{grade}", ProfileIconDir, ids);
            }

            EditorGUILayout.Space();
        }

        private void DrawWeaponTextures()
        {
            EditorGUILayout.LabelField("무기 스파인 텍스처 (빨간 십자 = 쥐는 점/피벗)", EditorStyles.boldLabel);
            for (var e = 0; e < Elements.Length; e++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Elements[e], GUILayout.Width(56f));
                    if (_showG8)
                    {
                        DrawWeapon(EquipmentId(1, 8, e));
                    }

                    DrawWeapon(EquipmentId(1, 9, e));
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawLobbyEquip()
        {
            EditorGUILayout.LabelField("로비 캐릭터에 장착 (플레이 모드 · 로비)", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드로 로비에 들어가면 버튼이 활성화됩니다.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                for (var e = 0; e < Elements.Length; e++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(Elements[e], GUILayout.Width(56f));
                        if (GUILayout.Button("G9 무기+갑옷", GUILayout.Width(110f)))
                        {
                            Equip(EquipmentId(1, 9, e), EquipmentId(2, 9, e));
                        }

                        if (GUILayout.Button("G8 무기+갑옷 (비교)", GUILayout.Width(140f)))
                        {
                            Equip(EquipmentId(1, 8, e), EquipmentId(2, 8, e));
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "풀 코스튬을 입고 있으면 동작하지 않습니다(먼저 벗어 주세요). 인벤토리에서 장비를 다시 바꾸면 원래 외형으로 돌아갑니다.",
                MessageType.None);
        }

        private static void DrawRow(string label, string dir, int[] ids)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(56f));
                foreach (var id in ids)
                {
                    DrawTexture(dir, id, IconSize, IconSize);
                    GUILayout.Space(4f);
                }
            }
        }

        private static Rect DrawTexture(string dir, int id, float width, float height)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}{id}.png");
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
                if (texture != null)
                {
                    GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(rect, new Color(0.55f, 0.1f, 0.1f));
                    GUI.Label(rect, "없음", EditorStyles.centeredGreyMiniLabel);
                }

                GUILayout.Label(id.ToString(), EditorStyles.miniLabel, GUILayout.Width(width));
                return rect;
            }
        }

        private static void DrawWeapon(int id)
        {
            var rect = DrawTexture(WeaponTextureDir, id, WeaponWidth, WeaponHeight);
            if (AssetImporter.GetAtPath($"{WeaponTextureDir}{id}.png") is not TextureImporter importer)
            {
                return;
            }

            // 텍스처가 277x121 이라 ScaleToFit 여백이 없다. 피벗은 좌하단 기준 0~1.
            var pivot = importer.spritePivot;
            var x = rect.x + rect.width * pivot.x;
            var y = rect.y + rect.height * (1f - pivot.y);
            EditorGUI.DrawRect(new Rect(x - 8f, y - 1f, 16f, 2f), Color.red);
            EditorGUI.DrawRect(new Rect(x - 1f, y - 8f, 2f, 16f), Color.red);
        }

        private static void Equip(int weaponId, int armorId)
        {
            var character = Nekoyume.Game.Game.instance?.Lobby?.Character;
            if (character == null)
            {
                Debug.LogWarning($"[{Title}] 로비 캐릭터가 없습니다. 로비에 들어간 뒤 실행하세요.");
                return;
            }

            // LobbyCharacter.appearance 는 private 이라 컴포넌트로 찾는다.
            var appearance = character.GetComponentInChildren<CharacterAppearance>(true);
            var controller = appearance != null ? appearance.SpineController : null;
            if (controller == null)
            {
                Debug.LogWarning($"[{Title}] 캐릭터 외형이 아직 준비되지 않았습니다.");
                return;
            }

            if (IsFullCostumeActive(controller))
            {
                Debug.LogWarning($"[{Title}] 풀 코스튬을 입고 있습니다. 벗은 뒤 다시 실행하세요.");
                return;
            }

            // 실제 장착 순서(CharacterAppearance.UpdateAvatar)와 같게: 갑옷 → 무기 → 보조 아우라.
            // 갑옷이 바디 스킨을 갈아 끼우므로 무기는 반드시 그 뒤에 붙여야 한다.
            controller.UpdateBody(armorId, 0, false);

            var weaponVfx = ResourcesHelper.GetAuraWeaponPrefab(weaponId, 0);
            controller.UpdateWeapon(weaponId, weaponVfx);

            // 보조 아우라는 장착 장비의 최고 등급으로 고른다 — 미리보기 무기·갑옷 등급을 쓴다.
            var grade = weaponId / 10000 % 10;
            controller.UpdateSecondaryAura(ResourcesHelper.GetSecondaryAuraPrefab(grade));

            Debug.Log(
                $"[{Title}] 무기 {weaponId}, 갑옷 {armorId} 적용" +
                (weaponVfx == null ? $" (VFX_WeaponAura 에 {weaponId} 엔트리 없음 → 무기 VFX 없음)" : string.Empty));
        }

        private static bool IsFullCostumeActive(AvatarSpineController controller)
        {
            // 공개 상태가 없어 private 필드를 읽는다. 이름이 바뀌어 못 찾으면 막지 않고 진행한다.
            var field = typeof(AvatarSpineController).GetField(
                "_isActiveFullCostume", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && (bool)field.GetValue(controller);
        }
    }
}
