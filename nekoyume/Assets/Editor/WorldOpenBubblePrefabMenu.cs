using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NekoyumeEditor
{
    /// <summary>
    /// 월드 버튼에 하드월드 개방 조건 말풍선(<see cref="WorldOpenStatusBubble"/>)을 붙인다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 붙이는 곳은 WorldButton 프리팹과, UI_WorldMap 에 프리팹 인스턴스가 아니라 직접 만든 월드 버튼(VanaheimButton)이다.
    /// 월드맵의 다른 월드 버튼은 WorldButton 프리팹 인스턴스라 함께 바뀐다. 이름이 Hard 로 시작하는 옛 배치와 월드가
    /// 아닌 버튼(이벤트 던전·탑·모험 보스)은 건너뛰고, 거기 붙은 말풍선은 지운다.
    /// </para>
    /// <para>
    /// 말풍선은 버튼 위쪽 가운데에 꼬리 끝이 오도록 놓고 꺼 둔다(월드맵이 다음 지역에만 켠다). 이미 말풍선이 있는 버튼은
    /// 그대로 둔다 — 새로 만들면 식별값이 바뀌어 월드맵에서 버튼별로 손본 위치·꼬리 방향이 끊긴다. 모양을 바꿔 새로
    /// 만들려면 '말풍선 다시 만들기'를 쓴다(버튼별 배치가 초기화된다).
    /// 글자는 그 버튼의 월드 이름 텍스트(LockNameImage/Text)를 복제해 같은 글꼴·다국어 글꼴 교체를 쓴다.
    /// </para>
    /// </remarks>
    public static class WorldOpenBubblePrefabMenu
    {
        private const string ButtonPrefabPath = "Assets/AddressableAssets/UI/Module/WorldMap/WorldButton.prefab";
        private const string WorldMapPrefabPath = "Assets/Resources/UI/Prefabs/UI_WorldMap.prefab";
        private const string SpritePath =
            "Assets/UIResources/Atlas_image/Common/UI_Worldmap/UI_worldmap_open_tooltip.png";
        private const string BubbleName = "OpenStatusBubble";
        private const string TemplatePath = "LockNameImage/Text";

        // 바탕 그림(301×98) 원래 크기. 아래 17% 는 꼬리다.
        private static readonly Vector2 BubbleSize = new(301f, 98f);
        private static readonly Color LabelColor = new(0.80f, 0.75f, 0.66f);

        [MenuItem("Tools/WorldMap/개방 조건 말풍선 붙이기")]
        private static void Attach() => Attach(recreate: false);

        [MenuItem("Tools/WorldMap/개방 조건 말풍선 다시 만들기 (버튼별 배치 초기화)")]
        private static void Recreate()
        {
            if (EditorUtility.DisplayDialog(
                    "말풍선 다시 만들기",
                    "모든 월드 버튼의 말풍선을 새로 만든다. 월드맵에서 버튼별로 옮긴 위치·꼬리 방향이 초기화된다.",
                    "다시 만들기",
                    "취소"))
            {
                Attach(recreate: true);
            }
        }

        private static void Attach(bool recreate)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[WorldOpen] 말풍선 그림이 없다: {SpritePath}");
                return;
            }

            EditPrefab(ButtonPrefabPath, root =>
            {
                var button = root.GetComponent<WorldButton>();
                return button != null && AttachTo(button, sprite, recreate) ? 1 : 0;
            });

            // 월드맵에 직접 만든 월드 버튼. 프리팹 인스턴스는 위에서 바뀐 WorldButton 프리팹을 따른다.
            EditPrefab(WorldMapPrefabPath, root =>
            {
                var count = 0;
                foreach (var button in root.GetComponentsInChildren<WorldButton>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(button))
                    {
                        continue;
                    }

                    if (!IsWorldSlot(button))
                    {
                        var stray = button.transform.Find(BubbleName);
                        if (stray != null)
                        {
                            Object.DestroyImmediate(stray.gameObject);
                            count++;
                        }

                        continue;
                    }

                    if (AttachTo(button, sprite, recreate))
                    {
                        count++;
                    }
                }

                return count;
            });
        }

        private static void EditPrefab(string path, System.Func<GameObject, int> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var count = edit(root);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                Debug.Log($"[WorldOpen] {path}: 월드 버튼 {count}개의 {BubbleName} 을 붙이거나 지웠다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>월드 자리로 쓰는 버튼인지. 옛 Hard 배치·이벤트 던전·탑·모험 보스는 아니다.</summary>
        private static bool IsWorldSlot(WorldButton button)
        {
            var name = button.name;
            return !name.StartsWith("Hard") &&
                !name.StartsWith("EventDungeon") &&
                !name.StartsWith("InfiniteTower") &&
                !name.StartsWith("AdventureBoss");
        }

        /// <returns>프리팹을 바꿨는지.</returns>
        private static bool AttachTo(WorldButton button, Sprite sprite, bool recreate)
        {
            var template = button.transform.Find(TemplatePath)?.GetComponent<TextMeshProUGUI>();
            if (template == null)
            {
                Debug.LogError($"[WorldOpen] {button.name} 에서 {TemplatePath} 를 찾지 못했다.");
                return false;
            }

            var old = button.transform.Find(BubbleName);
            if (old != null)
            {
                if (!recreate)
                {
                    return false;
                }

                Object.DestroyImmediate(old.gameObject);
            }

            var bubble = new GameObject(BubbleName, typeof(RectTransform), typeof(WorldOpenStatusBubble));
            var rect = (RectTransform)bubble.transform;
            rect.SetParent(button.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = BubbleSize;

            // 바탕은 따로 둔다 — 꼬리를 위로 할 때 바탕만 뒤집는다.
            var background = NewRect("Background", rect, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.raycastTarget = false; // "눌러서 열기"일 때만 말풍선 코드가 켠다.

            // 본문은 꼬리를 뺀 몸통 안쪽, 좌우 여백 7.5%. 제목 한 줄 + 조건·기간 두 줄.
            var body = NewRect("Body", rect, new Vector2(0.075f, 0.21f), new Vector2(0.925f, 0.96f));
            var title = NewText(template, body, "Title", 17f, TextAlignmentOptions.Left,
                new Vector2(0f, 0.66f), Vector2.one);
            var conditionRow = NewRect("ConditionRow", body, new Vector2(0f, 0.33f), new Vector2(1f, 0.66f));
            var conditionLabel = NewText(template, conditionRow, "Label", 15f, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one);
            var conditionValue = NewText(template, conditionRow, "Value", 15f, TextAlignmentOptions.Right,
                Vector2.zero, Vector2.one);
            var periodRow = NewRect("PeriodRow", body, Vector2.zero, new Vector2(1f, 0.33f));
            var periodLabel = NewText(template, periodRow, "Label", 15f, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one);
            var periodValue = NewText(template, periodRow, "Value", 15f, TextAlignmentOptions.Right,
                Vector2.zero, Vector2.one);
            conditionLabel.color = LabelColor;
            periodLabel.color = LabelColor;

            var bubbleObject = new SerializedObject(bubble.GetComponent<WorldOpenStatusBubble>());
            bubbleObject.FindProperty("background").objectReferenceValue = background;
            bubbleObject.FindProperty("body").objectReferenceValue = body;
            bubbleObject.FindProperty("titleText").objectReferenceValue = title;
            bubbleObject.FindProperty("conditionRow").objectReferenceValue = conditionRow.gameObject;
            bubbleObject.FindProperty("conditionLabelText").objectReferenceValue = conditionLabel;
            bubbleObject.FindProperty("conditionValueText").objectReferenceValue = conditionValue;
            bubbleObject.FindProperty("periodLabelText").objectReferenceValue = periodLabel;
            bubbleObject.FindProperty("periodValueText").objectReferenceValue = periodValue;
            bubbleObject.ApplyModifiedPropertiesWithoutUndo();

            var buttonObject = new SerializedObject(button);
            buttonObject.FindProperty("openStatusBubble").objectReferenceValue =
                bubble.GetComponent<WorldOpenStatusBubble>();
            buttonObject.ApplyModifiedPropertiesWithoutUndo();

            bubble.SetActive(false);
            return true;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax);
            return rect;
        }

        private static TextMeshProUGUI NewText(
            TextMeshProUGUI template,
            RectTransform parent,
            string name,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = Object.Instantiate(template.gameObject, parent, false);
            go.name = name;
            go.SetActive(true);
            for (var i = go.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            }

            var rect = (RectTransform)go.transform;
            Stretch(rect, anchorMin, anchorMax);
            rect.localScale = Vector3.one;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            // 월드 이름 글자는 버튼 재질(어두운 테두리)·작은 대문자라 작은 글씨가 뭉개진다. 기본 재질·보통 글꼴로 바꾼다.
            text.fontSharedMaterial = text.font.material;
            text.fontStyle = FontStyles.Normal;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            // 말줄임은 줄 높이가 영역보다 크면 그 줄을 통째로 숨긴다(제목이 안 보였던 원인). 넘쳐도 그린다.
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            // 템플릿의 다국어 키·기억해 둔 글자 크기·스타일·재질을 지운다. 문구는 말풍선 코드가 넣는다.
            var l10n = go.GetComponent<L10nTextMeshProUGUI>();
            if (l10n != null)
            {
                var l10nObject = new SerializedObject(l10n);
                l10nObject.FindProperty("l10nKey").stringValue = string.Empty;
                l10nObject.FindProperty("fontMaterialType").enumValueIndex = (int)FontMaterialType.Default;
                l10nObject.FindProperty("defaultFontSizeInitialized").boolValue = false;
                l10nObject.FindProperty("defaultFontStylesInitialized").boolValue = false;
                l10nObject.FindProperty("fontMaterialIndexInitialized").boolValue = false;
                l10nObject.ApplyModifiedPropertiesWithoutUndo();
            }

            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
