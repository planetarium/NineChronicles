using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Nekoyume.Game.VFX
{
    /// <summary>
    /// This object is used by VFXController only. Do not use directly.
    /// </summary>
    public class VFX : MonoBehaviour
    {
        private const string StringVFX = "VFX";

        private ParticleSystem[] _particles = null;
        private int _particlesLength = 0;
        private float _particlesDuration = 0f;

        private ParticleSystem _particleRoot;

        protected virtual ParticleSystem ParticlesRoot
        {
            get => _particleRoot;
            set
            {
                _particleRoot = value;
                _rootRenderer = ParticlesRoot.GetComponent<Renderer>();
            }
        }

        private Renderer _rootRenderer;

        protected virtual float EmitDuration  { get; set; } = 1.0f;

        protected bool _isPlaying = false;
        protected bool _isFinished = false;

        [SerializeField]
        private bool _initializeSortingProbsOfChildren;

        /// <summary>
        /// VFX 재생이 성공적으로 완료되었을 때 호출되는 콜백
        /// </summary>
        public System.Action OnFinished = null;
        /// <summary>
        /// VFX 재생이 성공적으로 완료되고 비활성화 될 때 호출되는 콜백
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
            Assert.Greater(_particlesLength, 0);
            ParticlesRoot = _particles[0];

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

                if (_initializeSortingProbsOfChildren && !particle.Equals(ParticlesRoot))
                {
                    InitializeSortingProbsOfChildren(particle, r);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (ReferenceEquals(ParticlesRoot, null))
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

            transform.FlipX(false);
        }

        #endregion

        public virtual void LazyStop()
        {
            ParticlesRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (gameObject.activeSelf)
            {
                StartCoroutine(CoLazyStop(_particlesDuration));
            }
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
            if (_isFinished)
            {
                OnTerminated?.Invoke();
            }
            yield return new WaitForSeconds(delay);
            Stop();
        }

        private void InitializeSortingProbsOfChildren(ParticleSystem particle, Renderer r)
        {
            particle.gameObject.layer = ParticlesRoot.gameObject.layer;
            if (_rootRenderer)
            {
                r.sortingLayerName = _rootRenderer.sortingLayerName;
                r.sortingOrder = _rootRenderer.sortingOrder + 9;
            }
        }
    }
}
