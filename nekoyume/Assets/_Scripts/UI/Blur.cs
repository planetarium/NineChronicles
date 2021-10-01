using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class Blur : MonoBehaviour
    {
        private static readonly int SizePropertyID = Shader.PropertyToID("_Size");

        private static readonly List<Blur> _blurs = new List<Blur>();
        private static readonly Subject<Unit> _onBlurAdded = new Subject<Unit>();
        private static readonly Subject<Unit> _onBlurRemoved = new Subject<Unit>();

        public Image image;
        public Button button;
        public System.Action onClick;

        private Material _glassOriginal;
        private Material _glass;
        private float _originalBlurSize;

        private readonly List<IDisposable> _disposablesAtEnable = new List<IDisposable>();

        #region override

        private void Awake()
        {
            if (!image ||
                !image.material ||
                !image.material.shader.name.Equals("Custom/TintedUIBlur"))
            {
                return;
            }

            _glassOriginal = image.material;
            _originalBlurSize = _glassOriginal.GetFloat(SizePropertyID);
            _glass = image.material = new Material(_glassOriginal);

            button.OnClickAsObservable()
                .Subscribe(_ => onClick?.Invoke())
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            _blurs.Add(this);
            _onBlurAdded.OnNext(Unit.Default);
            _onBlurAdded.Subscribe(_ => image.enabled = false).AddTo(_disposablesAtEnable);
            _onBlurRemoved.Subscribe(_ =>
            {
                var lastBlur = _blurs.LastOrDefault();
                if (lastBlur &&
                    lastBlur == this &&
                    !image.enabled)
                {
                    image.enabled = true;
                }
            }).AddTo(_disposablesAtEnable);
        }

        private void OnDisable()
        {
            _disposablesAtEnable.DisposeAllAndClear();
            var lastBlur = _blurs.LastOrDefault();
            if (lastBlur)
            {
                _blurs.Remove(lastBlur);
                _onBlurRemoved.OnNext(Unit.Default);
                if (lastBlur != this)
                {
                    Debug.LogWarning("Last Blur object not equals to this object.");
                }
            }

            if (!_glass)
            {
                return;
            }

            _glass.SetFloat(SizePropertyID, _originalBlurSize);
        }

        public virtual void Show(float time = 0.33f)
        {
            gameObject.SetActive(true);
            StartBlur(_originalBlurSize, time);
        }

        public virtual void Show(float size, float time = 0.33f)
        {
            gameObject.SetActive(true);
            StartBlur(size, time);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        #endregion

        public virtual void StartBlur(float size, float time)
        {
            if (!gameObject.activeSelf)
                return;
            StartCoroutine(CoBlur(size, time));
        }

        private IEnumerator CoBlur(float size, float time)
        {
            if (!_glass)
                yield break;

            var from = 0f;
            var to = size;

            _glass.SetFloat(SizePropertyID, from);
            var elapsedTime = 0f;
            while (true)
            {
                var current = Mathf.Lerp(from, to, elapsedTime / time);
                _glass.SetFloat(SizePropertyID, current);

                elapsedTime += Time.deltaTime;
                if (elapsedTime > time ||
                    !gameObject.activeInHierarchy)
                {
                    break;
                }

                yield return null;
            }

            _glass.SetFloat(SizePropertyID, to);
        }
    }
}
