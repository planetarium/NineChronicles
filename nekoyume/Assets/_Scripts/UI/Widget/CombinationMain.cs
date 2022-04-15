using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationMain : Widget
    {
        [SerializeField]
        private Button combineButton = null;

        [SerializeField]
        private Button upgradeButton = null;

        [SerializeField]
        private Button grindButton;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private Image craftNotificationImage = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        protected override void Awake()
        {
            base.Awake();

            combineButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Craft>().Show();
                AudioController.PlayClick();
            });

            upgradeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Enhancement>().Show();
                AudioController.PlayClick();
            });

            grindButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Grind>().Show();
                AudioController.PlayClick();
            });

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
                AudioController.PlayClick();
            });

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            speechBubble.SetKey("SPEECH_COMBINE_EQUIPMENT_");
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            craftNotificationImage.enabled = Craft.SharedModel.HasNotification;
            base.Show(ignoreShowAnimation);

            var audioController = AudioController.instance;
            var musicName = AudioController.MusicCode.Combination;
            if (!audioController.CurrentPlayingMusicName.Equals(musicName))
            {
                AudioController.instance.PlayMusic(musicName);
            }

            HelpTooltip.HelpMe(100007, true);
            StartCoroutine(speechBubble.CoShowText(true));
        }
    }
}
