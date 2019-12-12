using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Mail;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class QuestCellView : EnhancedScrollerCellView
    {
        public Image background;
        public TextMeshProUGUI contentText;
        public TextMeshProUGUI rewardsText;
        public SimpleCountableItemView[] rewardViews;
        public SubmitButton receiveButton;

        private Game.Quest.Quest _quest;
        private int _currentDataIndex;

        public Func<int, bool> onClickSubmitButton;

        #region Mono

        private void Awake()
        {
            rewardsText.text = LocalizationManager.Localize("UI_REWARDS");
            receiveButton.submitText.text = LocalizationManager.Localize("UI_GET_REWARDS");
            receiveButton.SetSubmittable(true);

            receiveButton.OnSubmitClick.Subscribe(OnReceiveClick).AddTo(gameObject);
        }

        #endregion

        public void SetData(Game.Quest.Quest quest, bool isLocalReceived)
        {
            _quest = quest;
            _currentDataIndex = dataIndex;

            UpdateView(isLocalReceived);
        }

        private void OnReceiveClick(SubmitButton submitButton)
        {
            AudioController.PlayClick();
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            RequestReward();
            onClickSubmitButton?.Invoke(_currentDataIndex);
        }

        private void RequestReward()
        {
            UpdateView(true);

            ActionManager.instance.QuestReward(_quest.Id);
            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REQUEST_REWARD");
            var msg = string.Format(format, _quest.GetName());
            Notification.Push(MailType.System, msg);

            // 로컬 아바타의 퀘스트 상태 업데이트.
            var quest = States.Instance.CurrentAvatarState.Value.questList.FirstOrDefault(q => q == _quest);
            if (quest is null)
                return;

            quest.Receive = true;
        }

        private void UpdateView(bool isLocalReceived)
        {
            var isReceived = false;
            contentText.text = _quest.ToInfo();

            if (_quest.Complete)
            {
                if (_quest.Receive || isLocalReceived)
                {
                    isReceived = true;
                    
                    background.color = ColorHelper.HexToColorRGB("7b7b7b");
                    contentText.color = ColorHelper.HexToColorRGB("3f3f3f");
                    rewardsText.color = ColorHelper.HexToColorRGB("3f3f3f");
                    receiveButton.Hide();
                }
                else
                {
                    background.color = Color.white;
                    contentText.color = ColorHelper.HexToColorRGB("d3a03b");
                    rewardsText.color = ColorHelper.HexToColorRGB("e5d1a7");
                    receiveButton.Show();
                }
            }
            else
            {
                background.color = Color.white;
                contentText.color = ColorHelper.HexToColorRGB("d3a03b");
                rewardsText.color = ColorHelper.HexToColorRGB("e5d1a7");
                receiveButton.Hide();
            }

            var itemMap = _quest.Reward.ItemMap;
            for (var i = 0; i < rewardViews.Length; i++)
            {
                if (i < itemMap.Count)
                {
                    var pair = itemMap.ElementAt(i);
                    var rewardView = rewardViews[i];
                    var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(itemRow => itemRow.Id == pair.Key);
                    var item = ItemFactory.Create(row, new Guid());
                    var countableItem = new CountableItem(item, pair.Value);
                    countableItem.Dimmed.Value = isReceived;
                    rewardView.SetData(countableItem);
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
