using System.Collections.Generic;
using System.Reflection;
using Nekoyume.Game.Avatar;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    /// <summary>
    /// 울티밋(G9~G19) 장비 리소스를 아이템 없이 확인하는 창.
    /// </summary>
    /// <remarks>
    /// 두 가지를 본다.
    /// <list type="number">
    /// <item>
    /// 리소스 점검(플레이 불필요): 아이템 아이콘·프로필 초상화·무기 스파인 텍스처·재료 아이콘을
    /// 비교 등급과 나란히 그린다. 파일이 없으면 인게임에서는 예외 없이 기본 아이콘으로 바뀌므로 여기서 빨갛게 표시한다.
    /// 무기 텍스처에는 임포터 피벗(= 캐릭터가 쥐는 점)을 십자로 그려 비교 등급과 손잡이 위치를 비교한다.
    /// </item>
    /// <item>
    /// 로비 장착(플레이 모드): 로비 캐릭터에 보는 등급/비교 등급 무기·갑옷을 바로 입힌다. 실제 장착 경로
    /// (<c>AvatarSpineController.UpdateBody</c>/<c>UpdateWeapon</c>)를 그대로 태우므로 갑옷 스킨 매핑과
    /// 무기 쥔 위치를 인게임과 같은 모습으로 확인할 수 있다. 장비를 다시 바꾸면 원래 외형으로 돌아간다.
    /// 풀 코스튬을 입고 있으면 실행하지 않는다 — 해제 후 귀·머리·꼬리 파츠와 HUD·충돌 영역 갱신은
    /// <c>CharacterAppearance</c> 의 private 경로라 이 도구가 재현할 수 없다.
    /// </item>
    /// </list>
    /// </remarks>
    public class UltimateEquipmentPreviewWindow : EditorWindow
    {
        private const string Title = "울티밋 등급 미리보기";
        private const string ItemIconDir = "Assets/Resources/UI/Icons/Item/";
        private const string ProfileIconDir = "Assets/Resources/UI/Icons/Profile/";
        private const string WeaponTextureDir = "Assets/Resources/Character/PlayerSpineTexture/Weapon/";

        private static readonly string[] Elements = { "Normal", "Fire", "Water", "Land", "Wind" };
        private static readonly string[] Parts = { "무기", "갑옷", "벨트", "목걸이", "반지" };

        private const int MinGrade = 8;
        private const int MaxGrade = 19;

        private const float IconSize = 58f;
        private const float WeaponWidth = 277f;
        private const float WeaponHeight = 121f;

        private int _grade = 10;
        private int _compareGrade = 9;
        private bool _showCompare = true;
        private Vector2 _scroll;

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        // 장비 id 는 G9 이하 10{부위}{등급}{속성}000, G10 부터 13{부위}{등급-10}{속성}000
        // (재호님 ultimate_grade_resource_ids_G9-G19). 부위 1~5 = 무기·갑옷·벨트·목걸이·반지.
        private static int EquipmentId(int part, int grade, int element) =>
            grade <= 9
                ? 10000000 + part * 100000 + grade * 10000 + element * 1000
                : 13000000 + part * 100000 + (grade - 10) * 10000 + element * 1000;

        // 강화 재료(단단한 검·옷·벨트·목걸이·반지)는 G9 이하 3{등급}3{부위-1}02, G10 부터 4{등급-10}3{부위-1}02.
        // 등급 토큰은 G8 800129, G9 800130 뿐이다.
        private static int[] Materials(int grade)
        {
            var ids = new System.Collections.Generic.List<int>();
            for (var part = 0; part < Parts.Length; part++)
            {
                ids.Add(grade <= 9
                    ? 300002 + grade * 10000 + 3000 + part * 100
                    : 400002 + (grade - 10) * 10000 + 3000 + part * 100);
            }

            if (grade == 8 || grade == 9)
            {
                ids.Add(800121 + grade);
            }

            return ids.ToArray();
        }

        private static string[] GradeOptions()
        {
            var options = new string[MaxGrade - MinGrade + 1];
            for (var g = MinGrade; g <= MaxGrade; g++)
            {
                options[g - MinGrade] = $"G{g}";
            }

            return options;
        }

        [MenuItem("Tools/Item/" + Title)]
        private static void Open() => GetWindow<UltimateEquipmentPreviewWindow>(Title);

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var options = GradeOptions();
            _grade = EditorGUILayout.Popup("보는 등급", _grade - MinGrade, options) + MinGrade;
            using (new EditorGUILayout.HorizontalScope())
            {
                _showCompare = EditorGUILayout.ToggleLeft("비교 등급과 나란히 보기", _showCompare, GUILayout.Width(180f));
                using (new EditorGUI.DisabledScope(!_showCompare))
                {
                    _compareGrade = EditorGUILayout.Popup(_compareGrade - MinGrade, options) + MinGrade;
                }
            }

            EditorGUILayout.Space();

            DrawItemIcons();
            DrawSlots();
            DrawMaterials();
            DrawProfiles();
            DrawWeaponTextures();
            DrawLobbyEquip();
            DrawAura();

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
                    var label = _showCompare ? $"{part} (G{_compareGrade} | G{_grade})" : part;
                    GUILayout.Label(label, header, GUILayout.Width(_showCompare ? IconSize * 2 + 8f : IconSize + 4f));
                }
            }

            for (var e = 0; e < Elements.Length; e++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Elements[e], GUILayout.Width(56f));
                    for (var part = 1; part <= Parts.Length; part++)
                    {
                        if (_showCompare)
                        {
                            DrawTexture(ItemIconDir, EquipmentId(part, _compareGrade, e), IconSize, IconSize);
                        }

                        DrawTexture(ItemIconDir, EquipmentId(part, _grade, e), IconSize, IconSize);
                        GUILayout.Space(4f);
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private const string TextureDir = "Assets/Resources/UI/Textures/";
        private const string ItemViewDataPath = "Assets/Resources/ScriptableObject/UI_ItemViewData.asset";
        private int _slotElement = 1;
        private bool _useCustomFrame;
        // 아트 시안 테두리 파일을 시험할 때 경로를 바꾼다. 기본값은 지금 G10 이상에 쓰는 테두리다.
        private string _customFramePath = "Assets/Resources/UI/Textures/item_frame_ultimate.png";
        private static string _loadedFramePath;
        private static Texture2D _customFrame;

        // 인벤토리 칸과 같은 겹: 등급 배경(62) → 아이콘(58) → 장식 테두리(62). VanillaItemView 프리팹 크기 기준.
        // UI_ItemViewData 에 등급 항목이 있으면 그 테두리를 쓰고, 없으면 금테만 남긴 시험 테두리를 쓴다.
        // 배경의 HSV 보정(UIHsvModifier)은 그리지 않으므로, G9 이상은 배색 규칙
        // "9등급부터 1~8등급 색을 다시 쓴다" 대로 같은 색으로 미리 칠해 둔 item_bg_{(등급-1)%8+1} 로 대신 그린다.
        private void DrawSlots()
        {
            EditorGUILayout.LabelField("인벤토리 칸 (등급 배경 + 아이콘 + 테두리)", EditorStyles.boldLabel);
            _slotElement = EditorGUILayout.Popup("속성", _slotElement, Elements);
            _useCustomFrame = EditorGUILayout.ToggleLeft("G9 이상 테두리를 파일로 시험 (배경은 item_bg_{(등급-1)%8+1})", _useCustomFrame);
            using (new EditorGUI.DisabledScope(!_useCustomFrame))
            {
                _customFramePath = EditorGUILayout.TextField("테두리 파일", _customFramePath);
            }

            var data = AssetDatabase.LoadAssetAtPath<Nekoyume.ItemViewDataScriptableObject>(ItemViewDataPath);
            foreach (var grade in _showCompare ? new[] { _compareGrade, _grade } : new[] { _grade })
            {
                var (background, frame, isRule) = SlotSprites(data, grade);
                // G9 이상은 테두리를 잘라 낸 배경(item_bg_*_Inner)에 색 보정(HSV)을 걸어 그린다.
                // 이 창은 색 보정을 그리지 않으므로, 같은 색으로 미리 칠해 둔 item_bg_{(등급-1)%8+1} 의
                // 테두리를 잘라 대신 그린다. 그 위에 등급 테두리가 배경 가장자리를 덮는다.
                var trimRim = grade >= 9;
                if (_useCustomFrame && grade >= 9)
                {
                    frame = CustomFrame(_customFramePath);
                    isRule = true;
                    trimRim = true;
                }

                if (trimRim)
                {
                    background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}item_bg_{(grade - 1) % 8 + 1}.png");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(isRule ? $"G{grade}\n(규칙)" : $"G{grade}", GUILayout.Width(56f));
                    for (var part = 1; part <= Parts.Length; part++)
                    {
                        var rect = GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f));
                        var inner = new Rect(rect.x + 1f, rect.y + 1f, 62f, 62f);
                        if (trimRim && background != null)
                        {
                            // 배경의 바깥 테두리(검정 1 + 색 3픽셀, 99 기준)를 잘라 내고 사방 3/64 안쪽에 깐다 —
                            // 게임의 item_bg_*_Inner(64 기준 바깥 3픽셀을 투명으로 비움)와 같은 배치다.
                            // 금테 모서리가 파여 있어 칸 전체에 깔면 모서리로 배경 색이 비친다.
                            const float rim = 4f / 99f;
                            var inset = inner.width * 3f / 64f;
                            GUI.DrawTextureWithTexCoords(
                                new Rect(inner.x + inset, inner.y + inset, inner.width - 2f * inset, inner.height - 2f * inset),
                                background,
                                new Rect(rim, rim, 1f - 2f * rim, 1f - 2f * rim));
                        }
                        else
                        {
                            DrawSprite(inner, background);
                        }
                        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ItemIconDir}{EquipmentId(part, grade, _slotElement)}.png");
                        if (icon != null)
                        {
                            GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, 58f, 58f), icon, ScaleMode.ScaleToFit);
                        }

                        DrawSprite(inner, frame);
                        GUILayout.Space(6f);
                    }
                }

                GUILayout.Space(4f);
            }

            EditorGUILayout.HelpBox(
                "(규칙) = UI_ItemViewData 에 등급 항목이 없거나 테두리 파일로 시험 중이라 배색 규칙대로 그린 모습입니다. " +
                "G9 이상 배경은 색 보정 대신 같은 색 item_bg 파일로 그립니다.",
                MessageType.None);
            EditorGUILayout.Space();
        }

        private static (Texture background, Texture frame, bool isRule) SlotSprites(
            Nekoyume.ItemViewDataScriptableObject data, int grade)
        {
            var exact = data == null ? null : data.GetItemViewData(grade);
            if (exact != null && exact.Grade == grade)
            {
                return (SpriteTexture(exact.GradeBackground), SpriteTexture(exact.GradeFrameOverlay), false);
            }

            var background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}item_bg_{(grade - 1) % 8 + 1}.png");
            return (background, GoldBorderOnly(), true);
        }

        // 프로젝트 밖 파일(아트 시안)을 에셋으로 넣지 않고 읽는다. 경로가 바뀔 때만 다시 읽는다.
        private static Texture2D CustomFrame(string path)
        {
            if (_customFrame != null && _loadedFramePath == path)
            {
                return _customFrame;
            }

            _loadedFramePath = path;
            if (_customFrame != null)
            {
                DestroyImmediate(_customFrame);
            }

            _customFrame = null;
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(System.IO.File.ReadAllBytes(path)))
            {
                _customFrame = texture;
            }

            return _customFrame;
        }

        private static Texture2D _goldBorderOnly;

        // item_frame_9 는 금테만이 아니라 금테 안쪽에 G1 색(갈색) 띠까지 들고 있다 — G9 배경과 같은 색이라
        // G9 에선 안 보이지만, 다른 색 배경에 얹으면 갈색 띠가 겹쳐 배경이 두 색으로 보인다.
        // 시험 표시에서는 item_bg_1 과 같은 픽셀(차이 12 이하)을 지워 금테만 남긴다.
        private static Texture2D GoldBorderOnly()
        {
            if (_goldBorderOnly != null)
            {
                return _goldBorderOnly;
            }

            var frame = new Texture2D(2, 2);
            var g1 = new Texture2D(2, 2);
            if (!frame.LoadImage(System.IO.File.ReadAllBytes($"{TextureDir}item_frame_9.png")) ||
                !g1.LoadImage(System.IO.File.ReadAllBytes($"{TextureDir}item_bg_1.png")) ||
                frame.width != g1.width || frame.height != g1.height)
            {
                return frame;
            }

            var f = frame.GetPixels32();
            var b = g1.GetPixels32();
            for (var i = 0; i < f.Length; i++)
            {
                if (f[i].a > 0 &&
                    Mathf.Abs(f[i].r - b[i].r) <= 12 &&
                    Mathf.Abs(f[i].g - b[i].g) <= 12 &&
                    Mathf.Abs(f[i].b - b[i].b) <= 12)
                {
                    f[i] = new Color32(0, 0, 0, 0);
                }
            }

            frame.SetPixels32(f);
            frame.Apply();
            DestroyImmediate(g1);
            _goldBorderOnly = frame;
            return _goldBorderOnly;
        }

        // 스프라이트가 아틀라스로 묶이면 sprite.texture 가 아틀라스 전체가 된다 — 원본 파일을 읽는다.
        private static Texture SpriteTexture(Sprite sprite) =>
            sprite == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sprite));

        private static void DrawSprite(Rect rect, Texture texture)
        {
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            }
        }

        private void DrawMaterials()
        {
            EditorGUILayout.LabelField("재료 아이콘 (강화 재료 5종 + 등급 토큰)", EditorStyles.boldLabel);
            if (_showCompare)
            {
                DrawRow($"G{_compareGrade}", ItemIconDir, Materials(_compareGrade));
            }

            DrawRow($"G{_grade}", ItemIconDir, Materials(_grade));
            EditorGUILayout.Space();
        }

        private void DrawProfiles()
        {
            EditorGUILayout.LabelField("갑옷 프로필 초상화 (UI/Icons/Profile)", EditorStyles.boldLabel);
            foreach (var grade in _showCompare ? new[] { _compareGrade, _grade } : new[] { _grade })
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
                    if (_showCompare)
                    {
                        DrawWeapon(EquipmentId(1, _compareGrade, e));
                    }

                    DrawWeapon(EquipmentId(1, _grade, e));
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
                        if (GUILayout.Button($"G{_grade} 무기+갑옷", GUILayout.Width(120f)))
                        {
                            Equip(EquipmentId(1, _grade, e), EquipmentId(2, _grade, e), _grade);
                        }

                        if (GUILayout.Button($"G{_compareGrade} 무기+갑옷 (비교)", GUILayout.Width(150f)))
                        {
                            Equip(EquipmentId(1, _compareGrade, e), EquipmentId(2, _compareGrade, e), _compareGrade);
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

        private static void Equip(int weaponId, int armorId, int grade)
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
            controller.UpdateSecondaryAura(ResourcesHelper.GetSecondaryAuraPrefab(grade));

            Debug.Log(
                $"[{Title}] 무기 {weaponId}, 갑옷 {armorId} 적용" +
                (weaponVfx == null ? $" (VFX_WeaponAura 에 {weaponId} 엔트리 없음 → 무기 VFX 없음)" : string.Empty));
        }

        // 아우라 id 는 G9 이하 106{등급}000{변형}, G10 부터 136{등급-10}000{변형}. 변형 0~3 = Ymir·Beast·Aegis·Barrage.
        private static readonly string[] AuraVariations = { "Ymir", "Beast", "Aegis", "Barrage" };

        private static int AuraId(int grade, int variation) =>
            grade <= 9
                ? 10600000 + grade * 10000 + variation
                : 13600000 + (grade - 10) * 10000 + variation;

        // 261029 얼티밋 등급 납품본의 등급 색(G10 그린 ~ G19 오팔). 시험 색 — 확정 아님.
        private static readonly Color[] UltimateGradeColors =
        {
            new Color32(0x33, 0xF6, 0x24, 0xFF), // G10 그린
            new Color32(0x0D, 0x67, 0xF7, 0xFF), // G11 블루
            new Color32(0xFB, 0xA9, 0x27, 0xFF), // G12 골드
            new Color32(0xCE, 0x19, 0xF8, 0xFF), // G13 퍼플
            new Color32(0xF0, 0x44, 0x4F, 0xFF), // G14 레드
            new Color32(0x23, 0xA6, 0xA6, 0xFF), // G15 터쿼이즈
            new Color32(0xC8, 0x50, 0x1A, 0xFF), // G16 오렌지
            new Color32(0x3A, 0x3A, 0x46, 0xFF), // G17 블랙
            new Color32(0xF5, 0xE0, 0x4A, 0xFF), // G18 레몬
            new Color32(0xD8, 0xC8, 0xFF, 0xFF), // G19 오팔 (파티클마다 색상을 돌려 무지개로)
        };

        private const int OpalGrade = 19;

        private static GameObject _tintTarget;
        private static Color _tintColor;
        private static bool _tintOpal;
        private static readonly HashSet<int> TintedObjects = new();
        private static double _tintUntil;

        private void DrawAura()
        {
            EditorGUILayout.LabelField("아우라 이펙트 (플레이 모드 · 로비)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "G9 이하와, VFX_Aura.asset 에 등록된 등급은 실제 프리팹을 씁니다. " +
                "G10 이상은 아직 프리팹이 없어 G9(흰색) 아우라를 띄운 뒤 등급 색으로 바꿔 칠해 보여 줍니다 — " +
                "파티클 밝기는 그대로 두고 색만 바꿉니다(G9 를 만든 방식의 반대 방향). " +
                "가산 합성이라 블랙(G17)은 어두운 회색으로 보입니다.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                foreach (var grade in _showCompare ? new[] { _grade, _compareGrade } : new[] { _grade })
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"G{grade}", GUILayout.Width(56f));
                        for (var v = 0; v < AuraVariations.Length; v++)
                        {
                            if (GUILayout.Button(AuraVariations[v], GUILayout.Width(80f)))
                            {
                                ApplyAura(grade, v);
                            }
                        }
                    }
                }

                if (GUILayout.Button("아우라 끄기", GUILayout.Width(100f)) &&
                    TryGetController(out var controller))
                {
                    StopTint();
                    controller.UpdateAura(null);
                }
            }
        }

        private static void ApplyAura(int grade, int variation)
        {
            if (!TryGetController(out var controller))
            {
                return;
            }

            var id = AuraId(grade, variation);
            var prefab = ResourcesHelper.GetAuraPrefab(id, 0);
            var tint = false;
            if (prefab == null && grade >= 10)
            {
                prefab = ResourcesHelper.GetAuraPrefab(AuraId(9, variation), 0);
                tint = true;
            }

            if (prefab == null)
            {
                Debug.LogError($"[{Title}] VFX_Aura.asset 에 아우라 id {id} 엔트리가 없습니다.");
                return;
            }

            // 같은 프리팹이면 UpdateAura 가 다시 만들지 않는다 — 색을 겹쳐 칠하지 않도록 끄고 새로 띄운다.
            StopTint();
            controller.UpdateAura(null);
            controller.UpdateAura(prefab);

            if (tint)
            {
                var field = typeof(AvatarSpineController).GetField(
                    "_cachedAuraVFX", BindingFlags.Instance | BindingFlags.NonPublic);
                _tintTarget = field?.GetValue(controller) as GameObject;
                _tintColor = UltimateGradeColors[grade - 10];
                _tintOpal = grade == OpalGrade;
                TintedObjects.Clear();

                // aura_01/02 는 래퍼가 내부 프리팹을 늦게 만든다 — 몇 초 동안 새로 생긴 파티클도 칠한다.
                _tintUntil = EditorApplication.timeSinceStartup + 3d;
                EditorApplication.update -= TintTick;
                EditorApplication.update += TintTick;
            }

            Debug.Log($"[{Title}] 아우라 G{grade} {AuraVariations[variation]} → {prefab.name}" +
                (tint ? " (G9 아우라를 등급 색으로 칠한 시험 표시)" : string.Empty));
        }

        private static void StopTint()
        {
            EditorApplication.update -= TintTick;
            _tintTarget = null;
            TintedObjects.Clear();
        }

        private static void TintTick()
        {
            if (_tintTarget == null || EditorApplication.timeSinceStartup > _tintUntil)
            {
                EditorApplication.update -= TintTick;
                return;
            }

            var systems = _tintTarget.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                if (!TintedObjects.Add(systems[i].GetInstanceID()))
                {
                    continue;
                }

                var color = _tintOpal ? OpalColor(i) : _tintColor;
                TintParticleSystem(systems[i], color);
            }

            // Ymir 은 파티클이 아니라 스파인이라 색이 텍스처에 구워져 있다 — 스켈레톤 색을 곱한다.
            foreach (var spine in _tintTarget.GetComponentsInChildren<SkeletonAnimation>(true))
            {
                if (TintedObjects.Add(spine.GetInstanceID()) && spine.Skeleton != null)
                {
                    var c = Color.Lerp(Color.white, _tintColor, 0.7f);
                    spine.Skeleton.SetColor(new Color(c.r, c.g, c.b, spine.Skeleton.A));
                }
            }
        }

        private static Color OpalColor(int index)
        {
            Color.RGBToHSV(new Color32(0xD8, 0xC8, 0xFF, 0xFF), out var h, out _, out _);
            return Color.HSVToRGB((h + index * 0.17f) % 1f, 0.55f, 1f);
        }

        private static void TintParticleSystem(ParticleSystem ps, Color target)
        {
            var main = ps.main;
            main.startColor = Tint(main.startColor, target);

            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                col.color = Tint(col.color, target);
            }

            var cbs = ps.colorBySpeed;
            if (cbs.enabled)
            {
                cbs.color = Tint(cbs.color, target);
            }

            var trails = ps.trails;
            if (trails.enabled)
            {
                trails.colorOverLifetime = Tint(trails.colorOverLifetime, target);
                trails.colorOverTrail = Tint(trails.colorOverTrail, target);
            }
        }

        private static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient g, Color target)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Tint(g.color, target));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Tint(g.colorMin, target), Tint(g.colorMax, target));
                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(Tint(g.gradient, target)) { mode = g.mode };
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Tint(g.gradientMin, target), Tint(g.gradientMax, target));
                default:
                    return g;
            }
        }

        private static Gradient Tint(Gradient gradient, Color target)
        {
            if (gradient == null)
            {
                return null;
            }

            var keys = gradient.colorKeys;
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i].color = Tint(keys[i].color, target);
            }

            var result = new Gradient { mode = gradient.mode };
            result.SetKeys(keys, gradient.alphaKeys);
            return result;
        }

        // 원래 색의 밝기(휘도)를 지키면서 색상·채도만 목표 색으로 바꾼다. 알파는 그대로.
        // 목표 색을 그 밝기에 맞추다 한 채널이 1 을 넘으면, 넘친 만큼 흰색 쪽으로 섞어 밝기를 맞춘다.
        private static Color Tint(Color source, Color target)
        {
            var lum = Luminance(source);
            var targetLum = Mathf.Max(Luminance(target), 0.0001f);
            var c = target * (lum / targetLum);
            var max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (max > 1f)
            {
                c /= max;
                var t = Mathf.InverseLerp(Luminance(c), 1f, lum);
                c = Color.Lerp(c, Color.white, t);
            }

            return new Color(c.r, c.g, c.b, source.a);
        }

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        private static bool TryGetController(out AvatarSpineController controller)
        {
            controller = null;
            var character = Nekoyume.Game.Game.instance?.Lobby?.Character;
            var appearance = character != null ? character.GetComponentInChildren<CharacterAppearance>(true) : null;
            controller = appearance != null ? appearance.SpineController : null;
            if (controller == null)
            {
                Debug.LogWarning($"[{Title}] 로비 캐릭터가 없습니다. 플레이 모드로 로비에 들어간 뒤 실행하세요.");
                return false;
            }

            return true;
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
