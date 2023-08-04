using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

namespace Nekoyume.UI
{
    using UniRx;
    public class CodeReaderView : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawCamImage;

        [SerializeField]
        private CapturedImage capturedImage;

        private WebCamTexture _camTexture;
        private IDisposable _disposable;
        private Coroutine _coroutine;

        private void Awake()
        {
            var rect = rawCamImage.rectTransform.rect;
            _camTexture = new WebCamTexture
            {
                requestedHeight = (int)rect.height,
                requestedWidth = (int)rect.width
            };
            rawCamImage.texture = _camTexture;
        }

        private void OnDisable()
        {
            _disposable?.Dispose();
            _disposable = null;
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private IEnumerator CoRequestPermission(Action<Result> onSuccess = null)
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission
                    .HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission
                    .RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }

            yield return UnityEngine.Android.Permission
                .HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#endif
            yield return null;
            _camTexture.Play();
            _disposable = Observable.EveryUpdate().Where(_ => _camTexture.isPlaying).Subscribe(_ =>
            {
                try
                {
                    IBarcodeReader barcodeReader = new BarcodeReader();
                    barcodeReader.Options.PureBarcode = false;
                    var result = barcodeReader.Decode(_camTexture.GetPixels32(), _camTexture.width,
                        _camTexture.height);
                    if (result != null)
                    {
                        onSuccess?.Invoke(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
            });
        }

        public void Show(Action<Result> onSuccess = null)
        {
            capturedImage.Show();
            gameObject.SetActive(true);
            _coroutine = StartCoroutine(CoRequestPermission(onSuccess));
        }

        public void Close()
        {
            _camTexture.Stop();
            gameObject.SetActive(false);
        }
    }
}
