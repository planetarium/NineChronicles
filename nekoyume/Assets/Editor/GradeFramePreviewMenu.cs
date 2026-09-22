using System.Collections.Generic;
using System.Reflection;
using Coffee.UIEffects;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.UI.Module;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.EditorTools
{
    /// <summary>
    /// 9등급 아이템을 보유하지 않은 상태에서 등급 테두리(PLD-1612)의 모양을 확인한다.
    /// </summary>
    /// <remarks>
    /// 왜 필요한가: 울티밋 코스튬은 시트 id 와 클라 리소스가 어긋나 있어 지금 장착할 수가 없다.
    /// 테두리가 꽉 찬 아이콘 위에서 어떻게 보이는지는 아이템 없이도 확인할 수 있어야 한다.
    ///
    /// 어디를 가로채는가: 씬에 이미 그려진 슬롯을 직접 훑어 오버레이를 붙인다.
    /// 데이터를 바꾸는 게 아니라서 스크롤을 다시 그리면(셀 재사용) 원래대로 돌아간다 —
    /// 그때는 메뉴를 다시 실행하면 된다.
    /// </remarks>
    public static class GradeFramePreviewMenu
    {
        private const string MenuRoot = "Tools/Item/등급 테두리 미리보기/";

        /// <summary>배경까지 바꾼 슬롯의 원래 값. '끄기' 로 되돌린다.</summary>
        private static readonly List<Restore> Restores = new();

        private readonly struct Restore
        {
            public readonly Image Image;
            public readonly Sprite Sprite;
            public readonly UIHsvModifier Hsv;
            public readonly float Range, Hue, Saturation, Value;

            public Restore(Image image, UIHsvModifier hsv)
            {
                Image = image;
                Sprite = image.overrideSprite;
                Hsv = hsv;
                Range = hsv != null ? hsv.range : 0f;
                Hue = hsv != null ? hsv.hue : 0f;
                Saturation = hsv != null ? hsv.saturation : 0f;
                Value = hsv != null ? hsv.value : 0f;
            }

            public void Apply()
            {
                if (Image != null)
                {
                    Image.overrideSprite = Sprite;
                }

                if (Hsv != null)
                {
                    Hsv.range = Range;
                    Hsv.hue = Hue;
                    Hsv.saturation = Saturation;
                    Hsv.value = Value;
                }
            }
        }

        [MenuItem(MenuRoot + "테두리만 덧씌우기", priority = 0)]
        private static void FrameOnly() => Apply(false);

        [MenuItem(MenuRoot + "9등급처럼 보기 (배경+테두리)", priority = 1)]
        private static void LikeGrade9() => Apply(true);

        [MenuItem(MenuRoot + "끄기", priority = 20)]
        private static void Clear()
        {
            var restored = RestoreAll();

            var hidden = 0;
            foreach (var (grade, _, _) in EnumerateSlots())
            {
                GradeFrameHelper.Hide(grade);
                hidden++;
            }

            Debug.Log($"[등급테두리] 해제 — 테두리 {hidden}개 숨김, 배경 {restored}개 복구.");
        }

        [MenuItem(MenuRoot + "테두리만 덧씌우기", true)]
        [MenuItem(MenuRoot + "9등급처럼 보기 (배경+테두리)", true)]
        [MenuItem(MenuRoot + "끄기", true)]
        private static bool ValidatePlaying() => Application.isPlaying;

        /// <returns>되돌린 슬롯 수.</returns>
        private static int RestoreAll()
        {
            foreach (var restore in Restores)
            {
                restore.Apply();
            }

            var restored = Restores.Count;
            Restores.Clear();
            return restored;
        }

        private static void Apply(bool includeBackground)
        {
            var data = Resources
                .Load<ItemViewDataScriptableObject>("ScriptableObject/UI_ItemViewData")
                ?.GetItemViewData(9);

            if (data == null)
            {
                Debug.LogError("[등급테두리] UI_ItemViewData 에서 9등급 항목을 찾지 못했다.");
                return;
            }

            if (data.GradeFrameOverlay == null)
            {
                Debug.LogError(
                    "[등급테두리] 9등급 항목에 gradeFrameOverlay 가 비어 있다." +
                    " UI_ItemViewData.asset 의 9등급에 item_frame_9 를 지정해야 한다.");
                return;
            }

            // 두 번 실행하면 '이미 9등급으로 바꾼 값'을 원본으로 저장해 되돌릴 수 없게 된다.
            // 그래서 항상 직전 상태를 먼저 복구하고 새로 기록한다.
            RestoreAll();

            var count = 0;
            foreach (var (grade, icon, hsv) in EnumerateSlots())
            {
                if (includeBackground)
                {
                    Restores.Add(new Restore(grade, hsv));
                    grade.overrideSprite = data.GradeBackground;
                    if (hsv != null)
                    {
                        hsv.range = data.GradeHsvRange;
                        hsv.hue = data.GradeHsvHue;
                        hsv.saturation = data.GradeHsvSaturation;
                        hsv.value = data.GradeHsvValue;
                    }
                }

                GradeFrameHelper.ApplyGradeFrame(grade, icon, data);
                count++;
            }

            Debug.Log(
                $"[등급테두리] 슬롯 {count}개에 적용{(includeBackground ? " (배경 포함)" : "")}." +
                " 0개면 플레이 모드가 아니거나 아이템 화면이 열려 있지 않은 상태다.");
        }

        /// <summary>
        /// 씬에 있는 아이템 슬롯을 (등급배경, 아이콘, HSV) 로 돌려준다.
        /// </summary>
        private static IEnumerable<(Image grade, Image icon, UIHsvModifier hsv)> EnumerateSlots()
        {
            foreach (var view in Object.FindObjectsOfType<BaseItemView>(true))
            {
                if (view.GradeImage != null)
                {
                    yield return (view.GradeImage, view.ItemImage, view.GradeHsv);
                }
            }

            // EquipmentSlot 은 이미지 필드를 공개하지 않는다. 에디터 전용 도구라 리플렉션으로 읽는다.
            foreach (var slot in Object.FindObjectsOfType<EquipmentSlot>(true))
            {
                var grade = GetField<Image>(slot, "gradeImage");
                if (grade != null)
                {
                    yield return (grade, GetField<Image>(slot, "itemImage"),
                        GetField<UIHsvModifier>(slot, "gradeHsv"));
                }
            }
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            // 상속받은 private 필드는 GetField 가 돌려주지 않으므로 타입을 거슬러 올라간다.
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(target) as T;
                }
            }

            return null;
        }
    }
}
