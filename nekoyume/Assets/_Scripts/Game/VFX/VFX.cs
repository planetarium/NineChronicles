using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.VFX
{
    /// <summary>
    /// This object is used by VFXController only. Do not use directly.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class VFX : MonoBehaviour
    {
        private const string StringVFX = "VFX";

        private ParticleSystem[] _particles = null;
        private int _particlesLength = 0;
        private float _particlesDuration = 0f;

        protected ParticleSystem _particlesRoot = null;
        protected virtual float EmitDuration => 1f;

        private bool _isPlaying = false;
        private bool _isFinished = false;

        /// <summary>
        /// VFX 재생이 성공적으로 완료되었을 때 호출되는 콜백
        /// </summary>
        public System.Action OnFinished = null;
        /// <summary>
        /// VFX 재생이 성공적으로 완료되고 비활성화 되었을 때 호출되는 콜백
        /// </summary>
        public System.Action OnTerminated = null;
        /// <summary>
        /// VFX 재생 도중 비활성화되었을 때 호출되는 콜백
        /// </summary>
        public System.Action OnInterrupted = null;

        #region Mono

        public virtual void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>();
            _particlesLength = _particles.Length;
            if (_particlesLength == 0)
            {
                return;
            }

            foreach (var particle in _particles)
            {
                if (particle.main.duration > _particlesDuration)
                {
                    _particlesDuration = particle.main.duration;
                }

                if (particle.gameObject.layer == LayerMask.NameToLayer("Default"))
                    particle.gameObject.layer = LayerMask.NameToLayer(StringVFX);
                var r = particle.GetComponent<Renderer>();
                if (r && r.sortingLayerName == "Default")
                {
                    r.sortingLayerName = StringVFX;
                }
            }

            _particlesRoot = _particles[0];
        }

        protected virtual void OnEnable()
        {
            if (ReferenceEquals(_particlesRoot, null))
            {
                return;
            }

            if (EmitDuration > 0f)
            {
                StartCoroutine(CoAutoInactive());   
            }
        }

        protected virtual void OnDisable()
        {
            if (_isPlaying)
                OnInterrupted?.Invoke();
        }

        #endregion

        public void LazyStop()
        {
            _particlesRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(CoLazyStop(_particlesDuration));
        }

        public virtual void Play()
        {
            _isFinished = false;
            gameObject.SetActive(true);
        }

        public virtual void Stop()
        {
            gameObject.SetActive(false);
            _isPlaying = false;
            if (_isFinished)
            {
                OnTerminated?.Invoke();
            }
        }

        private IEnumerator CoAutoInactive()
        {
            _isPlaying = true;
            var duration = 0f;

            while (duration < EmitDuration)
            {
                duration += Time.deltaTime;

                if (!gameObject.activeSelf)
                {
                    yield break;
                }

                yield return null;
            }

            _isFinished = true;
            OnFinished?.Invoke();
            LazyStop();
        }

        private IEnumerator CoLazyStop(float delay)
        {
            yield return new WaitForSeconds(delay);
            Stop();
        }
    }
}
