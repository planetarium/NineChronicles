using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI
{
    public class Blur : MonoBehaviour
    {
        private static readonly int RadiusPropertyID = Shader.PropertyToID("_Radius");
        private static readonly int ScreenWidthPropertyID = Shader.PropertyToID("_ScreenWidth");

        public Image image;
        public Button button;
        public System.Action onClick;

        private Material _glassOriginal;
        private Material _glass;
        private float _originalBlurRadius;

        #region override

        protected void Awake()
        {
            FindGlassMaterial(gameObject);
            button.OnClickAsObservable()
                .Subscribe(_ => onClick?.Invoke())
                .AddTo(gameObject);
        }

        public virtual void Show(float time = 0.33f)
        {
            gameObject.SetActive(true);
            StartBlur(_originalBlurRadius, time);
        }

        public virtual void Show(float radius, float time = 0.33f)
        {
            gameObject.SetActive(true);
            StartBlur(radius, time);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        #endregion

        public virtual void StartBlur(float radius, float time)
        {
            if (!gameObject.activeSelf)
                return;
            StartCoroutine(CoBlur(radius, time));
        }

        private void FindGlassMaterial(GameObject go)
        {
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
            _glass.SetFloat(ScreenWidthPropertyID, Screen.width);
            var elapsedTime = 0f;
            while (true)
            {
                var current = Mathf.Lerp(from, to, elapsedTime / time);
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

        protected void OnDisable()
        {
            if (!_glass)
                return;
            _glass.SetFloat(RadiusPropertyID, _originalBlurRadius);
        }
    }
}
