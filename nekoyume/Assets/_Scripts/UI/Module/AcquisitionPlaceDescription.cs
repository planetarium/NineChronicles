using System;
using Nekoyume.EnumType;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class AcquisitionPlaceDescription : MonoBehaviour
    {
        [SerializeField]
        private RectTransform leftDeco;

        [SerializeField]
        private RectTransform rightDeco;

        [SerializeField]
        private RectTransform contentsRectTransform;

        [SerializeField]
        private Button bgButton;

        private static Camera _mainCamera;

        private static readonly float2 DescriptionOffset = new float2(200f, 0f);

        private void Awake()
        {
            _mainCamera = Camera.main;
            bgButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Show(RectTransform parentTransform, RectTransform helpButtonTransform)
        {
            gameObject.SetActive(true);
            var parentWorldPos = parentTransform.GetWorldPositionOfCenter();
            var parentScreenPos = _mainCamera.WorldToScreenPoint(parentWorldPos);
            if (parentScreenPos.x <= _mainCamera.pixelWidth / 2)
            {
                leftDeco.gameObject.SetActive(true);
                rightDeco.gameObject.SetActive(false);
                contentsRectTransform.MoveToRelatedPosition(helpButtonTransform, PivotPresetType.BottomRight, DescriptionOffset);
                var contentAnchoredPosition = contentsRectTransform.anchoredPosition;
                contentAnchoredPosition.y = 0;
                contentsRectTransform.anchoredPosition = contentAnchoredPosition;
                var leftDecoPosition = leftDeco.position;
                leftDecoPosition.y = helpButtonTransform.position.y;
                leftDeco.position = leftDecoPosition;
            }
            else
            {
                leftDeco.gameObject.SetActive(false);
                rightDeco.gameObject.SetActive(true);
                contentsRectTransform.MoveToRelatedPosition(helpButtonTransform, PivotPresetType.BottomLeft, -DescriptionOffset);
                var contentAnchoredPosition = contentsRectTransform.anchoredPosition;
                contentAnchoredPosition.y = 0;
                contentsRectTransform.anchoredPosition = contentAnchoredPosition;
                var rightDecoPosition = rightDeco.position;
                rightDecoPosition.y = helpButtonTransform.position.y;
                rightDeco.position = rightDecoPosition;
            }
        }
    }
}
