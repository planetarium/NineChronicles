using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using System.Linq;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using UniRx;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestCellView : EnhancedScrollerCellView
    {
        public Image background;
        public Image fillImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        public TextMeshProUGUI progressText;
        public Slider progressBar;
        public SimpleCountableItemView[] rewardViews;
        public SubmitButton receiveButton;

        private QuestModel _quest;
        private int _currentDataIndex;

        public System.Action onClickSubmitButton;

        #region Mono

        private void Awake()
        {
            receiveButton.SetSubmitText(
                LocalizationManager.Localize("UI_PROGRESS"),
                LocalizationManager.Localize("UI_RECEIVE"));
            receiveButton.SetSubmittable(true); 
            receiveButton.OnSubmitClick.Subscribe(OnReceiveClick).AddTo(gameObject);
            receiveButton.submitText.color = ColorHelper.HexToColorRGB("955c4a");
        }

        #endregion

        public void SetData(QuestModel quest)
        {
            _quest = quest;
            _currentDataIndex = dataIndex;

            UpdateView();
        }
         
        private void OnReceiveClick(SubmitButton submitButton)
        {
            AudioController.PlayClick();
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            for(int i=0; i < rewardViews.Length; i++)
            {
                if (rewardViews[i].gameObject.activeSelf)
                    QuestRewardItem.Show(rewardViews[i], i);
            }
            var quest = Widget.Find<Quest>();   
            RequestReward();
            quest.UpdateTabs();
            onClickSubmitButton?.Invoke();
        }

        private void RequestReward()
        {
            UpdateView();

            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REQUEST_REWARD");
            var msg = string.Format(format, _quest.GetContent());
            Notification.Push(MailType.System, msg);

            // 로컬 아바타의 퀘스트 상태 업데이트.
            var quest = States.Instance.CurrentAvatarState.questList.FirstOrDefault(q => q == _quest);
            if (quest is null)
            {
                return;
            }
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var rewardMap = quest.Reward.ItemMap;

            foreach (var reward in rewardMap)
            {
                var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet
                    .First(pair => pair.Key == reward.Key);

                LocalStateModifier.AddItem(avatarAddress, materialRow.Value.ItemId, reward.Value);
            }
            LocalStateModifier.RemoveReceivableQuest(avatarAddress, quest.Id);
        }

        private void UpdateView()
        {
            var isReceived = false;
            titleText.text = _quest.GetTitle();
            contentText.text = _quest.GetContent();

            string text = _quest.GetProgressText();
            bool showProgressBar = !string.IsNullOrEmpty(text); 
            progressText.gameObject.SetActive(showProgressBar);
            progressBar.gameObject.SetActive(showProgressBar);
            if (showProgressBar)
            {
                progressText.text = text;
                progressBar.value = _quest.Progress;
            }

            if (_quest.Complete)
            {
                if (_quest.isReceivable)
                {
                    background.color = Color.white;
                    fillImage.color = ColorHelper.HexToColorRGB("ffffff");
                    titleText.color = ColorHelper.HexToColorRGB("ffa78b");
                    contentText.color = ColorHelper.HexToColorRGB("955c4a");
                    progressText.color = ColorHelper.HexToColorRGB("e0a491");
                    receiveButton.Show();
                    receiveButton.SetSubmittable(true);
                }
                else
                {
                    isReceived = true;
                    fillImage.color = ColorHelper.HexToColorRGB("282828");
                    background.color = ColorHelper.HexToColorRGB("7b7b7b");
                    titleText.color = ColorHelper.HexToColorRGB("614037");
                    contentText.color = ColorHelper.HexToColorRGB("38251e");
                    progressText.color = ColorHelper.HexToColorRGB("282828");
                    receiveButton.Hide();
                }
            }
            else
            {
                background.color = Color.white;
                fillImage.color = ColorHelper.HexToColorRGB("ffffff");
                titleText.color = ColorHelper.HexToColorRGB("ffa78b");
                contentText.color = ColorHelper.HexToColorRGB("955c4a");
                progressText.color = ColorHelper.HexToColorRGB("e0a491");
                receiveButton.Show();
                receiveButton.SetSubmittable(false);
            }

            var itemMap = _quest.Reward.ItemMap;
            for (var i = 0; i < rewardViews.Length; i++)
            {
                if (i < itemMap.Count)
                {
                    var pair = itemMap.ElementAt(i);
                    var rewardView = rewardViews[i];
                    rewardView.ignoreOne = true;
                    var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(itemRow => itemRow.Id == pair.Key);
                    var item = ItemFactory.Create(row, new Guid());
                    var countableItem = new CountableItem(item, pair.Value);
                    countableItem.Dimmed.Value = isReceived;
                    rewardView.SetData(countableItem);
                    rewardView.iconImage.rectTransform.sizeDelta *= 0.7f;
                    rewardView.gameObject.SetActive(true);
                }
                else
                {
                    rewardViews[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
