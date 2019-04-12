using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.Vfx
{
    [RequireComponent(typeof(ParticleSystem))]
    public abstract class Vfx : MonoBehaviour
    {
        private const string StringVfx = "Vfx";

        private ParticleSystem[] _particles = null;
        private int _particlesLength = 0;
        private ParticleSystem _particlesRoot = null;
        private float _particlesDuration = 0f;

        #region Mono

        private void Awake()
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
                
                particle.gameObject.layer = LayerMask.NameToLayer(StringVfx);
                var r = particle.GetComponent<Renderer>();
                if (r)
                {
                    r.sortingLayerName = StringVfx;
                }
            }

            _particlesRoot = _particles[0];
            _particlesRoot.Stop();
            
            Debug.LogWarning($"{name} {_particlesDuration}");
        }

        #endregion

        public void Play(float duration = 0f)
        {
            if (ReferenceEquals(_particlesRoot, null))
            {
                return;
            }

            StartCoroutine(PlayAndAutoInactiveAsync(duration > 0f ? duration : _particlesDuration));
        }

        public void LazyStop()
        {
            _particlesRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(CoLazyStop(_particlesDuration));
        }

        public void Stop()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator PlayAndAutoInactiveAsync(float duration)
        {
            if (!_particlesRoot.isPlaying)
            {
                _particlesRoot.time = 0f;
                _particlesRoot.Play();
            }

            while (duration > 0f)
            {
                duration -= Time.deltaTime;

                if (!gameObject.activeSelf)
                {
                    yield break;
                }

                yield return null;
            }

            LazyStop();
        }

        private IEnumerator CoLazyStop(float delay)
        {
            yield return new WaitForSeconds(delay);
            Stop();
        }
    }
}
