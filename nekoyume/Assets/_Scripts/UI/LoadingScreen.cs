using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoadingScreen : Widget
    {
        public Image loadingImage;
        public Text loadingText;

        private Color _color;
        private Sequence[] _sequences;

        private const float AlphaToBeginning = 0.5f;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            var pos = transform.localPosition;
            pos.z = -1f;
            transform.localPosition = pos;

            if (ReferenceEquals(loadingText, null) || ReferenceEquals(loadingImage, null))
            {
                throw new SerializeFieldNullException();
            }

            _color = loadingText.color;
            _color.a = AlphaToBeginning;
        }

        private void OnEnable()
        {
            loadingImage.color = _color;
            _sequences = new[]
            {
                DOTween.Sequence()
                    .Append(loadingImage.DOFade(1f, 0.3f))
                    .Append(loadingImage.DOFade(AlphaToBeginning, 0.6f))
                    .SetLoops(-1),
                DOTween.Sequence()
                    .Append(loadingText.DOFade(1f, 0.3f))
                    .Append(loadingText.DOFade(AlphaToBeginning, 0.6f))
                    .SetLoops(-1)
            };
        }

        private void OnDisable()
        {
            foreach (var sequence in _sequences)
            {
                sequence.Kill();
            }
            _sequences = null;
        }

        #endregion
    }
}
