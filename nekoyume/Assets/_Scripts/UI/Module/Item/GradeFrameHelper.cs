using Nekoyume.Game.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    /// <summary>
    /// 등급 배경에 장식이 있는 등급(9등급~)에서, 그 장식만 아이템 아이콘 '위'에 한 겹 더 그린다.
    /// </summary>
    /// <remarks>
    /// 슬롯은 등급 배경 위에 아이콘을 겹쳐 그린다. 1~8등급 배경은 민무늬라 아이콘이 덮어도
    /// 티가 나지 않았지만, 9등급 배경(item_bg_9)은 금색 장식이 그려져 있어서 코스튬처럼
    /// 모서리까지 꽉 찬 아이콘이 덮으면 테두리가 잘린 조각으로 보인다(PLD-1612).
    /// 배경 64px 안에 아이콘 58px 이 들어가 항상 드러나는 여백은 좌우 3px(폭의 4.7%)뿐이라,
    /// 아이콘을 줄이는 것만으로는 99px 자산에 그려진 장식을 담을 수 없다.
    /// <para>
    /// 슬롯 프리팹이 수십 개라 프리팹마다 레이어를 추가하는 대신 런타임에 만들어 붙인다.
    /// 장식이 없는 등급(<see cref="ItemViewData.GradeFrameOverlay"/> 가 null)에서는
    /// 오버레이를 꺼두기만 하므로 기존 등급의 그림은 바뀌지 않는다.
    /// </para>
    /// </remarks>
    public static class GradeFrameHelper
    {
        private const string OverlayName = "GradeFrameOverlay";

        /// <param name="gradeImage">등급 배경. 오버레이는 이 RectTransform 을 그대로 따라간다.</param>
        /// <param name="itemImage">
        /// 아이템 아이콘. 같은 부모를 쓰면 이 바로 위에 끼워 넣는다 — 강화 수치 같은 뱃지는
        /// 보통 더 뒤 형제라 계속 테두리 위에 남는다. null 이면 맨 위로 올린다.
        /// </param>
        public static void ApplyGradeFrame(Image gradeImage, Image itemImage, ItemViewData data)
        {
            if (gradeImage == null)
            {
                return;
            }

            var sprite = data?.GradeFrameOverlay;
            var overlay = Find(gradeImage);

            if (sprite == null)
            {
                if (overlay != null)
                {
                    overlay.gameObject.SetActive(false);
                }

                return;
            }

            overlay = overlay != null ? overlay : Create(gradeImage);
            if (overlay == null)
            {
                return;
            }

            overlay.overrideSprite = sprite;
            overlay.gameObject.SetActive(true);

            var overlayTransform = overlay.transform;
            if (itemImage != null && itemImage.transform.parent == overlayTransform.parent)
            {
                overlayTransform.SetSiblingIndex(itemImage.transform.GetSiblingIndex() + 1);
            }
            else
            {
                overlayTransform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 슬롯이 비워질 때 호출한다. 오버레이는 별도 오브젝트라
        /// <c>gradeImage.enabled = false</c> 로는 같이 사라지지 않는다.
        /// </summary>
        public static void Hide(Image gradeImage)
        {
            if (gradeImage == null)
            {
                return;
            }

            var overlay = Find(gradeImage);
            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
            }
        }

        private static Image Find(Image gradeImage)
        {
            var parent = gradeImage.transform.parent;
            if (parent == null)
            {
                return null;
            }

            var found = parent.Find(OverlayName);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static Image Create(Image gradeImage)
        {
            var source = (RectTransform)gradeImage.transform;
            if (source.parent == null)
            {
                return null;
            }

            var go = new GameObject(OverlayName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(source.parent, false);

            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
            rect.localScale = source.localScale;
            rect.localRotation = source.localRotation;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            // 등급 배경이 Simple 이라 여기도 Simple 이어야 한다. Sliced 로 두면 보더가
            // 슬롯 크기와 무관하게 고정 픽셀로 그려져 배경의 장식과 배율이 어긋나고,
            // 보더 바깥(가운데 영역)으로 밀려난 페이드 구간이 통째로 안 그려진다.
            image.type = Image.Type.Simple;
            return image;
        }
    }
}
