using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using UniRx;
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
        private GameObject[] activatedObjects = null;
        [SerializeField]
        private RectTransform vfxClamper = null;
        [SerializeField]
        private RectTransform vfxOffset = null;
        [SerializeField]
        private float smoothenSpeed = 2.0f;
        [SerializeField]
        private float smoothenFinishThreshold = 0.005f;
        private float _xLength = 0;

        private readonly ReactiveProperty<int> _currentStar = new ReactiveProperty<int>(0);
        private long _currentWaveHpSum = 0;
        private long _progress = 0;
        private bool _vfxEnabled;

        [SerializeField] private StageProgressBarVFX stageProgressBarVFX = null;
        [SerializeField] private List<VFX> starVFXList = null;
        [SerializeField] private List<VFX> starEmissionVFXList = null;

        private Coroutine _smoothenCoroutine = null;

        private void Awake()
        {
            _xLength = vfxClamper.rect.width;
            Game.Event.OnEnemyDeadStart.AddListener(OnEnemyDeadStart);
            Game.Event.OnWaveStart.AddListener(SetNextWave);
        }

        public void Show()
        {
            Clear();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
                _smoothenCoroutine = null;
            }

            Clear();
            gameObject.SetActive(false);
        }

        public void Initialize(bool vfxEnabled)
        {
            _vfxEnabled = vfxEnabled;

            _currentStar.Subscribe(star =>
            {
                if (star > 0)
                    AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);

                PlayVFX(star);
            }).AddTo(gameObject);
        }

        public void SetStarProgress(int star)
        {
            _currentStar.Value = star;
            var lerpSpeed = _currentStar.Value == 3 ? 0.5f : 1.0f;

            if (!(_smoothenCoroutine is null))
            {
                TerminateCurrentSmoothen(false);
            }

            var sliderValue = (float) _currentStar.Value / MaxWave;
            if (isActiveAndEnabled)
            {
                _smoothenCoroutine = StartCoroutine(LerpProgressBar(sliderValue, lerpSpeed));
            }
            else
            {
                UpdateSliderValue(sliderValue);
                for (int i = 0; i < star; ++i)
                {
                    activatedObjects[i].SetActive(true);
                }
            }
        }

        private void CompleteWave()
        {
            SetStarProgress(_currentStar.Value + 1);
        }

        private void SetNextWave(long waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        private void IncreaseProgress(long hp)
        {
            if (_vfxEnabled)
            {
                stageProgressBarVFX.Play();
            }

            _progress -= hp;
            if (_progress <= 0)
            {
                CompleteWave();
                return;
            }

            var sliderValue = GetProgress(_progress);

            slider.value = GetProgress(_progress + hp);
            TerminateCurrentSmoothen();
            if (isActiveAndEnabled)
            {
                _smoothenCoroutine = StartCoroutine(LerpProgressBar(sliderValue));
            }
            else
            {
                UpdateSliderValue(sliderValue);
            }
        }

        private IEnumerator LerpProgressBar(float value, float additionalSpeed = 1.0f)
        {
            var current = slider.value;
            var speed = smoothenSpeed * additionalSpeed;

            while (current < value - smoothenFinishThreshold)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * speed);
                vfxOffset.anchoredPosition = new Vector2(current * _xLength, vfxOffset.anchoredPosition.y);
                slider.value = current;
                yield return null;
            }

            UpdateSliderValue(value);
        }

        private float GetProgress(float progress)
        {
            return ((_currentWaveHpSum - progress) / _currentWaveHpSum + _currentStar.Value) / MaxWave;
        }

        private void TerminateCurrentSmoothen(bool callTerminated = true)
        {
            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
            }
            _smoothenCoroutine = null;
        }

        private void OnEnemyDeadStart(StageMonster stageMonster)
        {
            IncreaseProgress(stageMonster.Hp - stageMonster.CharacterModel.AdditionalHP);
        }

        public void OnValueChanged()
        {
            stageProgressBarVFX.transform.position = vfxOffset.transform.position;
        }

        private void UpdateSliderValue(float value)
        {
            vfxOffset.anchoredPosition = new Vector2(value * _xLength, vfxOffset.anchoredPosition.y);
            slider.value = value;
            _smoothenCoroutine = null;
        }

        private void PlayVFX(int star)
        {
            for (int i = 0; i < star; ++i)
            {
                var starVFX = starVFXList[i];
                var emissionVFX = starEmissionVFXList[i];
                var isStarEnabled = activatedObjects[i].activeSelf;

                if (!isStarEnabled)
                {
                    if (_vfxEnabled)
                    {
                        starVFX.Play();
                        emissionVFX.Play();
                    }
                    activatedObjects[i].SetActive(true);
                }
            }
        }

        private void Clear()
        {
            _currentStar.Value = 0;
            slider.value = 0.0f;
            foreach (var activated in activatedObjects)
            {
                activated.SetActive(false);
            }
            foreach (var vfx in starVFXList)
            {
                vfx.Stop();
            }
            foreach (var vfx in starEmissionVFXList)
            {
                vfx.Stop();
            }
            stageProgressBarVFX.Stop();
        }
    }
}
