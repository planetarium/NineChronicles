using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI
{
    public class Blur : Widget
    {
        private static readonly int RadiusPropertyID = Shader.PropertyToID("_Radius");

        public Button button;
        public System.Action onClick;

        private static Material _glass;
        private Material _glassOriginal;
        private float _originalBlurRadius;

        #region override

        protected override void Awake()
        {
            base.Awake();
            FindGlassMaterial(gameObject);
            button.OnClickAsObservable()
                .Subscribe(_ => onClick?.Invoke())
                .AddTo(gameObject);
        }

        public virtual void Show(float time = 0.33f)
        {
            base.Show();
            StartBlur(_originalBlurRadius, time);
        }

        public virtual void Show(float radius, float time = 0.33f)
        {
            base.Show();
            StartBlur(radius, time);
        }

        public void Close()
        {
            base.Close();
        }

        #endregion

        public virtual void StartBlur(float radius, float time)
        {
            StartCoroutine(CoBlur(radius, time));
        }

        private void FindGlassMaterial(GameObject go)
        {
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (!image ||
                !image.material ||
                !image.material.shader.name.Equals("UI/Unlit/FrostedGlass"))
                return;

            _glassOriginal = image.material;
            _originalBlurRadius = _glassOriginal.GetFloat(RadiusPropertyID);
            _glass
                = image.material
                = new Material(_glassOriginal);
        }

        private IEnumerator CoBlur(float radius, float time)
        {
            if (!_glass)
                yield break;

            var from = 0f;
            var to = radius;

            _glass.SetFloat(RadiusPropertyID, from);
            var elapsedTime = 0f;
            while (true)
            {
                var current = Mathf.Lerp(from, to, elapsedTime);
                _glass.SetFloat(RadiusPropertyID, current);

                elapsedTime += Time.deltaTime;
                if (elapsedTime > time ||
                    !gameObject.activeInHierarchy)
                {
                    break;
                }
                yield return null;
            }

            _glass.SetFloat(RadiusPropertyID, to);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!_glass)
                return;
            _glass.SetFloat(RadiusPropertyID, _originalBlurRadius);
        }
    }
}
