using System;
using System.IO;
using System.Linq;
using Nekoyume;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NekoyumeEditor
{
    /// <summary>
    /// 칭호(Title 코스튬) 리소스를 아이템 없이 확인하는 창.
    /// </summary>
    /// <remarks>
    /// 두 가지를 본다.
    /// <list type="number">
    /// <item>
    /// 리소스 점검(플레이 불필요): 아이템 아이콘, 머리 위 칭호의 텍스트 스타일·글로우 이미지,
    /// <c>UI_CharacterTitle</c> 의 id 별 prefab 등록 여부를 비교 칭호와 나란히 그린다.
    /// 등록이 없으면 인게임에서는 등급 공용 prefab 으로 조용히 바뀌므로 여기서 표시한다.
    /// prefab 레이아웃은 RectTransform·Image 만 따라 그린 근사치다 — 애니메이션·머티리얼(가산 블렌드)은 빠진다.
    /// </item>
    /// <item>
    /// 로비 표시(플레이 모드): 로비 머리 위에 칭호를 바로 붙인다. 실제 경로
    /// (<c>LobbyMenu.UpdateTitle</c> → <c>ResourcesHelper.GetCharacterTitle</c>)를 그대로 태우므로
    /// 이름 로컬라이즈와 prefab 선택이 인게임과 같다. 칭호를 다시 장착하면 원래대로 돌아간다.
    /// </item>
    /// </list>
    /// </remarks>
    public class TitlePreviewWindow : EditorWindow
    {
        private const string Title = "칭호 미리보기";
        private const string ItemIconDir = "Assets/Resources/UI/Icons/Item/";
        private const string TextStyleDir = "Assets/UIResources/Atlas_image/Common/UI_ChStyle/";
        private const string TitleSoPath = "Assets/Resources/ScriptableObject/UI_CharacterTitle.asset";
        private const string CostumeSheetPath = "Assets/_Scripts/Lib9c/lib9c/Lib9c/TableCSV/CostumeItemSheet.csv";

        private const float IconSize = 58f;
        private const float StyleWidth = 190f;
        private const float StyleHeight = 100f;
        private const float LayoutWidth = 300f;
        private const float LayoutHeight = 180f;

        private int _titleId = 49900064;
        private int _compareId = 49900057;
        private string _previewText;
        private Vector2 _scroll;

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        [MenuItem("Tools/Item/" + Title)]
        private static void Open() => GetWindow<TitlePreviewWindow>(Title);

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _titleId = EditorGUILayout.IntField("칭호 id", _titleId);
            _compareId = EditorGUILayout.IntField("비교 칭호 id (0 = 끔)", _compareId);
            _previewText = EditorGUILayout.TextField(
                new GUIContent("레이아웃 글자", "비우면 시트 이름을 쓴다. 인게임 글자는 로컬라이즈된 이름이다."),
                _previewText);
            EditorGUILayout.Space();

            var ids = _compareId > 0 ? new[] { _titleId, _compareId } : new[] { _titleId };
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var id in ids)
                {
                    DrawTitleColumn(id);
                    GUILayout.Space(16f);
                }
            }

            EditorGUILayout.Space();
            DrawLobbyTitle(ids);

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitleColumn(int id)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LayoutWidth)))
            {
                var row = FindSheetRow(id);
                EditorGUILayout.LabelField(
                    row == null ? $"{id} (시트에 없음)" : $"{id} · {row.Value.name} · G{row.Value.grade}",
                    EditorStyles.boldLabel);

                var prefab = FindOverridePrefab(id);
                if (prefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "UI_CharacterTitle 의 titleByItemId 에 등록 없음 → 인게임에서는 등급 공용 prefab 으로 표시됩니다.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField($"prefab: {prefab.name}", EditorStyles.miniLabel);
                }

                EditorGUILayout.LabelField("아이템 아이콘 · 텍스트 스타일 · 글로우", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawTexture($"{ItemIconDir}{id}.png", IconSize, IconSize);
                    GUILayout.Space(4f);
                    DrawTexture($"{TextStyleDir}Icon_ChTextStyle_{id}.png", StyleWidth, StyleHeight);
                }

                DrawTexture($"{TextStyleDir}Icon_ChTextStyle_{id}_Outglow.png", StyleWidth, StyleHeight);

                if (prefab != null)
                {
                    EditorGUILayout.LabelField("prefab 레이아웃 (근사치)", EditorStyles.miniBoldLabel);
                    var text = string.IsNullOrEmpty(_previewText) ? row?.name ?? id.ToString() : _previewText;
                    DrawPrefabLayout(prefab, text);
                }
            }
        }

        private static void DrawLobbyTitle(int[] ids)
        {
            EditorGUILayout.LabelField("로비 머리 위에 표시 (플레이 모드 · 로비)", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드로 로비에 들어가면 버튼이 활성화됩니다.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var id in ids)
                {
                    if (GUILayout.Button($"{id} 표시", GUILayout.Width(140f)))
                    {
                        ShowInLobby(id);
                    }
                }
            }

            EditorGUILayout.HelpBox("인벤토리에서 칭호를 다시 장착하거나 해제하면 원래대로 돌아갑니다.", MessageType.None);
        }

        private static void ShowInLobby(int id)
        {
            // TableSheets.Instance 는 Game.instance 가 없으면 NRE 라 직접 따라간다.
            var sheet = Nekoyume.Game.Game.instance?.TableSheets?.CostumeItemSheet;
            if (sheet == null || !sheet.TryGetValue(id, out var row))
            {
                Debug.LogWarning($"[{Title}] CostumeItemSheet 에 {id} 가 없습니다. 로그인 후 로비에서 실행하세요.");
                return;
            }

            if (row.ItemSubType != ItemSubType.Title)
            {
                Debug.LogWarning($"[{Title}] {id} 는 칭호가 아닙니다 ({row.ItemSubType}).");
                return;
            }

            // Find 는 없으면 예외를 던지거나(모바일 타깃이면) 위젯을 새로 만든다 — TryFind 로 확인만 한다.
            if (!Widget.TryFind<LobbyMenu>(out var lobbyMenu))
            {
                Debug.LogWarning($"[{Title}] LobbyMenu 를 찾지 못했습니다. 로비에 들어간 뒤 실행하세요.");
                return;
            }

            lobbyMenu.UpdateTitle(ItemFactory.CreateCostume(row, Guid.NewGuid()));
            Debug.Log($"[{Title}] {id} 칭호 표시");
        }

        private static GameObject FindOverridePrefab(int id)
        {
            var so = AssetDatabase.LoadAssetAtPath<CharacterTitleScriptableObject>(TitleSoPath);
            return so?.titleByItemId?.FirstOrDefault(x => x.ItemId == id)?.Title;
        }

        // 플레이 전에도 이름·등급을 보이려고 시트 CSV 를 직접 읽는다(id,_name,item_sub_type,grade,...).
        private static (string name, int grade)? FindSheetRow(int id)
        {
            if (!File.Exists(CostumeSheetPath))
            {
                return null;
            }

            var prefix = $"{id},";
            var line = File.ReadLines(CostumeSheetPath).FirstOrDefault(x => x.StartsWith(prefix));
            var cols = line?.Split(',');
            if (cols == null || cols.Length < 4 || !int.TryParse(cols[3], out var grade))
            {
                return null;
            }

            return (cols[1], grade);
        }

        private static void DrawTexture(string path, float width, float height)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
                if (texture != null)
                {
                    GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                    GUILayout.Label($"{texture.width}x{texture.height}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUI.DrawRect(rect, new Color(0.55f, 0.1f, 0.1f));
                    GUI.Label(rect, "없음", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label(Path.GetFileName(path), EditorStyles.miniLabel);
                }
            }
        }

        private static void DrawPrefabLayout(GameObject prefab, string text)
        {
            var area = GUILayoutUtility.GetRect(LayoutWidth, LayoutHeight,
                GUILayout.Width(LayoutWidth), GUILayout.Height(LayoutHeight));
            EditorGUI.DrawRect(area, new Color(0.25f, 0.27f, 0.3f));

            var root = (RectTransform)prefab.transform;
            var center = area.center;

            // 계층 순서 = UI 그리기 순서. 꺼진 노드와 투명한 이미지는 건너뛴다.
            var previousColor = GUI.color;
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (!image.enabled || image.sprite == null || image.color.a <= 0f ||
                    !IsActive(image.transform, root))
                {
                    continue;
                }

                var rect = ToGuiRect(ResolveRect((RectTransform)image.transform, root), center);
                var sprite = image.sprite;
                var tex = sprite.texture;
                var tr = sprite.textureRect;
                GUI.color = image.color;
                GUI.DrawTextureWithTexCoords(rect, tex,
                    new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
            }

            GUI.color = previousColor;

            var label = prefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }

            var textRect = ToGuiRect(ResolveRect(label.rectTransform, root), center);
            // 폭 0 + 오버플로 설정이라 실제로는 글자가 rect 밖으로 퍼진다 — 넓게 잡아 가운데 정렬로 그린다.
            textRect = new Rect(textRect.center.x - 150f, textRect.y, 300f, textRect.height);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(label.fontSizeMax > 0 ? label.fontSizeMax : label.fontSize),
                normal = { textColor = Color.black },
            };
            GUI.Label(new Rect(textRect.x + 1f, textRect.y + 1f, textRect.width, textRect.height), text, style);
            style.normal.textColor = Color.white;
            GUI.Label(textRect, text, style);
        }

        private static bool IsActive(Transform t, Transform root)
        {
            for (; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    return false;
                }

                if (t == root)
                {
                    break;
                }
            }

            return true;
        }

        // 루트 중심 기준 rect (y 위쪽 +). 앵커·피벗을 부모 rect 에 대해 풀어 쌓는다. 스케일·회전은 무시한다.
        private static Rect ResolveRect(RectTransform rt, RectTransform root)
        {
            if (rt == root || rt.parent is not RectTransform parentRt)
            {
                var size = rt.sizeDelta;
                return new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
            }

            var parent = ResolveRect(parentRt, root);
            var anchorMin = parent.min + Vector2.Scale(parent.size, rt.anchorMin);
            var anchorMax = parent.min + Vector2.Scale(parent.size, rt.anchorMax);
            var rectSize = anchorMax - anchorMin + rt.sizeDelta;
            // anchoredPosition 은 앵커 기준점(앵커 사각형 안의 피벗 위치)에서의 피벗 오프셋이다.
            var pivotPos = anchorMin + Vector2.Scale(anchorMax - anchorMin, rt.pivot) + rt.anchoredPosition;
            var min = pivotPos - Vector2.Scale(rectSize, rt.pivot);
            return new Rect(min, rectSize);
        }

        private static Rect ToGuiRect(Rect r, Vector2 center) =>
            new Rect(center.x + r.x, center.y - r.yMax, r.width, r.height);
    }
}
