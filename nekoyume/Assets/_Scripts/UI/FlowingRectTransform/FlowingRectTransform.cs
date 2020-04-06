using System;
using System.Collections;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class FlowingRectTransform : MonoBehaviour
    {
        public enum Direction
        {
            Horizontal,
            Vertical
        }

        public Direction direction;
        public float speed;
        public float beginningPoint;
        public float endPoint;

        protected RectTransform RectTransform { get; private set; }

        protected void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        protected void OnEnable()
        {
            if (Mathf.Approximately(speed, 0f))
            {
                return;
            }

            switch (direction)
            {
                case Direction.Horizontal:
                    StartCoroutine(CoFlowingHorizontal());
                    break;
                case Direction.Vertical:
                    StartCoroutine(CoFlowingVertical());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void Reset()
        {
            direction = Direction.Horizontal;
            speed = -1f;
            RectTransform = GetComponent<RectTransform>();

            var parent = RectTransform.parent;
            if (parent is null)
            {
                beginningPoint = -RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopLeft).x;
                endPoint = -RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopRight).x;

                return;
            }

            var parentRectTransform = parent.GetComponent<RectTransform>();
            if (parentRectTransform is null)
            {
                beginningPoint = -RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopLeft).x;
                endPoint = -RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopRight).x;

                return;
            }

            var widthHalf = parentRectTransform.rect.width / 2;
            beginningPoint = widthHalf - RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopLeft).x;
            endPoint = -widthHalf - RectTransform.GetPivotPositionFromAnchor(PivotPresetType.TopRight).x;
        }

        private IEnumerator CoFlowingHorizontal()
        {
            var anchoredPosition = RectTransform.anchoredPosition;

            while (enabled)
            {
                anchoredPosition.x += speed * Time.deltaTime;
                if (speed > 0f)
                {
                    if (anchoredPosition.x > endPoint)
                    {
                        anchoredPosition.x = beginningPoint + anchoredPosition.x - endPoint;
                    }
                }
                else
                {
                    if (anchoredPosition.x < endPoint)
                    {
                        anchoredPosition.x = beginningPoint - (endPoint - anchoredPosition.x);
                    }
                }

                RectTransform.anchoredPosition = anchoredPosition;

                yield return null;
            }
        }

        private IEnumerator CoFlowingVertical()
        {
            var anchoredPosition = RectTransform.anchoredPosition;

            while (enabled)
            {
                anchoredPosition.y += speed * Time.deltaTime;
                if (speed > 0f)
                {
                    if (anchoredPosition.y > endPoint)
                    {
                        anchoredPosition.y = beginningPoint + anchoredPosition.y - endPoint;
                    }
                }
                else
                {
                    if (anchoredPosition.y < endPoint)
                    {
                        anchoredPosition.y = beginningPoint - (endPoint - anchoredPosition.y);
                    }
                }

                RectTransform.anchoredPosition = anchoredPosition;

                yield return null;
            }
        }
    }
}
