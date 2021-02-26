using System;
using UnityEngine;

namespace Nekoyume.Constraints
{
    /// <summary>
    /// 매 프레임 마우스 포인트를 핸들링해서 설정된 캔버스 포지션으로 변환한 값을 만든다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ConstraintsToMousePosition : MonoBehaviour
    {
        [SerializeField] private bool useSpeed = false;
        [SerializeField] private float speed = 1f;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Camera _camera;

        private System.Action _update;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _canvas = _rectTransform.GetComponentInParent<Canvas>();
            if (_canvas is null)
                return;

            switch (_canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    _update = UpdateForScreenSpaceOverlay;
                    UpdateForScreenSpaceOverlay();
                    break;
                case RenderMode.ScreenSpaceCamera:
                    _update = UpdateForScreenSpaceCamera;
                    _camera = _canvas.worldCamera;
                    Constraints(false);
                    break;
                case RenderMode.WorldSpace:
                    _update = UpdateForWorldSpace;
                    _camera = _canvas.worldCamera;
                    Constraints(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            _update();
        }

        private void UpdateForScreenSpaceOverlay()
        {
            _rectTransform.position = Input.mousePosition;
        }

        private void UpdateForScreenSpaceCamera()
        {
            UpdateForWorldSpace();
        }

        private void UpdateForWorldSpace()
        {
            Constraints(useSpeed);
        }

        private void Constraints(bool lerp)
        {
            if (_canvas is null || _camera is null)
                return;

            var position = _rectTransform.position;
            var mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = position.z;
            
            _rectTransform.position = lerp
                ? Vector3.Lerp(position, mousePosition, Time.deltaTime * speed)
                : mousePosition;
        }
    }
}
 