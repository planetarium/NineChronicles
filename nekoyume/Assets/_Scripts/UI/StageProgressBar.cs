using Nekoyume.Game.VFX;
using System.Collections;
using Nekoyume.Game.Character;
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
        private RectTransform vfxClamper;
        [SerializeField]
        private RectTransform vfxOffset;
        [SerializeField]
        private float smoothenSpeed = 2.0f;
        [SerializeField]
        private float smoothenFinishThreshold = 0.01f;
        private float _xLength = 0;

        private int _currentStar = 0;
        private int _currentWaveHpSum = 0;
        private int _progress = 0;

        [SerializeField] private StageProgressBarVFX stageProgressBarVFX;
        [SerializeField] private Star01VFX star01VFX;
        [SerializeField] private Star02VFX star02VFX;
        [SerializeField] private Star03VFX star03VFX;
        [SerializeField] private StarEmission01VFX starEmission01VFX;
        [SerializeField] private StarEmission02VFX starEmission02VFX;
        [SerializeField] private StarEmission03VFX starEmission03VFX;

        private Coroutine _smoothenCoroutine = null;
        private System.Action _onCurrentSmoothenTerminated = null;

        private void Awake()
        {
            _xLength = vfxClamper.rect.width;
            Game.Event.OnEnemyDeadStart.AddListener(OnEnemyDeadStart);
            Game.Event.OnWaveStart.AddListener(SetNextWave);
            star01VFX.Stop();
            star02VFX.Stop();
            star03VFX.Stop();
            starEmission01VFX.Stop();
            starEmission02VFX.Stop();
            starEmission03VFX.Stop();
            stageProgressBarVFX.Stop();
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
            stageProgressBarVFX.Stop();
            stageProgressBarVFX = null;
            star01VFX.Stop();
            star02VFX.Stop();
            star03VFX.Stop();
            starEmission01VFX.Stop();
            starEmission02VFX.Stop();
            starEmission03VFX.Stop();
            gameObject.SetActive(false);
        }

        public void Initialize()
        {
            _currentStar = 0;
            slider.value = 0.0f;
        }

        private void CompleteWave()
        {
            ++_currentStar;

            var lerpSpeed = 1.0f;

            switch(_currentStar)
            {
                case 1:
                    _onCurrentSmoothenTerminated = () =>
                    {
                        star01VFX.Play();
                        starEmission01VFX.Play();
                        activatedStarImages[0].enabled = true;
                    };
                    break;
                case 2:
                    _onCurrentSmoothenTerminated = () =>
                    {
                        star02VFX.Play();
                        starEmission02VFX.Play();
                        activatedStarImages[1].enabled = true;
                    };
                    break;
                case 3:
                    _onCurrentSmoothenTerminated = () =>
                    {
                        star03VFX.Play();
                        starEmission03VFX.Play();
                        activatedStarImages[2].enabled = true;
                        lerpSpeed = 0.5f;
                    };
                    break;
                default:
                    return;
            }

            if (!(_smoothenCoroutine is null))
            {
                TerminateCurrentSmoothen(false);
            }

            if (isActiveAndEnabled)
            {
                _smoothenCoroutine = StartCoroutine(LerpProgressBar((float) _currentStar / MaxWave, lerpSpeed));
            }
            else
            {
                TerminateCurrentSmoothen();
            }
        }

        private void SetNextWave(int waveHpSum)
        {
            _currentWaveHpSum = _progress = waveHpSum;
        }

        private void IncreaseProgress(int hp)
        {
            stageProgressBarVFX.Play();

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
                TerminateCurrentSmoothen();
            }

            if (isActiveAndEnabled)
            {
                _smoothenCoroutine = StartCoroutine(LerpProgressBar(sliderValue));
            }
            else
            {
                TerminateCurrentSmoothen();
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

            slider.value = value;
            vfxOffset.anchoredPosition = new Vector2(value * _xLength, vfxOffset.anchoredPosition.y);
            _onCurrentSmoothenTerminated?.Invoke();
            _onCurrentSmoothenTerminated = null;
            _smoothenCoroutine = null;
        }

        private float GetProgress(float progress)
        {
            return ((_currentWaveHpSum - progress) / _currentWaveHpSum + _currentStar) / MaxWave;
        }

        private void TerminateCurrentSmoothen(bool callTerminated = true)
        {
            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
            }
            if (callTerminated)
            {
                _onCurrentSmoothenTerminated?.Invoke();
                _onCurrentSmoothenTerminated = null;
            }
            _smoothenCoroutine = null;
        }

        private void OnEnemyDeadStart(Enemy enemy)
        {
            IncreaseProgress(enemy.HP);
        }

        public void OnValueChanged()
        {
            stageProgressBarVFX.transform.position = vfxOffset.transform.position;
        }
    }
}
