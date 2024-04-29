using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Battle
{
    public class BackgroundGroup : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> sprites;
        [SerializeField] private ParticleSystem[] particles;
        [SerializeField] private Background[] backgrounds;

        private float _currentFadeValue = 1f;
        private float _targetFadeValue = 1f;
        private float _fadeDuration;
        private float _fadeTime;

        private void Awake()
        {
            foreach (var background in backgrounds)
            {
                background.Initialize();
                sprites.AddRange(background.GetComponentsInChildren<SpriteRenderer>(true));
            }
        }

        private void Update()
        {
            if (_fadeTime > _fadeDuration)
            {
                return;
            }
            _fadeTime += Time.deltaTime;
            UpdateFade();
        }

        private void UpdateFade()
        {
            if (Mathf.Approximately(_currentFadeValue, _targetFadeValue))
            {
                return;
            }

            _currentFadeValue = Mathf.Lerp(_currentFadeValue, _targetFadeValue, _fadeTime / _fadeDuration);
            SetBackgroundAlpha(_currentFadeValue);
        }

        public void FadeOut(float duration)
        {
            _fadeDuration = duration;
            _fadeTime = 0;
            _targetFadeValue = 0;

            foreach (var particle in particles)
            {
                particle.Stop();
            }
        }

        public void FadeIn(float duration)
        {
            _fadeDuration = duration;
            _fadeTime = 0;
            _targetFadeValue = 1;

            foreach (var particle in particles)
            {
                particle.Play();
            }
        }

        public void SetBackgroundAlpha(float alpha)
        {
            _currentFadeValue = alpha;
            foreach (var background in sprites)
            {
                if (background == null)
                {
                    continue;
                }

                var color = background.color;
                color.a = alpha;
                background.color = color;
            }
        }

        [ContextMenu("GetResources")]
        public void GetResources()
        {
            sprites = GetComponentsInChildren<SpriteRenderer>(true).ToList();
            particles = GetComponentsInChildren<ParticleSystem>(true);
            backgrounds = GetComponentsInChildren<Background>(true);

            // background에 속한 sprite는 동적으로 변경되므로 제외
            foreach (var background in backgrounds)
            {
                foreach (var spriteInBg in background.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sprites.Remove(spriteInBg);
                }
            }
        }
    }
}
