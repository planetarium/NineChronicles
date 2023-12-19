using System;
using System.Collections;
using Nekoyume.L10n;
using Nekoyume.Native;
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
        private bool _shouldRequestPermissionWhenApplicationFocusedIn;

        private void Awake()
        {
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

            if (_camTexture is { isPlaying: true })
            {
                _camTexture.Stop();
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
            gameObject.SetActive(false);
        }

        private IEnumerator CoRequestPermission(Action<Result> onSuccess = null)
        {
            Debug.Log("[CodeReaderView] CoRequestPermission start.");
            rawCamImage.gameObject.SetActive(false);
            _shouldRequestPermissionWhenApplicationFocusedIn = false;
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
#endif

            rawCamImage.gameObject.SetActive(true);
            StartCoroutine(CoScanQrCode(onSuccess));
            yield return null;
        }

        private IEnumerator CoScanQrCode(Action<Result> onSuccess = null)
        {
            Debug.Log("[CodeReaderView] CoScanQrCode start.");
            var rect = rawCamImage.rectTransform.rect;
            // NOTE: WebCamTexture need to construct after the camera permission granted.
            _camTexture = new WebCamTexture
            {
                requestedHeight = (int)rect.height,
                requestedWidth = (int)rect.width
            };
            rawCamImage.texture = _camTexture;
            var barcodeReader = new BarcodeReader
            {
                Options =
                {
                    PureBarcode = false,
                },
            };
            while (gameObject.activeSelf)
            {
                // NOTE: Why we need to check isPlaying in every frame?
                //       Because the WebCamTexture throws an exception when the camera is not playing.
                //       It may occur by native reasons.
                if (!_camTexture.isPlaying)
                {
                    Debug.Log("[CodeReaderView] WebCamTexture is not playing. Now start playing and wait for playing.");
                    _camTexture.Play();
                    yield return new WaitUntil(() => _camTexture.isPlaying);
                    Debug.Log("[CodeReaderView] WebCamTexture is playing.");
                }

                var localScale = rawCamImage.rectTransform.localScale;
                localScale.y = _camTexture.videoVerticallyMirrored ? -1 : 1;
                rawCamImage.rectTransform.localScale = localScale;

                try
                {
                    var result = barcodeReader.Decode(
                        _camTexture.GetPixels32(),
                        _camTexture.width,
                        _camTexture.height);
                    if (result != null)
                    {
                        if (_camTexture.isPlaying)
                        {
                            _camTexture.Stop();
                        }

                        Debug.Log("[CodeReaderView] QR code detected." +
                                  $" Text: {result.Text}" +
                                  $", Format: {result.BarcodeFormat}");
                        Debug.Log("[CodeReaderView] CoRequestPermission end.");
                        onSuccess?.Invoke(result);
                    }
                }
                catch (Exception ex)
                {
                    if (_camTexture.isPlaying)
                    {
                        _camTexture.Stop();
                    }

                    Debug.LogError($"[CodeReaderView] Exception: {ex.Message}");
                    Debug.LogException(ex);
                    // Don't invoke onSuccess? with null. Just try again.
                }

                yield return null;
            }
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionDenied: {permission}");
            _cameraPermissionState = PermissionState.Denied;
            OpenSystemSettingsAndQuit();
        }

        private void OnPermissionDeniedAndDontAskAgain(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionDeniedAndDontAskAgain: {permission}");
            _cameraPermissionState = PermissionState.DeniedAndDontAskAgain;
            OpenSystemSettingsAndQuit();
        }

        private void OnPermissionGranted(string permission)
        {
            Debug.Log($"[CodeReaderView] OnPermissionGranted: {permission}");
            _cameraPermissionState = PermissionState.Granted;
        }

        private void OpenSystemSettingsAndQuit()
        {
            if (!Widget.TryFind<OneButtonSystem>(out var widget))
            {
                widget = Widget.Create<OneButtonSystem>();
            }

            widget.Show(
                L10nManager.Localize("STC_REQUIRED_CAMERA_PERMISSION_FOR_IMPORT_ACCOUNT_QR_CODE"),
                confirmText: L10nManager.Localize("BTN_OPEN_SYSTEM_SETTINGS"),
                confirmCallback: () =>
                {
                    Debug.Log("[CodeReaderView] Open system settings.");
                    _shouldRequestPermissionWhenApplicationFocusedIn = true;
                    SystemSettingsOpener.OpenApplicationDetailSettings();
                });
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"[CodeReaderView] OnApplicationFocus: {hasFocus}");
            if (hasFocus && _shouldRequestPermissionWhenApplicationFocusedIn)
            {
                _shouldRequestPermissionWhenApplicationFocusedIn = false;
                Debug.Log("[CodeReaderView] Request camera permission.(Retry)");
                Permission.RequestUserPermission(
                    Permission.Camera,
                    _permissionCallbacks);
            }
        }
    }
}
