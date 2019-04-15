using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoadingScreen : Widget
    {
        public Image _ui_loading_01;
        
        private Color _color;
        private Sequence _sequence = null;

        private float _alphaToBeginning = 0.5f;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            var pos = transform.localPosition;
            pos.z = -1f;
            transform.localPosition = pos;
            
            if (_ui_loading_01 == null)
            {
                return;
            }

            _color = _ui_loading_01.color;
            _color.a = _alphaToBeginning;
        }

        private void OnEnable()
        {
            if (!_ui_loading_01)
            {
                return;
            }

            _ui_loading_01.color = _color;
            _sequence = DOTween.Sequence()
                .Append(_ui_loading_01.DOFade(1f, 0.3f))
                .Append(_ui_loading_01.DOFade(_alphaToBeginning, 0.6f))
                .SetLoops(-1);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        #endregion
    }
}
