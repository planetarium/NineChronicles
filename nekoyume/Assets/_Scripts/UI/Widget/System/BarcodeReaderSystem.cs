using System;
using System.Collections;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.Native;
using Nekoyume.Permissions;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using ZXing;

namespace Nekoyume.UI
{
    public class BarcodeReaderSystem : Widget
    {
        [SerializeField]
        private CapturedImage capturedImage;

        [SerializeField]
        private RawImage rawCamImage;

        private WebCamTexture _camTexture;
        private IDisposable _disposable;

        private PermissionCallbacks _permissionCallbacks;
        private PermissionState? _cameraPermissionState;
        private bool _shouldRequestPermissionWhenApplicationFocusedIn;

        public override WidgetType WidgetType => WidgetType.System;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

        protected override void Awake()
        {
            base.Awake();
            _permissionCallbacks = new PermissionCallbacks();
            _permissionCallbacks.PermissionDenied += OnPermissionDenied;
            _permissionCallbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedAndDontAskAgain;
            _permissionCallbacks.PermissionGranted += OnPermissionGranted;
        }

        protected override void OnDisable()
        {
            if (_camTexture is { isPlaying: true })
            {
                _camTexture.Stop();
            }

            _disposable?.Dispose();
            _disposable = null;
            base.OnDisable();
        }

        public void Show(Action<Result> onSuccess = null)
        {
            if (IsActive())
            {
                return;
            }

            capturedImage.Show();
            gameObject.SetActive(true);
            StartCoroutine(CoRequestPermission(onSuccess));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public IEnumerator CoReadBarcode(Action<Result> onSuccess = null)
        {
            if (IsActive())
            {
                yield break;
            }

            yield return CoRequestPermission(onSuccess);
            switch (_cameraPermissionState)
            {
                case PermissionState.Granted:
                    yield return CoScanQrCode(onSuccess);
                    break;
                case PermissionState.Denied:
                    OpenNativeSystemSettings();
                    break;
                case PermissionState.DeniedAndDontAskAgain:
                    OpenNativeSystemSettings();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CoRequestPermission(Action<Result> onSuccess = null)
        {
            Debug.Log("[BarcodeReaderSystem] CoRequestPermission start.");
            rawCamImage.gameObject.SetActive(false);
            _shouldRequestPermissionWhenApplicationFocusedIn = false;
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("[BarcodeReaderSystem] Camera permission already granted.");
                _cameraPermissionState = PermissionState.Granted;
            }
            else
            {
                Debug.Log("[BarcodeReaderSystem] Request camera permission.");
                Permission.RequestUserPermission(Permission.Camera, _permissionCallbacks);

                Debug.Log("[BarcodeReaderSystem] Wait for camera permission.");
                yield return new WaitUntil(() => _cameraPermissionState is PermissionState.Granted);
                Debug.Log("[BarcodeReaderSystem] Camera permission granted.");
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
            Debug.Log("[BarcodeReaderSystem] CoScanQrCode start.");
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
                    Debug.Log("[BarcodeReaderSystem] WebCamTexture is not playing. Now start playing and wait for playing.");
                    _camTexture.Play();
                    yield return new WaitUntil(() => _camTexture.isPlaying);
                    Debug.Log("[BarcodeReaderSystem] WebCamTexture is playing.");
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

                        Debug.Log("[BarcodeReaderSystem] QR code detected." +
                                  $" Text: {result.Text}" +
                                  $", Format: {result.BarcodeFormat}");
                        Debug.Log("[BarcodeReaderSystem] CoRequestPermission end.");
                        onSuccess?.Invoke(result);
                    }
                }
                catch (Exception ex)
                {
                    if (_camTexture.isPlaying)
                    {
                        _camTexture.Stop();
                    }

                    Debug.LogError($"[BarcodeReaderSystem] Exception: {ex.Message}");
                    Debug.LogException(ex);
                    // Don't invoke onSuccess? with null. Just try again.
                }

                yield return null;
            }
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.Log($"[BarcodeReaderSystem] OnPermissionDenied: {permission}");
            _cameraPermissionState = PermissionState.Denied;
            OpenNativeSystemSettings();
        }

        private void OnPermissionDeniedAndDontAskAgain(string permission)
        {
            Debug.Log($"[BarcodeReaderSystem] OnPermissionDeniedAndDontAskAgain: {permission}");
            _cameraPermissionState = PermissionState.DeniedAndDontAskAgain;
            OpenNativeSystemSettings();
        }

        private void OnPermissionGranted(string permission)
        {
            Debug.Log($"[BarcodeReaderSystem] OnPermissionGranted: {permission}");
            _cameraPermissionState = PermissionState.Granted;
        }

        private void OpenNativeSystemSettings()
        {
            FindOrCreate<OneButtonSystem>().Show(
                L10nManager.Localize("STC_REQUIRED_CAMERA_PERMISSION_FOR_IMPORT_ACCOUNT_QR_CODE"),
                confirmText: L10nManager.Localize("BTN_OPEN_SYSTEM_SETTINGS"),
                confirmCallback: () =>
                {
                    Debug.Log("[BarcodeReaderSystem] Open system settings.");
                    _shouldRequestPermissionWhenApplicationFocusedIn = true;
                    SystemSettingsOpener.OpenApplicationDetailSettings();
                });
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"[BarcodeReaderSystem] OnApplicationFocus: {hasFocus}");
            if (hasFocus && _shouldRequestPermissionWhenApplicationFocusedIn)
            {
                _shouldRequestPermissionWhenApplicationFocusedIn = false;
                Debug.Log("[BarcodeReaderSystem] Request camera permission.(Retry)");
                Permission.RequestUserPermission(
                    Permission.Camera,
                    _permissionCallbacks);
            }
        }
    }
}
