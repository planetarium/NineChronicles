using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationMain : Widget
    {
        [SerializeField]
        private Button combineButton;

        [SerializeField]
        private Button upgradeButton;

        [SerializeField]
        private Button grindButton;

        [SerializeField]
        private Button runeButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Image craftNotificationImage;

        [SerializeField]
        private Image runeNotificationImage;

        [SerializeField]
        private SpeechBubble speechBubble;

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

            runeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Rune>().Show(true);
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
            UpdateNotification();
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

        private void UpdateNotification()
        {
            // craft
            craftNotificationImage.enabled = Craft.SharedModel.HasNotification;

            // rune
            runeNotificationImage.enabled = false;
            var runeStates = States.Instance.RuneStates;
            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            foreach (var value in sheet.Values)
            {
                var state = runeStates.FirstOrDefault(x => x.RuneId == value.Id);
                var runeItem = new RuneItem(value, state?.Level ?? 0);
                if (runeItem.HasNotification)
                {
                    runeNotificationImage.enabled = true;
                    return;
                }
            }
        }
    }
}
