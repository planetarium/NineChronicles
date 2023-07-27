using System;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

namespace Nekoyume.UI
{
    using UniRx;
    public class DataMatrixReaderView : MonoBehaviour
    {
        [SerializeField]
        private RawImage rawCamImage;

        private WebCamTexture _camTexture;
        private IDisposable _disposable;

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
        }

        public void Show(Action<Result> onSuccess)
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission
                    .HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission
                    .RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
#endif
            gameObject.SetActive(true);
            _camTexture.Play();
            _disposable = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (_camTexture.isPlaying)
                {
                    try
                    {
                        IBarcodeReader barcodeReader = new BarcodeReader();
                        barcodeReader.Options.PureBarcode = false;
                        var result = barcodeReader.Decode(_camTexture.GetPixels32(), _camTexture.width, _camTexture.height);
                        if (result != null)
                        {
                            onSuccess?.Invoke(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex.Message);
                    }
                }
            });
        }

        public void Close()
        {
            _camTexture.Stop();
            _disposable?.Dispose();
            gameObject.SetActive(false);
        }
    }
}
