#nullable enable

using System;
using System.Collections;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using Nekoyume.Pattern;
using Unity.Mathematics;
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
            set => _mainCamera = value;
            get
            {
                if (_mainCamera != null) return _mainCamera;

                var mainCamera = Camera.main;
                if (mainCamera == null) return null;
                _mainCamera = mainCamera.gameObject.GetOrAddComponent<ActionCamera>();

                return _mainCamera;
            }
        }
        #endregion Properties
    }
}
