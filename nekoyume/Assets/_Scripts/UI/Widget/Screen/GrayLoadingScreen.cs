using System;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public enum GameInitProgress : int
    {
        InitAgent = 1,
        RequestPledge,
        ApprovePledge,
        EndPledge,
        CompleteLogin,

        InitIAP,
        InitTableSheet,
        InitCanvas,

        ProgressCompleted,
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
                case GameInitProgress.InitAgent:
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
            loadingSlider.slider.value = percent;
            loadingSlider.text.text = $"{percent}%";

            base.Show(true);
        }
    }
}
