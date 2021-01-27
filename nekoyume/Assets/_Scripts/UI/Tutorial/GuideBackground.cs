using Coffee.UISoftMask;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideBackground : MonoBehaviour
    {
        [SerializeField] private SoftMask mask;
        [SerializeField] private Image background;

        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private float alpha = 0.8f;

        private void SetFade(bool isIn, float duration, System.Action action = null)
        {
            background.DOKill();
            var color = background.color;
            color.a = isIn ? 0 : alpha;
            background.color = color;
            background.DOFade(isIn ? alpha : 0.0f, duration).OnComplete(() => action?.Invoke());
        }

        private void SetMask(bool isEnable, Vector2 position)
        {
            mask.alpha = isEnable ? alpha : 0.0f;
            if (isEnable)
            {
                mask.rectTransform.anchoredPosition  = position;
            }
        }

        public void Play(GuideBackgroundData data)
        {
            SetMask(data.IsEnableMask, data.Target);
            SetFade(true, data.IsExistFadeIn ? fadeDuration : 0.0f, data.Callback);
        }

        public void Stop()
        {
            SetFade(false, fadeDuration);
        }
    }
}
