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
#if !UNITY_IOS
            _camTexture = new WebCamTexture
            {
                requestedHeight = (int)rect.height,
                requestedWidth = (int)rect.width
            };
            rawCamImage.texture = _camTexture;
#endif
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
            rawCamImage.gameObject.SetActive(false);
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission
                    .HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission
                    .RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }

            yield return UnityEngine.Android.Permission
                .HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#elif UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            }
            
            var rect = rawCamImage.rectTransform.rect;
            _camTexture = new WebCamTexture
            {
                requestedHeight = (int)rect.height,
                requestedWidth = (int)rect.width
            };
            rawCamImage.texture = _camTexture;
#endif
            yield return null;
            rawCamImage.gameObject.SetActive(true);
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
