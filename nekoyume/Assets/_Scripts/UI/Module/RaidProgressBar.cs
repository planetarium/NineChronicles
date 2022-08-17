using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
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

        private void Awake()
        {
            _xLength = vfxClamper.rect.width;
            _currentStar.Subscribe(PlayVFX).AddTo(gameObject);
        }

        public void Show()
        {
            Clear();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (_smoothenCoroutine is not null)
            {
                StopCoroutine(_smoothenCoroutine);
                _smoothenCoroutine = null;
            }

            Clear();
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

            var sliderValue = (float)_currentStar.Value / MaxWave;
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

        public void CompleteWave()
        {
            SetStarProgress(_currentStar.Value + 1);
        }

        public void UpdateScore(int score)
        {
            scoreText.text = score.ToString("N0");
            var grade = (WorldBossGrade) WorldBossHelper.CalculateRank(score);
            if (_currentGrade != grade &&
                WorldBossFrontHelper.TryGetGrade(grade, out var prefab))
            {
                Destroy(gradeObject);
                gradeObject = Instantiate(prefab, gradeParent);
                gradeObject.transform.localScale = Vector3.one;
                _currentGrade = grade;
            }
        }

        private IEnumerator LerpProgressBar(float value, float additionalSpeed = 1.0f)
        {
            stageProgressBarVFX.Play();
            var current = slider.value;
            var speed = smoothenSpeed * additionalSpeed;

            while (current < value - smoothenFinishThreshold)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * speed);
                slider.value = current;
                yield return null;
            }

            UpdateSliderValue(value);
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

        private void UpdateSliderValue(float value)
        {
            var vfxValue = (float) Mathf.Clamp(_currentStar.Value + 1, 0, MaxWave) / MaxWave;
            vfxOffset.anchoredPosition = new Vector2(vfxValue * _xLength, vfxOffset.anchoredPosition.y);
            slider.value = value;
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

        private void Clear()
        {
            _currentGrade = WorldBossGrade.None;
            UpdateScore(0);
            _currentStar.Value = 0;
            slider.value = 0.0f;
            foreach (var activated in activatedObjects)
            {
                activated.SetActive(false);
            }
            foreach (var vfx in starEmissionVFXList)
            {
                vfx.Stop();
            }
            SetStarProgress(0);
        }
    }
}
