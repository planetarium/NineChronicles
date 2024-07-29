using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game;

namespace Nekoyume.UI
{
    using UniRx;

    public class CapturedImage : MonoBehaviour
    {
        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private RawImage rawImage;

        public System.Action OnClick { get; set; }

        public bool IsShowing => rawImage is { enabled: true };

#region override

        private void Awake()
        {
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick?.Invoke();
            }).AddTo(this);
        }

        public void Show()
        {
            rawImage.texture = CaptureCurrentScreen();
            rawImage.uvRect = ActionCamera.instance.Cam.rect;
            rawImage.enabled = true;
        }

        private void OnDisable()
        {
            var texture = rawImage.texture;
            rawImage.texture = null;
            Destroy(texture);
            rawImage.enabled = false;
        }

        private Texture CaptureCurrentScreen()
        {
            var prevRenderTexture = RenderTexture.active;

            // create render texture
            var cam = ActionCamera.instance.Cam;
            var renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            cam.targetTexture = renderTexture;
            cam.Render();

            // capture to created render texture
            RenderTexture.active = renderTexture;

            var width = cam.targetTexture.width;
            var height = cam.targetTexture.height;
            var captured = new Texture2D(width, height);
            var rect = new Rect(0, 0, width, height);
            captured.ReadPixels(rect, 0, 0);
            captured.Apply();

            RenderTexture.active = prevRenderTexture;
            cam.targetTexture = null;
            renderTexture.Release();

            return captured;
        }

#endregion
    }
}
