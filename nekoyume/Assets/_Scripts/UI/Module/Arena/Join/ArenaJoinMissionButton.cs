using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinMissionButton : MonoBehaviour
    {
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

        public void SetConditions((int max, int current) conditions)
        {
            var (max, current) = conditions;
            if (current == max)
            {
                _completedObject.SetActive(true);
                _originalProgressRectMaskPadding.z = 0f;
                _progressRectMask.padding = _originalProgressRectMaskPadding;
            }
            else
            {
                _completedObject.SetActive(false);
                _originalProgressRectMaskPadding.z = current == 0f
                    ? _originalRectWidth
                    : _originalRectWidth * (1f - (float)current / max);
                _progressRectMask.padding = _originalProgressRectMaskPadding;
            }

            _progressText.text = $"{current}/{max}";
        }
    }
}
