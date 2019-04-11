using System;
using System.Collections;
using Nekoyume.Pattern;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Nekoyume.Game
{
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

        [SerializeField] private ChaseData chaseData;
        [SerializeField] private ShakeData shakeData;

        private Transform _transform;
        private Fsm<State> _fsm;

        private Transform _target;
        private Transform _targetTemp;
        private float _shakeDuration = 0f;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _transform = transform;
            _fsm = new Fsm<State>(this);
            _fsm.RegisterStateCoroutine(State.Idle, CoIdle);
            _fsm.RegisterStateCoroutine(State.ChaseX, CoChaseX);
            _fsm.RegisterStateCoroutine(State.Shake, CoShake);
            _fsm.Run(State.Idle);
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
            _targetTemp = _target;
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
            while (!ReferenceEquals(_target, null))
            {
                var pos = _transform.position;
                var desiredPosX = _target.position.x + chaseData.offsetX;
                var smoothedPosX = Mathf.Lerp(pos.x, desiredPosX, chaseData.smoothSpeed * Time.deltaTime);
                pos.x = smoothedPosX;
                _transform.position = pos;
                
                yield return null;
            }

            _fsm.next = _shakeDuration > 0f ? State.Shake : State.Idle;
        }

        private IEnumerator CoShake()
        {
            var pos = _transform.position;
            
            while (_shakeDuration > 0f)
            {
                var x = Random.Range(-1f, 1f) * shakeData.magnitudeX;
                var y = Random.Range(-1f, 1f) * shakeData.magnitudeY;
                
                _transform.position = new Vector3(pos.x + x, pos.y + y, pos.z);
                
                _shakeDuration -= Time.deltaTime;
                
                yield return null;
            }
            
            _transform.position = pos;

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

        public void SetPoint(float x, float y)
        {
            var pos = _transform.position;
            pos.x = x;
            pos.y = y;
            _transform.position = pos;
        }
    }
}
