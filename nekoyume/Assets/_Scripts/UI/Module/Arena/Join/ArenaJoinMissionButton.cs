using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinMissionButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject progressObject;

        [SerializeField]
        private GameObject progressBarBackgroundGO;

        [SerializeField]
        private GameObject progressBarGO;

        [SerializeField]
        private RectMask2D progressRectMask;

        [SerializeField]
        private TextMeshProUGUI progressText;

        [SerializeField]
        private GameObject completedObject;

        private float _originalRectWidth;
        private Vector4 _originalProgressRectMaskPadding;

        private void Awake()
        {
            _originalRectWidth = progressRectMask.rectTransform.rect.width;
            _originalProgressRectMaskPadding = progressRectMask.padding;
        }

        public void Show((int required, int current) conditions)
        {
            var (required, current) = conditions;
            if (current >= required)
            {
                progressObject.SetActive(false);
                progressBarBackgroundGO.SetActive(false);
                progressBarGO.SetActive(false);
                completedObject.SetActive(true);
            }
            else
            {
                progressObject.SetActive(true);
                _originalProgressRectMaskPadding.z = current == 0f
                    ? _originalRectWidth
                    : _originalRectWidth * (1f - (float)current / required);
                progressRectMask.padding = _originalProgressRectMaskPadding;
                progressBarBackgroundGO.SetActive(true);
                progressBarGO.SetActive(true);
                completedObject.SetActive(false);
            }

            progressText.text = $"{current}/{required}";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
