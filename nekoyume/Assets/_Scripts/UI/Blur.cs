using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
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

        [SerializeField]
        private TouchHandler touchHandler;

        public Image image;
        private Material _glassOriginal;
        private Material _glass;
        private float _originalBlurSize;
        private const float _time = 0.33f;

        private readonly List<IDisposable> _disposablesAtEnable = new List<IDisposable>();

        private Coroutine _coroutine;

        public System.Action OnClick { get; set; }

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

            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick?.Invoke();
            }).AddTo(this);
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

            StartBlur(_originalBlurSize, _time);
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
                    NcDebug.LogWarning("Last Blur object not equals to this object.");
                }
            }

            if (!_glass)
            {
                return;
            }

            _glass.SetFloat(SizePropertyID, _originalBlurSize);
        }

        #endregion

        private void StartBlur(float size, float time)
        {
            if (!gameObject.activeSelf)
                return;

            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            _coroutine = StartCoroutine(CoBlur(size, time));
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
