using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageProgressBar : MonoBehaviour
    {
        private const int MaxWave = 3;

        [SerializeField]
        private Slider slider = null;
        [SerializeField]
        private RectTransform[] starImages = null;
        [SerializeField]
        private Image[] activatedStarImages = null;
        [SerializeField]
        private RectTransform vfxOffset;
        [SerializeField]
        private float smoothenSpeed = 2.0f;
        private float _xLength = 0;

        private int _currentStar = 0;
        private int _currentWaveHpSum = 0;
        private int _progress = 0;

        private StageProgressBarVFX _stageProgressBarVFX = null;
        private Star01VFX _star01VFX = null;
        private Star02VFX _star02VFX = null;
        private Star03VFX _star03VFX = null;

        private Coroutine _smoothenCoroutine = null;

        private void Awake()
        {
            _xLength = slider.GetComponent<RectTransform>().rect.width;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
                _smoothenCoroutine = null;
            }
            activatedStarImages[0].enabled = false;
            activatedStarImages[1].enabled = false;
            activatedStarImages[2].enabled = false;
            _stageProgressBarVFX?.Stop();
            _stageProgressBarVFX = null;
            _star01VFX?.Stop();
            _star01VFX = null;
            _star02VFX?.Stop();
            _star02VFX = null;
            _star03VFX?.Stop();
            _star03VFX = null;
            gameObject.SetActive(false);
        }

        public void Initialize()
        {
            _currentStar = 0;
            slider.value = 0.0f;
        }

        public void CompleteWave()
        {
            ++_currentStar;

            VFX starVFX = null;
            VFX emissionVFX = null;

            switch(_currentStar)
            {
                case 1:
                    starVFX = _star01VFX = VFXController.instance.CreateAndChaseRectTransform<Star01VFX>(starImages[0]);
                    emissionVFX = VFXController.instance.CreateAndChaseRectTransform<StarEmission01VFX>(starImages[0]);
                    activatedStarImages[0].enabled = true;
                    break;
                case 2:
                    starVFX = _star02VFX = VFXController.instance.CreateAndChaseRectTransform<Star02VFX>(starImages[1]);
                    emissionVFX = VFXController.instance.CreateAndChaseRectTransform<StarEmission02VFX>(starImages[1]);
                    activatedStarImages[1].enabled = true;
                    break;
                case 3:
                    starVFX = _star03VFX = VFXController.instance.CreateAndChaseRectTransform<Star03VFX>(starImages[2]);
                    emissionVFX = VFXController.instance.CreateAndChaseRectTransform<StarEmission03VFX>(starImages[2]);
                    activatedStarImages[2].enabled = true;
                    break;
                default:
                    return;
            }

            starVFX?.Play();
            emissionVFX?.Play();

            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
            }
            _smoothenCoroutine = StartCoroutine(LerpProgressBar((float)_currentStar / MaxWave));
        }

        public void SetNextWave(int waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        public void IncreaseProgress(int hp)
        {
            if(_stageProgressBarVFX is null)
            {
                _stageProgressBarVFX = VFXController.instance.CreateAndChaseRectTransform<StageProgressBarVFX>(vfxOffset);
                _stageProgressBarVFX.Play();
            }

            _progress -= hp;
            if (_progress == 0)
            {
                CompleteWave();
                return;
            }

            var sliderValue = GetProgress(_progress);

            if (!(_smoothenCoroutine is null))
            {
                slider.value = GetProgress(_progress + hp);
                StopCoroutine(_smoothenCoroutine);
            }
            _smoothenCoroutine = StartCoroutine(LerpProgressBar(sliderValue));
        }

        private IEnumerator LerpProgressBar(float value)
        {
            float current = slider.value;
            float t = 0;
            float duration = (value - current) / (Time.deltaTime * smoothenSpeed);

            while (t < duration)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * smoothenSpeed);
                slider.value = current;
                vfxOffset.anchoredPosition = new Vector2(current * _xLength, vfxOffset.anchoredPosition.y);
                t += Time.deltaTime;
                yield return null;
            }

            _smoothenCoroutine = null;
        }

        private float GetProgress(float progress)
        {
            return ((_currentWaveHpSum - progress) / _currentWaveHpSum + _currentStar) / MaxWave;
        }
    }
}
