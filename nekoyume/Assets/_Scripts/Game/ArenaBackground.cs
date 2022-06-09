using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Nekoyume.Game
{
    public class ArenaBackground : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _sprite = new List<SpriteRenderer>();
        private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();

        public void Awake()
        {
            var sprites = transform.GetComponentsInChildren<SpriteRenderer>();
            _sprite.AddRange(sprites);

            // var particles = transform.GetComponentsInChildren<ParticleSystem>();
            // _particles.AddRange(particles);
        }

        public void Show(float fadeTime)
        {
            foreach (var sprite in _sprite)
            {
                sprite.DOFade(1.0f, fadeTime);
            }
            //
            // foreach (var particle in _particles)
            // {
            //     particle.Play();
            // }
        }
    }
}
