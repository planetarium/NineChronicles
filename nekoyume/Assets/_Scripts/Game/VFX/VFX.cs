using System;
using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class VFX : MonoBehaviour
    {
        private const string StringVFX = "VFX";

        private ParticleSystem[] _particles = null;
        private int _particlesLength = 0;
        private ParticleSystem _particlesRoot = null;
        private float _particlesDuration = 0f;

        protected virtual float EmmitDuration => 1f;

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

        private void OnEnable()
        {
            if (ReferenceEquals(_particlesRoot, null))
            {
                return;
            }

            if (EmmitDuration > 0f)
            {
                StartCoroutine(CoAutoInactive());   
            }
        }

        #endregion

        public void LazyStop()
        {
            _particlesRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(CoLazyStop(_particlesDuration));
        }

        public virtual void Play()
        {
            gameObject.SetActive(true);
        }
        public void Stop()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator CoAutoInactive()
        {
            var duration = 0f;

            while (duration < EmmitDuration)
            {
                duration += Time.deltaTime;

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
