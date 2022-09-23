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
    // NOTE: ActionCamera는 처음에 카메라 연출을 위해 작성했는데, 이제는 게임의 메인 카메라 역할을 하고 있다.
    // 이번에는 스크린 해상도 코드가 추가됐는데, 이들을 적절히 분리하는 구조를 고민해보면 좋겠다.
    [RequireComponent(typeof(Camera))]
    public class ActionCamera : MonoSingleton<ActionCamera>
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

        private Fsm<State> _fsm;

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

        protected override void Awake()
        {
            // NOTE: 화면과 카메라가 밀접한 관계에 있고, 카메라 스크립트는 게임 초기화 스크립트인 `Game.Game`과 같은 프레임에 활성화 되니 이곳에서 설정해 본다.
            // Screen.SetResolution(referenceResolution.x, referenceResolution.y,
            //     FullScreenMode.FullScreenWindow);

            base.Awake();

            InitScreenResolution();

            _fsm = new Fsm<State>(this);
            _fsm.RegisterStateCoroutine(State.Idle, CoIdle);
            _fsm.RegisterStateCoroutine(State.ChaseX, CoChaseX);
            _fsm.RegisterStateCoroutine(State.Shake, CoShake);
            _fsm.Run(State.Idle);
        }

        public void RerunFSM() => _fsm.Run(State.Idle);

        private void Update()
        {
            UpdateScreenResolution();
        }

        protected void OnDisable()
        {
            _fsm.Kill();
        }

        protected override void OnDestroy()
        {
            _fsm.Kill();
        }

        #endregion

        #region Fsm

        public void Idle()
        {
            _target = null;
            _targetTemp = null;
            _shakeDuration = 0f;
        }

        public void ChaseX(Transform target)
        {
            _target = target;
            _targetTemp = null;
            _shakeDuration = 0f;
        }

        public void Shake()
        {
            if (_targetTemp is null)
            {
                _targetTemp = _target;
            }

            _target = null;
            _shakeDuration = shakeData.duration;
        }

        private IEnumerator CoIdle()
        {
            while (true)
            {
                if (_target)
                {
                    _fsm.next = State.ChaseX;
                    break;
                }

                if (_shakeDuration > 0f)
                {
                    _fsm.next = State.Shake;
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CoChaseX()
        {
            var data = InPrologue ? prologueChaseData : chaseData;
            while (_target &&
                   _target.gameObject.activeSelf)
            {
                var pos = Transform.position;
                var desiredPosX = _target.position.x + data.offsetX;
                var smoothedPosX = Mathf.Lerp(
                    pos.x,
                    desiredPosX,
                    data.smoothSpeed * Time.deltaTime);
                pos.x = smoothedPosX;
                Transform.position = pos;
                OnTranslate?.Invoke(Transform);

                yield return null;
            }

            _fsm.next = _shakeDuration > 0f
                ? State.Shake
                : State.Idle;
        }

        private IEnumerator CoShake()
        {
            var pos = Transform.position;
            var data = InPrologue ? prologueShakeData : shakeData;

            while (_shakeDuration > 0f)
            {
                var x = Random.Range(-1f, 1f) * data.magnitudeX;
                var y = Random.Range(-1f, 1f) * data.magnitudeY;

                Transform.position = new Vector3(pos.x + x, pos.y + y, pos.z);
                OnTranslate?.Invoke(Transform);

                _shakeDuration -= Time.deltaTime;

                yield return null;
            }

            Transform.position = pos;
            OnTranslate?.Invoke(Transform);

            if (_target)
            {
                _fsm.next = State.ChaseX;
            }
            else if (_targetTemp)
            {
                _target = _targetTemp;
                _targetTemp = null;
                _fsm.next = State.ChaseX;
            }
            else
            {
                _fsm.next = State.Idle;
            }
        }

        #endregion

        #region Position

        public void SetPosition(float x, float y)
        {
            var pos = Transform.position;
            pos.x = x;
            pos.y = y;
            Transform.position = pos;
            OnTranslate?.Invoke(Transform);
        }

        /// <summary>
        /// 스크린의 특정 피봇 위치에 대한 타겟의 위치를 구한다.
        /// </summary>
        /// <param name="targetTransform">위치를 얻고자 하는 타겟의 Transform</param>
        /// <param name="screenPivot">타겟의 스크린에 대한 PivotPresetType</param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(
            Transform targetTransform,
            PivotPresetType screenPivot)
        {
            if (targetTransform is null)
            {
                throw new ArgumentNullException(nameof(targetTransform));
            }

            float2 viewport2;
            switch (screenPivot)
            {
                case PivotPresetType.TopLeft:
                    viewport2 = Float2.ZeroOne;
                    break;
                case PivotPresetType.TopCenter:
                    viewport2 = Float2.HalfOne;
                    break;
                case PivotPresetType.TopRight:
                    viewport2 = Float2.OneOne;
                    break;
                case PivotPresetType.MiddleLeft:
                    viewport2 = Float2.ZeroHalf;
                    break;
                case PivotPresetType.MiddleCenter:
                    viewport2 = Float2.HalfHalf;
                    break;
                case PivotPresetType.MiddleRight:
                    viewport2 = Float2.OneHalf;
                    break;
                case PivotPresetType.BottomLeft:
                    viewport2 = Float2.ZeroZero;
                    break;
                case PivotPresetType.BottomCenter:
                    viewport2 = Float2.HalfZero;
                    break;
                case PivotPresetType.BottomRight:
                    viewport2 = Float2.OneZero;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(screenPivot), screenPivot, null);
            }

            var viewport3 = new float3(
                viewport2.x,
                viewport2.y,
                (targetTransform.position - Transform.position).magnitude);
            return Cam.ViewportToWorldPoint(viewport3);
        }

        /// <summary>
        /// 스크린의 특정 피봇 위치에 대한 타겟의 위치를 구한다. 이때 타겟 스프라이트의 피봇 또한 고려한다.
        /// </summary>
        /// <param name="targetTransform">위치를 얻고자 하는 타겟의 Transform</param>
        /// <param name="screenPivot">타겟 Sprite의 PivotPresetType</param>
        /// <param name="targetSprite">위치를 얻고자자 하는 타겟의 전체 영역을 자치하는 SpriteRenderer</param>
        /// <param name="spritePivot">타겟의 스크린에 대한 PivotPresetType</param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(
            Transform targetTransform,
            PivotPresetType screenPivot,
            Sprite targetSprite,
            PivotPresetType spritePivot
        )
        {
            if (targetTransform is null)
            {
                throw new ArgumentNullException(nameof(targetTransform));
            }

            if (targetSprite is null)
            {
                throw new ArgumentNullException(nameof(targetSprite));
            }

            var position = GetWorldPosition(targetTransform, screenPivot);
            var lossyScale = targetTransform.lossyScale;
            var ppu = targetSprite.pixelsPerUnit;
            var halfSizeAsUnit = new Vector3(
                lossyScale.x * targetSprite.rect.width / ppu / 2f,
                lossyScale.y * targetSprite.rect.height / ppu / 2f);
            Vector3 spritePivotPosition;
            switch (spritePivot)
            {
                case PivotPresetType.TopLeft:
                    spritePivotPosition = new Vector3(-halfSizeAsUnit.x, halfSizeAsUnit.y);
                    break;
                case PivotPresetType.TopCenter:
                    spritePivotPosition = new Vector3(0f, halfSizeAsUnit.y);
                    break;
                case PivotPresetType.TopRight:
                    spritePivotPosition = new Vector3(halfSizeAsUnit.x, halfSizeAsUnit.y);
                    break;
                case PivotPresetType.MiddleLeft:
                    spritePivotPosition = new Vector3(-halfSizeAsUnit.x, 0f);
                    break;
                case PivotPresetType.MiddleCenter:
                    spritePivotPosition = Vector3.zero;
                    break;
                case PivotPresetType.MiddleRight:
                    spritePivotPosition = new Vector3(halfSizeAsUnit.x, 0f);
                    break;
                case PivotPresetType.BottomLeft:
                    spritePivotPosition = new Vector3(-halfSizeAsUnit.x, -halfSizeAsUnit.y);
                    break;
                case PivotPresetType.BottomCenter:
                    spritePivotPosition = new Vector3(0f, -halfSizeAsUnit.y);
                    break;
                case PivotPresetType.BottomRight:
                    spritePivotPosition = new Vector3(halfSizeAsUnit.x, -halfSizeAsUnit.y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spritePivot), spritePivot, null);
            }

            return position - spritePivotPosition;
        }

        #endregion

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
