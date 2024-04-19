using System;
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
        private Button summonButton;

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
        private Image summonNotificationImage;

        [SerializeField]
        private SpeechBubble speechBubble;

        [SerializeField]
        private LockObject[] lockObjects;

        [Serializable]
        private struct LockObject
        {
            public GameObject lockObject;
            public int requiredStageId;
            public Button lockedButton;
        }

        protected override void Awake()
        {
            base.Awake();

            summonButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Summon>().Show();
                AudioController.PlayClick();
            });

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

            if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var lastClearedStageId))
            {
                foreach (var lockObj in lockObjects)
                {
                    var isLocked = lastClearedStageId < lockObj.requiredStageId;
                    lockObj.lockObject.SetActive(isLocked);
                    lockObj.lockedButton.interactable = !isLocked;
                }
            }

            var audioController = AudioController.instance;
            var musicName = AudioController.MusicCode.Combination;
            if (!audioController.CurrentPlayingMusicName.Equals(musicName))
            {
                AudioController.instance.PlayMusic(musicName);
            }

            StartCoroutine(speechBubble.CoShowText(true));
        }

        private void UpdateNotification()
        {
            // craft
            craftNotificationImage.enabled = Craft.SharedModel.HasNotification;

            // rune
            runeNotificationImage.enabled = false;
            var allRuneState = States.Instance.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            foreach (var runeRow in runeListSheet)
            {
                var runeLevel = allRuneState.TryGetRuneState(runeRow.Id, out var runeState)
                    ? runeState.Level
                    : 0;

                var runeItem = new RuneItem(runeRow, runeLevel);
                if (runeItem.HasNotification)
                {
                    runeNotificationImage.enabled = true;
                    break;
                }
            }

            summonNotificationImage.enabled = Summon.HasNotification;
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickSummonEnteringButton()
        {
            summonButton.onClick?.Invoke();
            Find<Summon>().SetCostUIForTutorial();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationUpgradeButton()
        {
            upgradeButton.onClick?.Invoke();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationGrindButton()
        {
            grindButton.onClick?.Invoke();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationRuneButton()
        {
            runeButton.onClick?.Invoke();
        }
    }
}
