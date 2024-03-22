#nullable enable

using Nekoyume.Game.Util;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.CameraSystem
{
    public class CameraManager
    {
        private static class Singleton
        {
            internal static readonly CameraManager Value = new();
        }

        public static CameraManager Instance => Singleton.Value;

        private ActionCamera? _mainCamera;

        #region Properties
        public ActionCamera? MainCamera
        {
            set
            {
                _mainCamera = value;
                if (_mainCamera != null)
                {
                    SetMainCanvasCamera(_mainCamera.Cam);
                }
                else
                {
                    SetMainCanvasCamera(null);
                }
            }
            get
            {
                if (_mainCamera != null) return _mainCamera;

                var mainCamera = Camera.main;
                if (mainCamera == null) return null;
                _mainCamera = mainCamera.gameObject.GetOrAddComponent<ActionCamera>();
                SetMainCanvasCamera(mainCamera);

                return _mainCamera;
            }
        }
        #endregion Properties

        private void SetMainCanvasCamera(Camera? camera)
        {
            MainCanvas.instance.Canvas.worldCamera = camera;
        }
    }
}
