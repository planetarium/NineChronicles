using System;
using System.Collections;
using Nekoyume.L10n;
using Nekoyume.Permissions;
using UnityEngine;
using UnityEngine.Android;
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

        private PermissionCallbacks _permissionCallbacks;
        private PermissionState? _cameraPermissionState;

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

            _permissionCallbacks = new PermissionCallbacks();
            _permissionCallbacks.PermissionDenied += OnPermissionDenied;
            _permissionCallbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedAndDontAskAgain;
            _permissionCallbacks.PermissionGranted += OnPermissionGranted;
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

        private IEnumerator CoRequestPermission(Action<Result> onSuccess = null)
        {
            Debug.Log("[CodeReaderView] CoRequestPermission start.");
            rawCamImage.gameObject.SetActive(false);
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("[CodeReaderView] Camera permission already granted.");
                _cameraPermissionState = PermissionState.Granted;
            }
            else
            {
                Debug.Log("[CodeReaderView] Request camera permission.");
                Permission.RequestUserPermission(Permission.Camera, _permissionCallbacks);

                Debug.Log("[CodeReaderView] Wait for camera permission.");
                yield return new WaitUntil(() => _cameraPermissionState is PermissionState.Granted);
                Debug.Log("[CodeReaderView] Camera permission granted.");
            }
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
            yield return null;
#endif

            rawCamImage.gameObject.SetActive(true);
            _camTexture.Play();
            _disposable?.Dispose();
            _disposable = Observable.EveryUpdate()
                .Where(_ => _camTexture.isPlaying)
                .Subscribe(_ =>
                {
                    try
                    {
                        IBarcodeReader barcodeReader = new BarcodeReader();
                        barcodeReader.Options.PureBarcode = false;
                        var result = barcodeReader.Decode(
                            _camTexture.GetPixels32(),
                            _camTexture.width,
                            _camTexture.height);
                        if (result != null)
                        {
                            Debug.Log("[CodeReaderView] QR code detected." +
                                      $" Text: {result.Text}" +
                                      $", Format: {result.BarcodeFormat}");
                            Debug.Log("[CodeReaderView] CoRequestPermission end.");
                            onSuccess?.Invoke(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CodeReaderView] Exception: {ex.Message}");
                        Debug.LogException(ex);
                        // Don't invoke onSuccess? with null. Just try again.
                    }
                });
            yield break;
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionDenied: {permission}");
            _cameraPermissionState = PermissionState.Denied;
            RetryOrQuit();
        }

        private void OnPermissionDeniedAndDontAskAgain(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionDeniedAndDontAskAgain: {permission}");
            _cameraPermissionState = PermissionState.DeniedAndDontAskAgain;
            RetryOrQuit();
        }

        private void OnPermissionGranted(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionGranted: {permission}");
            _cameraPermissionState = PermissionState.Granted;
        }

        private void RetryOrQuit()
        {
            if (!Widget.TryFind<TwoButtonSystem>(out var widget))
            {
                widget = Widget.Create<TwoButtonSystem>();
            }

            widget.Show(
                L10nManager.Localize("STC_REQUIRED_CAMERA_PERMISSION_FOR_IMPORT_ACCOUNT_QR_CODE"),
                confirmText: L10nManager.Localize("BTN_RETRY"),
                confirmCallback: () => {
                    Debug.Log("[CodeReaderView] Request camera permission.(Retry)");
                    Permission.RequestUserPermission(
                        Permission.Camera,
                        _permissionCallbacks);
                },
                cancelText: L10nManager.Localize("BTN_QUIT"),
                cancelCallback: () =>
                {
                    Debug.Log("[CodeReaderView] Quit.");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
        }
    }
}
