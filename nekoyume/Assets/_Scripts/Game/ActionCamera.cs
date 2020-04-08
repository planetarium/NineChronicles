using System;
using System.Collections;
using Nekoyume.EnumType;
using Nekoyume.Pattern;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Nekoyume.Game
{
    [DefaultExecutionOrder(300)]
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
        [SerializeField]
        private int screenWidth = 1136;

        [SerializeField]
        private int screenHeight = 640;

        [SerializeField]
        private bool maintainWidth = true;

        [SerializeField, Range(-1f, 1f)]
        private float adaptPosition = 0f;

        [Header("Direction")]
        [SerializeField]
        private ChaseData chaseData;

        [SerializeField]
        private ShakeData shakeData;

        private float _defaultAspect;
        private float _defaultOrthographicSizeTimesAspect;
        private float _defaultOrthographicSize;
        private Resolution _resolution;

        private Fsm<State> _fsm;

        private Transform _target;
        private Transform _targetTemp;
        private float _shakeDuration;

        public readonly Subject<Resolution> OnResolutionChange = new Subject<Resolution>();

        private Transform Transform { get; set; }
        public Camera Cam { get; private set; }

        #region Mono

        protected override void Awake()
        {
            // NOTE: 화면과 카메라가 밀접한 관계에 있고, 카메라 스크립트는 게임 초기화 스크립트인 `Game.Game`과 같은 프레임에 활성화 되니 이곳에서 설정해 본다.
            Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.FullScreenWindow);

            base.Awake();

            Transform = transform;
            Cam = GetComponent<Camera>();

            InitScreenResolution();

            _fsm = new Fsm<State>(this);
            _fsm.RegisterStateCoroutine(State.Idle, CoIdle);
            _fsm.RegisterStateCoroutine(State.ChaseX, CoChaseX);
            _fsm.RegisterStateCoroutine(State.Shake, CoShake);
            _fsm.Run(State.Idle);
        }

        private void Update()
        {
            UpdateScreenResolution();
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
                if (!ReferenceEquals(_target, null))
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
            while (_target != null &&
                   _target.gameObject.activeSelf)
            {
                var pos = Transform.position;
                var desiredPosX = _target.position.x + chaseData.offsetX;
                var smoothedPosX = Mathf.Lerp(pos.x, desiredPosX,
                    chaseData.smoothSpeed * Time.deltaTime);
                pos.x = smoothedPosX;
                Transform.position = pos;

                yield return null;
            }

            _fsm.next = _shakeDuration > 0f ? State.Shake : State.Idle;
        }

        private IEnumerator CoShake()
        {
            var pos = Transform.position;

            while (_shakeDuration > 0f)
            {
                var x = Random.Range(-1f, 1f) * shakeData.magnitudeX;
                var y = Random.Range(-1f, 1f) * shakeData.magnitudeY;

                Transform.position = new Vector3(pos.x + x, pos.y + y, pos.z);

                _shakeDuration -= Time.deltaTime;

                yield return null;
            }

            Transform.position = pos;

            if (!ReferenceEquals(_target, null))
            {
                _fsm.next = State.ChaseX;
            }
            else if (!ReferenceEquals(_targetTemp, null))
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

        public void SetPosition(float x, float y)
        {
            var pos = Transform.position;
            pos.x = x;
            pos.y = y;
            Transform.position = pos;
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

            Vector3 screenPoint;
            switch (screenPivot)
            {
                case PivotPresetType.TopLeft:
                    screenPoint = new Vector3(0f, 1f);
                    break;
                case PivotPresetType.TopCenter:
                    screenPoint = new Vector3(0.5f, 1f);
                    break;
                case PivotPresetType.TopRight:
                    screenPoint = new Vector3(1f, 1f);
                    break;
                case PivotPresetType.MiddleLeft:
                    screenPoint = new Vector3(0f, 0.5f);
                    break;
                case PivotPresetType.MiddleCenter:
                    screenPoint = new Vector3(0.5f, 0.5f);
                    break;
                case PivotPresetType.MiddleRight:
                    screenPoint = new Vector3(1f, 0.5f);
                    break;
                case PivotPresetType.BottomLeft:
                    screenPoint = new Vector3(0f, 0f);
                    break;
                case PivotPresetType.BottomCenter:
                    screenPoint = new Vector3(0.5f, 0f);
                    break;
                case PivotPresetType.BottomRight:
                    screenPoint = new Vector3(1f, 0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(screenPivot), screenPivot, null);
            }

            screenPoint.z = (targetTransform.position - Transform.position).magnitude;
            return Cam.ViewportToWorldPoint(screenPoint);
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

        private void InitScreenResolution()
        {
            _defaultAspect = (float) screenWidth / screenHeight;
            _defaultOrthographicSize = Cam.orthographicSize;
            _defaultOrthographicSizeTimesAspect = _defaultOrthographicSize * GetAspect();
            UpdateScreenResolution();
        }

        private float GetAspect()
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
            OnResolutionChange.OnNext(_resolution);

            if (maintainWidth)
            {
                Cam.orthographicSize = _defaultOrthographicSizeTimesAspect / GetAspect();

                var position = Transform.position;
                Transform.position = new Vector3(
                    position.x,
                    adaptPosition * (_defaultOrthographicSize - Cam.orthographicSize),
                    position.z);
            }
            else
            {
                var position = Transform.position;
                Transform.position = new Vector3(
                    adaptPosition * (_defaultOrthographicSizeTimesAspect -
                                     Cam.orthographicSize * GetAspect()),
                    position.y,
                    position.z);
            }
        }
    }
}
