using System;
using DG.Tweening;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public enum GameInitProgress : int
    {
        // 맨 앞에 ProgressStart, 맨 뒤에 ProgressCompleted는 필수. 이외의 enum은 순서가 중요하지 않음.
        // 해당 동작이 시작하기 전에 호출
        ProgressStart = 1, // called at after close loginSystem in Game.Start()
        RequestPledge, // called in mobile
        ApprovePledge,
        EndPledge,

        InitIAP, // ~called in mobile
        InitTableSheet,
        InitCanvas,

        ProgressCompleted // called at last waiting point in Game.Start()
    }

    public class GrayLoadingScreen : ScreenWidget
    {
        [Serializable]
        public class LoadingSlider
        {
            public GameObject container;
            public Slider slider;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private TextMeshProUGUI text;

        [SerializeField]
        private Image background;

        [SerializeField]
        private LoadingSlider loadingSlider;

        private int _progress = 0;

        public const float SliderAnimationDuration = 2f;

        public void Show(string message, bool localize, float alpha = 0.4f)
        {
            loadingSlider.container.SetActive(false);

            if (localize)
            {
                message = L10nManager.Localize(message);
            }

            text.text = message;

            var color = background.color;
            color.a = alpha;
            background.color = color;

            base.Show();
        }

        public void ShowProgress(GameInitProgress progress)
        {
            switch (progress)
            {
                case GameInitProgress.ProgressStart:
                case GameInitProgress.ProgressCompleted:
                    _progress = (int)progress;
                    break;
                default:
                    _progress++;
                    break;
            }

            const int end = (int)GameInitProgress.ProgressCompleted;
            var percent = Mathf.RoundToInt((float)_progress / end * 100);

            text.text = L10nManager.Localize($"UI_LOADING_GAME_START_{(int)progress}");

            loadingSlider.container.SetActive(true);
            loadingSlider.slider.DOValue(percent, SliderAnimationDuration);
            loadingSlider.text.text = $"{percent}%";

            base.Show(true);
        }
    }
}
