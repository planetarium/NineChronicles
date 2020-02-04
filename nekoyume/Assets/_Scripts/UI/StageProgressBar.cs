using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Slider slider = null;
        [SerializeField]
        private Image[] starImages = null;
        private int _maxWave = 3;
        private int _currentStar = 0;
        private int _currentWaveHpSum;
        private int _progress;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Initialize(int wave)
        {
            _currentStar = 0;
            slider.value = 0.0f;
            _maxWave = wave;
        }

        public void CompleteWave()
        {
            ++_currentStar;
            Debug.LogWarning("completeWave");
            slider.value = (float) _currentStar / _maxWave;
        }

        public void SetNextWave(int waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        public void IncreaseProgress(int hp)
        {
            _progress -= hp;
            if(_progress == 0)
            {
                CompleteWave();
                return;
            }
            slider.value = ((float) (_currentWaveHpSum - _progress) / _currentWaveHpSum + _currentStar) / _maxWave;
        }
    }
}
