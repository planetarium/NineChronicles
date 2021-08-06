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

        protected ParticleSystem _particlesRoot = null;
        protected virtual float EmitDuration { get; set; } = 1.0f;

        private bool _isPlaying = false;
        private bool _isFinished = false;

        /// <summary>
        /// 해당 필드가 false일 때, 하위 파티클 시스템들의 레이어를 루트 파티클 시스템과 동일하도록 통일시킵니다.
        /// 또한, 하위 파티클 시스템이 루트와 같은 레이어에 있게 하는 동시에
        /// 루트 파티클보다 위에 보이도록 Order in layer를 조절합니다.
        /// 기본값은 true입니다. 이니셜라이징을 원할 경우 인스펙터에서 false로 만들어주어야 합니다.
        /// </summary>
        [SerializeField]
        private bool isLayerInitializedWithChild = true;

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
            _particlesRoot = _particles[0];
            var rootParticleRenderer = _particlesRoot.GetComponent<Renderer>();

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

                if (!isLayerInitializedWithChild && !particle.Equals(_particlesRoot))
                {
                    particle.gameObject.layer = _particlesRoot.gameObject.layer;
                    if (rootParticleRenderer)
                    {
                        // 루트 파티클 시스템과 그 하위의 파티클 시스템의 레이어를 동일하게 맞춰줍니다.
                        // +9는 '같은 레이어' 레벨 안에서 루트보다 위에 보이도록 하고, 다른 레이어를 침범하게 하지 않기 위함입니다.
                        r.sortingLayerName = rootParticleRenderer.sortingLayerName;
                        r.sortingOrder = rootParticleRenderer.sortingOrder + 9;
                    }
                }
            }
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
    }
}
