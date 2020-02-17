using System.Collections;
using Nekoyume.Game.Character;
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
        private Image[] activatedStarImages = null;
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
        private int _currentWaveHpSum = 0;
        private int _progress = 0;
        private bool _vfxEnabled;

        [SerializeField] private StageProgressBarVFX stageProgressBarVFX = null;
        [SerializeField] private Star01VFX star01VFX = null;
        [SerializeField] private Star02VFX star02VFX = null;
        [SerializeField] private Star03VFX star03VFX = null;
        [SerializeField] private StarEmission01VFX starEmission01VFX = null;
        [SerializeField] private StarEmission02VFX starEmission02VFX = null;
        [SerializeField] private StarEmission03VFX starEmission03VFX = null;

        private Coroutine _smoothenCoroutine = null;

        private void Awake()
        {
            _xLength = vfxClamper.rect.width;
            Game.Event.OnEnemyDeadStart.AddListener(OnEnemyDeadStart);
            Game.Event.OnWaveStart.AddListener(SetNextWave);
            Clear();
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

            Clear();
            gameObject.SetActive(false);
        }

        public void Initialize(bool vfxEnabled)
        {
            _vfxEnabled = vfxEnabled;

            _currentStar.Subscribe(PlayVFX).AddTo(gameObject);
        }

        private void CompleteWave()
        {
            _currentStar.Value += 1;

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
            }
        }

        private void SetNextWave(int waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        private void IncreaseProgress(int hp)
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
                slider.value = current;
                vfxOffset.anchoredPosition = new Vector2(current * _xLength, vfxOffset.anchoredPosition.y);
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

        private void OnEnemyDeadStart(Enemy enemy)
        {
            IncreaseProgress(enemy.HP - enemy.CharacterModel.Stats.BuffStats.HP);
        }

        public void OnValueChanged()
        {
            stageProgressBarVFX.transform.position = vfxOffset.transform.position;
        }

        private void UpdateSliderValue(float value)
        {
            slider.value = value;
            vfxOffset.anchoredPosition = new Vector2(value * _xLength, vfxOffset.anchoredPosition.y);
            _smoothenCoroutine = null;
        }

        private void PlayVFX(int star)
        {
            switch (star)
            {
                case 1:
                    if (_vfxEnabled)
                    {
                        star01VFX.Play();
                        starEmission01VFX.Play();
                    }
                    activatedStarImages[0].gameObject.SetActive(true);
                    break;
                case 2:
                    if (_vfxEnabled)
                    {
                        star02VFX.Play();
                        starEmission02VFX.Play();
                    }
                    activatedStarImages[1].gameObject.SetActive(true);
                    break;
                case 3:
                    if (_vfxEnabled)
                    {
                        star03VFX.Play();
                        starEmission03VFX.Play();
                    }
                    activatedStarImages[2].gameObject.SetActive(true);
                    break;
                default:
                    return;
            }
        }

        private void Clear()
        {
            _currentStar.Value = 0;
            slider.value = 0.0f;
            activatedStarImages[0].gameObject.SetActive(false);
            activatedStarImages[1].gameObject.SetActive(false);
            activatedStarImages[2].gameObject.SetActive(false);
            star01VFX.Stop();
            star02VFX.Stop();
            star03VFX.Stop();
            starEmission01VFX.Stop();
            starEmission02VFX.Stop();
            starEmission03VFX.Stop();
            stageProgressBarVFX.Stop();
        }
    }
}
