using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// (NCU) 인게임 팝업 프리팹 스캐폴드 생성용 에디터 툴 — 일회성. batchmode -executeMethod로 실행.
//   Smoke: 라이선스/컴파일/executeMethod 동작 확인용 no-op.
//   Build: UI_NcuServerBlock.prefab 생성 + UI_NcuPopup.prefab에 컨테이너/notice/originBlock 배선.
//   Nekoyume 타입은 Type.GetType(", Nekoyume")로 참조(어셈블리 경계 회피), 필드는 SerializedObject로 배선.
//   ⚠️ 레이아웃은 대략값 — 에디터에서 비주얼 폴리싱 필요. 색/치수는 임의.
public static class NcuPrefabBuilder
{

    // 배치모드에서만 프로세스를 끝낸다. 에디터에서 메뉴로 실행할 때 Exit을 부르면 에디터가 통째로 닫힌다.
    private static void Finish(int code)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(code);
        }
    }


    private const string NcuNs = "Nekoyume.UI.Module.Ncu.";
    private const string ServerBlockPath = "Assets/Resources/UI/Prefabs/UI_NcuServerBlock.prefab";
    private const string PopupPath = "Assets/Resources/UI/Prefabs/UI_NcuPopup.prefab";

    // 팝업 프리팹 계층 + 컴포넌트 덤프(배치 위치 파악용, 읽기전용).
    [MenuItem("Tools/NCU/계층 덤프")]
    public static void Dump()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
        if (go == null) { Debug.LogError("[NcuPrefabBuilder] load failed: " + PopupPath); Finish(1); return; }
        var sb = new System.Text.StringBuilder();
        Walk(go.transform, 0, sb);
        Debug.Log("[NcuPrefabBuilder] HIERARCHY BEGIN\n" + sb + "[NcuPrefabBuilder] HIERARCHY END");
        Finish(0);
    }

    private static void Walk(Transform t, int depth, System.Text.StringBuilder sb)
    {
        var comps = t.GetComponents<Component>();
        var names = new System.Collections.Generic.List<string>();
        foreach (var c in comps) names.Add(c == null ? "<missing>" : c.GetType().Name);
        sb.Append(new string(' ', depth * 2)).Append(t.name).Append("  [").Append(string.Join(", ", names)).Append("]\n");
        foreach (Transform c in t) Walk(c, depth + 1, sb);
    }

    // 팝업을 임시 캔버스에 띄워 실제 rect/월드좌표 덤프(정렬 진단). 절대값은 캔버스 크기 가정이지만
    //   상대 관계(블록이 ContensArea/프레임 우측을 넘는지)는 정확히 드러남.
    [MenuItem("Tools/NCU/3. 배치 실측 덤프")]
    public static void DumpGeom()
    {
        GameObject canvasGo = null;
        try
        {
            canvasGo = new GameObject("TmpCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(1920, 1080);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
            var instRt = inst.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(instRt);
            Canvas.ForceUpdateCanvases();

            var sb = new System.Text.StringBuilder();
            var probe = new[] { "UI_NcuPopup", "Modal", "CenterArea", "TabArea", "ContensArea", "EventView", "ContentImage", "Bg", "ServerRail", "ServerBlock0", "ServerBlock1", "CopyColumn", "HeroLogo", "HeroBenefit", "HeroHook", "HeroCta", "NcuNotice" };
            foreach (var nm in probe)
            {
                var t = FindByName(inst.transform, nm);
                if (t == null) { sb.Append(nm).Append(": NOT FOUND\n"); continue; }
                var rt = t.GetComponent<RectTransform>();
                if (rt == null) { sb.Append(nm).Append(": no RectTransform\n"); continue; }
                var r = rt.rect;
                var c = new Vector3[4];
                rt.GetWorldCorners(c);
                sb.AppendFormat("{0}: rect(w={1:F0} h={2:F0}) worldX[{3:F0}..{4:F0}] worldY[{5:F0}..{6:F0}] aMin{7} aMax{8} pivot{9} sizeDelta{10} pos{11}\n",
                    nm, r.width, r.height, c[0].x, c[2].x, c[0].y, c[2].y,
                    rt.anchorMin, rt.anchorMax, rt.pivot, rt.sizeDelta, rt.anchoredPosition);
            }
            Debug.Log("[NcuPrefabBuilder] GEOM BEGIN\n" + sb + "GEOM END");
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[NcuPrefabBuilder] DUMPGEOM FAILED: " + e);
            Finish(1);
        }
        finally
        {
            // 예외로 빠져나가도 임시 캔버스를 씬에 남기지 않는다.
            if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
        }
    }

    // fraction 앵커(stretch) — 부모의 [xMin..xMax]×[yMin..yMax] 영역을 채우는 고정 프레임.
    private static void AnchorFrac(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---- 타입 해석 ----
    private static Type T(string shortName)
    {
        var full = NcuNs + shortName + ", Nekoyume";
        var t = Type.GetType(full);
        if (t == null) throw new Exception("Type not found: " + full);
        return t;
    }

    private static Type PopupType()
    {
        var t = Type.GetType("Nekoyume.UI.NcuPopup, Nekoyume");
        if (t == null) throw new Exception("Type not found: Nekoyume.UI.NcuPopup, Nekoyume");
        return t;
    }

    // ---- UI 헬퍼 ----
    private static GameObject UI(string name, Transform parent, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        if (parent != null) rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    // 팝업에 이미 쓰이는 TMP 폰트를 재사용한다.
    //   AddComponent만 하면 font가 비고, batchmode에서는 TMP 기본 폰트도 안 잡혀서
    //   값·활성·알파가 다 정상인데 글자만 렌더링되지 않는 상태가 된다.
    private static TMP_FontAsset _refFont;

    private static TMP_FontAsset ReferenceFont()
    {
        if (_refFont != null) return _refFont;

        var popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
        if (popup != null)
        {
            foreach (var t in popup.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font != null) { _refFont = t.font; break; }
            }
        }

        if (_refFont == null && TMP_Settings.defaultFontAsset != null)
        {
            _refFont = TMP_Settings.defaultFontAsset;
        }

        if (_refFont == null) Debug.LogWarning("[NcuPrefabBuilder] TMP 폰트를 못 찾았다 — 글자가 안 보일 수 있다.");
        return _refFont;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string txt, float size, float h, Color color)
    {
        var go = UI(name, parent, 340, h);
        var t = go.AddComponent<TextMeshProUGUI>();
        var font = ReferenceFont();
        if (font != null)
        {
            t.font = font;
            // 머티리얼까지 명시해야 한다. font만 지정하면 TMP가 머티리얼 인스턴스를 만드는데
            //   그 인스턴스는 SaveAsPrefabAsset에 안 실려서 끊긴 참조로 남고,
            //   그 결과 폰트는 멀쩡한데 characterCount=0으로 글자가 하나도 안 그려진다.
            t.fontSharedMaterial = font.material;
        }
        t.text = txt;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Left;
        t.enableWordWrapping = true;
        return t;
    }

    private static Button Button(string name, Transform parent, string label, out GameObject go)
    {
        go = UI(name, parent, 200, 48);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.16f, 0.17f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = Text("Label", go.transform, label, 20, 48, Color.white);
        t.alignment = TextAlignmentOptions.Center;
        var lrt = t.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        return btn;
    }

    // 폭을 강제로 늘리지 않는 세로 레이아웃 — 자식이 자기 preferredWidth를 지킨다(로고 240 상한 등).
    private static void VLayoutLeft(GameObject go, int spacing, RectOffset padding)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
        v.childAlignment = TextAnchor.UpperLeft;
        if (padding != null) v.padding = padding;
    }

    private static void VLayout(GameObject go, int spacing, RectOffset padding)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childControlWidth = true;
        v.childControlHeight = false; // 자식은 각자 sizeDelta.y 유지
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        if (padding != null) v.padding = padding;
        var f = go.AddComponent<ContentSizeFitter>();
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void Set(SerializedObject so, string field, UnityEngine.Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) throw new Exception("field not found on " + so.targetObject.GetType().Name + ": " + field);
        p.objectReferenceValue = value;
    }

    private static Transform FindByName(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindByName(c, name);
            if (r != null) return r;
        }
        return null;
    }

    private static void HLayout(GameObject go, int spacing, RectOffset padding, TextAnchor align)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.childAlignment = align;
        if (padding != null) h.padding = padding;
    }


    // ---- 시안 B 서버 행 — 기존 프리팹을 복제해 조립한다 ----
    //   맨바닥에서 TextMeshProUGUI를 AddComponent하면 폰트/머티리얼이 비어 글자가 아예 안 그려진다
    //   (프리팹에 끊긴 머티리얼 참조가 남는다). 그래서 텍스트도 버튼도 "이미 정상인 프리팹"에서
    //   복제해 값만 바꾼다. 그러면 폰트·머티리얼·나인슬라이스가 그대로 따라온다.
    private const string YellowButtonPath = "Assets/AddressableAssets/UI/Module/Button/YellowButton.prefab";
    private const string ServerRowPath = "Assets/Resources/UI/Prefabs/UI_NcuServerRow.prefab";

    [MenuItem("Tools/NCU/서버 행 만들기 (기존 프리팹 복제)")]
    public static void BuildServerRowFromTemplate()
    {
        GameObject root = null;
        try
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "서버 행 프리팹 새로 만들기", "UI_NcuServerRow.prefab을 통째로 덮어씁니다.\n에디터에서 다듬은 폰트·색·치수가 모두 사라집니다.", "진행", "취소"))
            {
                return;
            }

            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(YellowButtonPath);
            if (buttonPrefab == null) throw new Exception("템플릿 버튼을 못 찾음: " + YellowButtonPath);

            const float rowH = 48f, rowW = 246f;

            root = UI("UI_NcuServerRow", null, rowW, rowH);
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.09f, 0.09f, 0.10f, 0.95f);
            HLayout(root, 10, new RectOffset(12, 10, 6, 6), TextAnchor.MiddleLeft);
            root.AddComponent<LayoutElement>().preferredHeight = rowH;
            var block = root.AddComponent(T("NcuServerBlock"));

            // 상태 점 — 스프라이트는 나중에 인스펙터에서. 지금은 색만.
            var dot = UI("Dot", root.transform, 10, 10);
            var dotImg = dot.AddComponent<Image>();
            dotImg.color = new Color(0.85f, 0.70f, 0.30f);
            // 스프라이트가 없으면 네모로 그려진다. 공용 아틀라스의 점을 쓴다.
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UIResources/Atlas_image/Common/UI_Common/Common_dot.png");
            if (dotSprite != null) dotImg.sprite = dotSprite;
            else Debug.LogWarning("[NcuPrefabBuilder] Common_dot 스프라이트를 못 찾음 — 점이 네모로 보인다.");
            var dotLe = dot.AddComponent<LayoutElement>();
            dotLe.preferredWidth = 10;
            dotLe.preferredHeight = 10;

            // 액션 버튼 먼저 복제 — 여기서 텍스트 템플릿도 얻는다.
            var actionGo = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab);
            PrefabUtility.UnpackPrefabInstance(actionGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            actionGo.name = "ActionButton";

            var textTemplate = FindTmpTemplate(actionGo.transform);
            if (textTemplate == null) throw new Exception("템플릿에서 TMP 텍스트를 못 찾음");

            // 좌측 — 이름/상태. 템플릿 텍스트를 복제해 폰트·머티리얼을 물려받는다.
            var left = UI("Left", root.transform, 120, rowH);
            VLayoutLeft(left, 1, null);
            var leftLe = left.AddComponent<LayoutElement>();
            leftLe.flexibleWidth = 1;
            leftLe.preferredWidth = 120; // 0으로 눌려 글자가 사라지는 것 방지

            var nameText = CloneText(textTemplate, left.transform, "NameText", "V8", 15,
                new Color(0.93f, 0.94f, 0.96f), FontStyles.Bold, 20);
            var badgeText = CloneText(textTemplate, left.transform, "BadgeText", "Linked", 12,
                new Color(0.60f, 0.62f, 0.66f), FontStyles.Normal, 16);

            // 버튼을 행에 붙인다(텍스트 복제가 끝난 뒤).
            actionGo.transform.SetParent(root.transform, false);
            var actionRt = actionGo.GetComponent<RectTransform>();
            actionRt.sizeDelta = new Vector2(74, 32);
            var actionLe = actionGo.GetComponent<LayoutElement>() ?? actionGo.AddComponent<LayoutElement>();
            actionLe.preferredWidth = 74;
            actionLe.preferredHeight = 32;

            var actionLabel = FindTmpTemplate(actionGo.transform);
            // 버튼은 활성/비활성 라벨을 따로 들고 있다. 둘 다 같은 문구로 맞춘다.
            foreach (var lbl in actionGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                lbl.text = "Wallet";
            }
            var actionButton = actionGo.GetComponent<Button>() ?? actionGo.AddComponent<Button>();

            var so = new SerializedObject(block);
            Set(so, "nameText", nameText);
            Set(so, "badgeText", badgeText);
            Set(so, "serverLabelRoot", left);
            Set(so, "stateDot", dotImg);
            Set(so, "actionButton", actionButton);
            if (actionLabel != null) Set(so, "actionLabel", actionLabel);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[NcuPrefabBuilder] 배선 확인 name={(nameText != null ? nameText.name : "NULL")} " +
                      $"badge={(badgeText != null ? badgeText.name : "NULL")} " +
                      $"dot={(dotImg != null ? dotImg.name : "NULL")} " +
                      $"btn={(actionButton != null ? actionButton.name : "NULL")} " +
                      $"label={(actionLabel != null ? actionLabel.name : "NULL")}");

            System.IO.Directory.CreateDirectory("Assets/Resources/UI/Prefabs");
            var saved = PrefabUtility.SaveAsPrefabAsset(root, ServerRowPath);
            if (saved == null) throw new Exception("저장 실패: " + ServerRowPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NcuPrefabBuilder] SERVER-ROW(TEMPLATE) DONE -> " + ServerRowPath);
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[NcuPrefabBuilder] SERVER-ROW(TEMPLATE) FAILED: " + e);
            Finish(1);
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // 활성 상태인 TMP 텍스트 하나(복제 원본용).
    private static TextMeshProUGUI FindTmpTemplate(Transform t)
    {
        TextMeshProUGUI fallback = null;
        foreach (var tmp in t.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.font == null) continue;
            // 버튼에는 비활성 라벨(Text (TMP)_Disable)도 있다. 실제로 보이는 쪽을 고른다.
            if (tmp.gameObject.activeInHierarchy || tmp.gameObject.activeSelf) return tmp;
            fallback ??= tmp;
        }
        return fallback;
    }

    // 템플릿 텍스트를 복제 — 폰트/머티리얼을 그대로 물려받는다.
    private static TextMeshProUGUI CloneText(
        TextMeshProUGUI template, Transform parent, string name, string txt,
        float size, Color color, FontStyles style, float height)
    {
        var clone = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
        clone.name = name;
        clone.SetActive(true);
        var t = clone.GetComponent<TextMeshProUGUI>();
        // 순서가 중요하다. 템플릿은 오토사이즈가 켜진 상태이고, 켜진 채로 fontSize를 지정하면
        //   TMP가 곧바로 자기 계산값으로 덮어쓴다. 그 뒤에 오토사이즈를 끄면 fontSize와
        //   fontSizeBase가 어긋난 채 직렬화되어, 메시가 생성되지 않고 글자가 안 보인다
        //   (인스펙터에서 오토사이즈를 토글하면 그제서야 보이는 증상).
        t.enableAutoSizing = false;
        t.fontSize = size;
        t.fontSizeMin = size;
        t.fontSizeMax = size;
        t.text = txt;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Left;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        var rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(160, height);
        var le = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.preferredWidth = 160;

        t.ForceMeshUpdate();
        Debug.Log($"[NcuPrefabBuilder] CloneText '{name}' 요청='{txt}' 실제='{t.text}' " +
                  $"size={t.fontSize} auto={t.enableAutoSizing} font={(t.font != null ? t.font.name : "NULL")}");
        return t;
    }


    // 프리팹을 다시 굽지 않고 NcuServerBlock의 참조만 다시 건다.
    //   손으로 다듬은 오브젝트(텍스트 에셋 교체 등)를 그대로 두고 배선만 고칠 때 쓴다.
    //   이름으로 찾아 붙이므로, 오브젝트 이름만 유지하면 구조를 바꿔도 동작한다.
    [MenuItem("Tools/NCU/서버 행 배선만 다시 걸기")]
    public static void WireServerRow()
    {
        var root = PrefabUtility.LoadPrefabContents(ServerRowPath);
        try
        {
            var block = root.GetComponent(T("NcuServerBlock"))
                        ?? root.GetComponentInChildren(T("NcuServerBlock"), true);
            if (block == null) throw new Exception("NcuServerBlock 컴포넌트가 없다: " + ServerRowPath);

            var so = new SerializedObject(block);
            var log = new System.Text.StringBuilder("[NcuPrefabBuilder] 배선\n");

            WireByName(so, root.transform, "serverLabelRoot", "Left", log);
            WireByName(so, root.transform, "nameText", "NameText", log);
            WireByName(so, root.transform, "badgeText", "BadgeText", log);
            WireByName(so, root.transform, "stateDot", "Dot", log);
            WireByName(so, root.transform, "actionButton", "ActionButton", log);

            // 버튼 라벨은 ActionButton 아래의 "보이는" 텍스트.
            var btn = FindByName(root.transform, "ActionButton");
            var label = btn != null ? FindTmpTemplate(btn) : null;
            if (label != null)
            {
                var p = so.FindProperty("actionLabel");
                if (p != null)
                {
                    p.objectReferenceValue = label;
                    log.Append("  actionLabel <- ").Append(label.name).Append('\n');
                }
            }
            else log.Append("  actionLabel <- (못 찾음)\n");

            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ServerRowPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(log.ToString());
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[NcuPrefabBuilder] WIRE FAILED: " + e);
            Finish(1);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 이름으로 오브젝트를 찾아 필드 타입에 맞는 컴포넌트를 붙인다.
    private static void WireByName(
        SerializedObject so, Transform root, string field, string objectName, System.Text.StringBuilder log)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            log.Append("  ").Append(field).Append(" <- (필드 없음)\n");
            return;
        }

        var t = FindByName(root, objectName);
        if (t == null)
        {
            log.Append("  ").Append(field).Append(" <- (오브젝트 '").Append(objectName).Append("' 없음)\n");
            return;
        }

        UnityEngine.Object value = t.gameObject;
        var typeName = prop.type; // 예: PPtr<$TextMeshProUGUI>
        if (typeName.Contains("TextMeshProUGUI")) value = t.GetComponent<TextMeshProUGUI>();
        else if (typeName.Contains("Button")) value = t.GetComponent<Button>();
        else if (typeName.Contains("Image")) value = t.GetComponent<Image>();

        prop.objectReferenceValue = value;
        log.Append("  ").Append(field).Append(" <- ").Append(objectName)
           .Append(value == null ? "  ⚠️ 컴포넌트 없음" : "").Append('\n');
    }


    // 완성된 서버 행 프리팹을 팝업에 꽂는다.
    //   여기서는 텍스트를 새로 만들지 않는다 — 프리팹 인스턴스를 넣고 참조만 건다.
    //   행 2개를 담는 ServerRail을 TabArea 아래에 두고, 런타임에 NcuPopup이
    //   선택된 탭 바로 아래로 옮긴다(MoveServerRailUnderSelectedTab).
    [MenuItem("Tools/NCU/서버 행을 팝업에 꽂기")]
    public static void MountServerRows()
    {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "서버 행을 팝업에 꽂기", "UI_NcuPopup.prefab의 ServerRail을 지우고 새로 만듭니다.\n레일에 손댄 설정이 있으면 사라집니다.", "진행", "취소"))
            {
                return;
            }

        var root = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServerRowPath);
            if (rowPrefab == null) throw new Exception("행 프리팹이 없다: " + ServerRowPath);

            var tabArea = FindByName(root.transform, "TabArea")
                          ?? FindByName(root.transform, "CenterArea")
                          ?? root.transform;

            // 이전 산출물 정리 — 옛 세로 카드와 레일.
            foreach (var nm in new[] { "ServerRail", "ServerBlock0", "ServerBlock1", "ServerBlockContainer" })
            {
                var ex = FindByName(root.transform, nm);
                if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);
            }

            // 레일 — 행을 세로로 쌓고, 좌측 레일처럼 들여쓴다.
            var rail = UI("ServerRail", tabArea, 260, 100);
            var vlg = rail.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(14, 0, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            // 높이는 런타임에 켜진 행 수만큼 NcuPopup이 다시 잡는다(ResizeServerRail).
            rail.AddComponent<LayoutElement>().preferredHeight = 100;

            var blockType = T("NcuServerBlock");
            var blocks = new Component[2];
            for (var i = 0; i < 2; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, rail.transform);
                inst.name = "ServerBlock" + i;
                blocks[i] = inst.GetComponent(blockType);
                if (blocks[i] == null) throw new Exception("행 프리팹에 NcuServerBlock이 없다");
            }

            var popup = root.GetComponent(PopupType()) ?? root.GetComponentInChildren(PopupType(), true);
            if (popup == null) throw new Exception("NcuPopup 컴포넌트를 못 찾음");

            var so = new SerializedObject(popup);
            var arr = so.FindProperty("ncuServerBlocks");
            if (arr == null) throw new Exception("ncuServerBlocks 필드 없음");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = blocks[0];
            arr.GetArrayElementAtIndex(1).objectReferenceValue = blocks[1];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NcuPrefabBuilder] MOUNT DONE — ServerRail({tabArea.name} 아래) + 행 2개 배선 완료");
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[NcuPrefabBuilder] MOUNT FAILED: " + e);
            Finish(1);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }


}
