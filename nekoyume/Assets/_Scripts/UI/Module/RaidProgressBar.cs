using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RaidProgressBar : MonoBehaviour
    {
        private const int MaxWave = 5;

        [SerializeField]
        private TextMeshProUGUI scoreText = null;
        [SerializeField]
        private GameObject gradeObject = null;
        [SerializeField]
        private Transform gradeParent = null;

        [SerializeField]
        private Slider slider = null;
        [SerializeField]
        private Slider currentWaveSlider = null;
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

        [SerializeField]
        private StageProgressBarVFX stageProgressBarVFX = null;

        [SerializeField]
        private List<VFX> starEmissionVFXList = null;

        private Coroutine _smoothenCoroutine = null;
        private WorldBossGrade _currentGrade;
        private WorldBossCharacterSheet.Row _currentRow;

        private void Awake()
        {
            _xLength = vfxClamper.rect.width;
            _currentStar.Subscribe(PlayVFX).AddTo(gameObject);
        }

        public void Show()
        {
            if (_currentRow != null)
            {
                SetStarProgress(_currentStar.Value);
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Close()
        {
            if (_smoothenCoroutine is not null)
            {
                StopCoroutine(_smoothenCoroutine);
                _smoothenCoroutine = null;
            }

            gameObject.SetActive(false);
        }

        private void SetStarProgress(int star)
        {
            _currentStar.Value = star;
            var lerpSpeed = _currentStar.Value == MaxWave ? 0.5f : 1.0f;

            if (_smoothenCoroutine is not null)
            {
                TerminateCurrentSmoothen();
            }

            (var prev, var current) = GetSliderValue(_currentStar.Value, MaxWave);
            if (isActiveAndEnabled)
            {
                _smoothenCoroutine = StartCoroutine(LerpProgressBar(prev, current, lerpSpeed));
            }
            else
            {
                UpdateSliderValue(prev, current);
                for (int i = 0; i < star; ++i)
                {
                    activatedObjects[i].SetActive(true);
                }
            }
        }

        public void CompleteWave()
        {
            SetStarProgress(_currentStar.Value + 1);
        }

        public void UpdateScore(long score)
        {
            scoreText.text = score.ToString("N0");
            var grade = (WorldBossGrade) WorldBossHelper.CalculateRank(_currentRow, score);
            if (_currentGrade != grade &&
                WorldBossFrontHelper.TryGetGrade(grade, false, out var prefab))
            {
                Destroy(gradeObject);
                gradeObject = Instantiate(prefab, gradeParent);
                gradeObject.transform.localScale = Vector3.one;
                _currentGrade = grade;
            }
        }

        private IEnumerator LerpProgressBar(float prevValue, float currentValue, float additionalSpeed = 1.0f)
        {
            stageProgressBarVFX.Play();
            var prev = slider.value;
            var current = currentWaveSlider.value;
            var speed = smoothenSpeed * additionalSpeed;

            while (prev < prevValue - smoothenFinishThreshold)
            {
                prev = Mathf.Lerp(prev, prevValue, Time.deltaTime * speed);
                current = Mathf.Lerp(current, currentValue, Time.deltaTime * speed);
                slider.value = prev;
                currentWaveSlider.value = current;
                yield return null;
            }

            UpdateSliderValue(prevValue, currentValue);
        }

        private void TerminateCurrentSmoothen()
        {
            if (!(_smoothenCoroutine is null))
            {
                StopCoroutine(_smoothenCoroutine);
            }
            _smoothenCoroutine = null;
        }

        public void OnValueChanged()
        {
            stageProgressBarVFX.transform.position = vfxOffset.transform.position;
        }

        private void UpdateSliderValue(float prevValue, float currentValue)
        {
            vfxOffset.anchoredPosition = new Vector2(currentValue * _xLength, vfxOffset.anchoredPosition.y);
            slider.value = prevValue;
            currentWaveSlider.value = currentValue;
            _smoothenCoroutine = null;
        }

        private void PlayVFX(int star)
        {
            if (star > 0)
            {
                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            for (int i = 0; i < star; ++i)
            {
                var emissionVFX = starEmissionVFXList[i];
                var isStarEnabled = activatedObjects[i].activeSelf;

                if (!isStarEnabled)
                {
                    emissionVFX.Play();
                    activatedObjects[i].SetActive(true);
                }
            }
        }

        public void Clear(int bossId)
        {
            if (!Game.Game.instance.TableSheets.WorldBossCharacterSheet
                .TryGetValue(bossId, out _currentRow))
            {
                return;
            }

            SetStarProgress(0);
            UpdateScore(0);
            slider.value = 0.0f;
            currentWaveSlider.value = 1.0f / MaxWave;
            foreach (var activated in activatedObjects)
            {
                activated.SetActive(false);
            }
            foreach (var vfx in starEmissionVFXList)
            {
                vfx.Stop();
            }
        }

        private (float prev, float current) GetSliderValue(int wave, int maxWave)
        {
            var prev = (float)wave / maxWave;
            var current = (float)Mathf.Clamp(wave + 1, 0, maxWave) / maxWave;
            return (prev, current);
        }
    }
}
