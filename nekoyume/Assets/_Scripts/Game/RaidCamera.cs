using System;
using System.Collections;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using Nekoyume.Pattern;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Nekoyume.Game
{
    [RequireComponent(typeof(Camera))]
    public class RaidCamera : MonoBehaviour
    {
        private enum State
        {
            Idle,
            ChaseX,
            Shake,
        }

        [Serializable]
        public struct ChaseData
        {
            public float offsetX;
            public float smoothSpeed;
        }

        [Serializable]
        public struct ShakeData
        {
            public float duration;
            public float magnitudeX;
            public float magnitudeY;
        }

        [Header("Screen Resolution")]
        [SerializeField,
         Tooltip("기준이 될 스크린 해상도를 설정한다.(`CanvasScaler`의 `ReferenceResolution`과 같은 개념)")]
        private int2 referenceResolution = new int2(1136, 640);

        [SerializeField,
         Tooltip(
             "해상도를 조절할 때 가로와 세로 중 어느 쪽을 유지시킬 것인지를 설정한다. 값이 `true`일 때는 가로를, `false`일 때는 세로를 유지한다.(`CanvasScaler`의 `ScreenMathMode`와 `Match`값을 조절하는 것과 같은 개념)")]
        private bool maintainWidth = true;

        [SerializeField, Range(-1f, 1f),
         Tooltip("카메라의 위치를 보정한다. `maintainWidth`가 `true`일 때는 Y축을, `false`일 때는 X축을 보정한다.")]
        private float adaptPosition = 0f;

        [Header("Direction")]
        [SerializeField]
        private ChaseData chaseData = default;

        [SerializeField]
        private ShakeData shakeData = default;

        [Header("Direction For Prologue")]
        [SerializeField]
        private ChaseData prologueChaseData = default;

        [Header("Shake For Prologue")]
        [SerializeField]
        private ShakeData prologueShakeData = default;

        private Transform _transform;
        private Camera _cam;

        private float _defaultAspect;
        private float _defaultOrthographicSizeTimesAspect;
        private float _defaultOrthographicSize;
        private Resolution _resolution;

        private Transform _target;
        private Transform _targetTemp;
        private float _shakeDuration;

        public event Action<Resolution> OnScreenResolutionChange;
        public event Action<Transform> OnTranslate;

        private Transform Transform => _transform
            ? _transform
            : _transform = GetComponent<Transform>();

        public Camera Cam => _cam
            ? _cam
            : _cam = GetComponent<Camera>();

        public bool InPrologue = false;

        #region Mono

        protected void Awake()
        {
            InitScreenResolution();
        }

        private void Update()
        {
            UpdateScreenResolution();
        }

        #endregion

        public void Shake()
        {
            if (_targetTemp is null)
            {
                _targetTemp = _target;
            }

            _target = null;
            _shakeDuration = shakeData.duration;
        }

        #region Screen Resolution

        private void InitScreenResolution()
        {
            _defaultAspect = (float) referenceResolution.x / referenceResolution.y;
            _defaultOrthographicSize = Cam.orthographicSize;
            _defaultOrthographicSizeTimesAspect = _defaultOrthographicSize * GetCameraAspect();
            UpdateScreenResolution();
        }

        private float GetCameraAspect()
        {
            return maintainWidth
                ? Math.Min(Cam.aspect, _defaultAspect)
                : Math.Max(Cam.aspect, _defaultAspect);
        }

        private void UpdateScreenResolution()
        {
            if (Screen.currentResolution.Equals(_resolution))
            {
                return;
            }

            _resolution = Screen.currentResolution;

            if (maintainWidth)
            {
                Cam.orthographicSize = _defaultOrthographicSizeTimesAspect / GetCameraAspect();

                var position = Transform.position;
                var y = (_defaultOrthographicSize - Cam.orthographicSize) * adaptPosition;
                Transform.position = new Vector3(
                    position.x,
                    y,
                    position.z);
            }
            else
            {
                var position = Transform.position;
                var x = (_defaultOrthographicSizeTimesAspect -
                         Cam.orthographicSize * GetCameraAspect()) * adaptPosition;
                Transform.position = new Vector3(
                    x,
                    position.y,
                    position.z);
            }

            OnScreenResolutionChange?.Invoke(_resolution);
            OnTranslate?.Invoke(Transform);
        }

        #endregion
    }
}
