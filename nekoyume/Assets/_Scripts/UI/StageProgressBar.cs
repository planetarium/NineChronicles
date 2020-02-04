using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Slider slider = null;
        [SerializeField]
        private RectTransform[] starImages = null;
        private const int MaxWave = 3;
        private int _currentStar = 0;
        private int _currentWaveHpSum = 0;
        private int _progress = 0;

        private Star01VFX _star01VFX = null;
        private Star02VFX _star02VFX = null;
        private Star03VFX _star03VFX = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            _star01VFX.Stop();
            _star01VFX = null;
            _star02VFX.Stop();
            _star02VFX = null;
            _star03VFX.Stop();
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
                    break;
                case 2:
                    starVFX = _star02VFX = VFXController.instance.CreateAndChaseRectTransform<Star02VFX>(starImages[1]);
                    emissionVFX = VFXController.instance.CreateAndChaseRectTransform<StarEmission02VFX>(starImages[1]);
                    break;
                case 3:
                    starVFX = _star03VFX = VFXController.instance.CreateAndChaseRectTransform<Star03VFX>(starImages[2]);
                    emissionVFX = VFXController.instance.CreateAndChaseRectTransform<StarEmission03VFX>(starImages[2]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            starVFX?.Play();
            emissionVFX?.Play();
            slider.value = (float) _currentStar / MaxWave;
        }

        public void SetNextWave(int waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        public void IncreaseProgress(int hp)
        {
            _progress -= hp;
            Debug.LogWarning($"bef : {_progress + hp} / after : {_progress}");
            if(_progress == 0)
            {
                CompleteWave();
                return;
            }
            slider.value = ((float) (_currentWaveHpSum - _progress) / _currentWaveHpSum + _currentStar) / MaxWave;
        }
    }
}
