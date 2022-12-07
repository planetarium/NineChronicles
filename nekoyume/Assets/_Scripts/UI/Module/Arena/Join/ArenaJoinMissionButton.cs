using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinMissionButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject _progressBarBackgroundGO;

        [SerializeField]
        private GameObject _progressBarGO;

        [SerializeField]
        private RectMask2D _progressRectMask;

        [SerializeField]
        private TextMeshProUGUI _progressText;

        [SerializeField]
        private GameObject _completedObject;

        private float _originalRectWidth;
        private Vector4 _originalProgressRectMaskPadding;

        private void Awake()
        {
            _originalRectWidth = _progressRectMask.rectTransform.rect.width;
            _originalProgressRectMaskPadding = _progressRectMask.padding;
        }

        public void Show((int required, int current) conditions)
        {
            var (required, current) = conditions;
            if (current >= required)
            {
                _progressBarBackgroundGO.SetActive(false);
                _progressBarGO.SetActive(false);
                _completedObject.SetActive(true);
            }
            else
            {
                _originalProgressRectMaskPadding.z = current == 0f
                    ? _originalRectWidth
                    : _originalRectWidth * (1f - (float)current / required);
                _progressRectMask.padding = _originalProgressRectMaskPadding;
                _progressBarBackgroundGO.SetActive(true);
                _progressBarGO.SetActive(true);
                _completedObject.SetActive(false);
            }

            _progressText.text = $"{current}/{required}";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
