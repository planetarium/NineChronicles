using System.Collections;
using System.Text;
using UnityEngine;

namespace Nekoyume.Game.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public abstract class VfxBase : MonoBehaviour
    {
        private const string StringVFX = "VFX";
        
        private ParticleSystem[] _particles = null;
        private int _particlesLength = 0;
        private ParticleSystem _particlesRoot = null;
        private float _particlesDuration = 0f;

        // Mono

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>();
            _particlesLength = _particles.Length;
            if (_particlesLength == 0)
            {
                return;
            }

            for (int i = 0; i < _particlesLength; i++)
            {
                _particles[i].gameObject.layer = LayerMask.NameToLayer(StringVFX);
                var r = _particles[i].GetComponent<Renderer>();
                if (r)
                {
                    r.sortingLayerName = StringVFX;
                }
            }

            _particlesRoot = _particles[0];
            _particlesRoot.Stop();
            _particlesDuration = _particlesRoot.main.duration;
        }

        // ~Mono

        public void Play()
        {
            if (_particlesRoot == null ||
                _particlesRoot.isPlaying)
            {
                return;
            }

            StartCoroutine(nameof(PlayAndAutoInactiveAsync));
        }

        private IEnumerator PlayAndAutoInactiveAsync()
        {
            _particlesRoot.time = 0f;
            _particlesRoot.Play();

            yield return new WaitForSeconds(_particlesDuration);

            gameObject.SetActive(false);
        }
    }
}