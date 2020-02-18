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
        public Image contentTextBullet;
        public Image fillImage;
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
                LocalizationManager.Localize("UI_COMPLETED"),
                LocalizationManager.Localize("UI_RECEIVE"));
            receiveButton.SetSubmittable(true); 
            receiveButton.OnSubmitClick.Subscribe(OnReceiveClick).AddTo(gameObject);
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
            foreach(var view in rewardViews)
            {
                if (view.gameObject.activeSelf)
                    ItemMoveAnimation.Show(SpriteHelper.GetItemIcon(view.Model.ItemBase.Value.Data.Id), view.transform.position, Widget.Find<BottomMenu>().inventoryButton.transform.position);
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
            var msg = string.Format(format, _quest.GetName());
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
            contentText.text = _quest.GetName();

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
                    contentText.color = ColorHelper.HexToColorRGB("e0a491");
                    contentTextBullet.color = ColorHelper.HexToColorRGB("e0a491");
                    progressText.color = ColorHelper.HexToColorRGB("e0a491");
                    receiveButton.Show();
                    receiveButton.SetSubmittable(true);
                }
                else
                {
                    isReceived = true;
                    fillImage.color = ColorHelper.HexToColorRGB("282828");
                    background.color = ColorHelper.HexToColorRGB("7b7b7b");
                    contentText.color = ColorHelper.HexToColorRGB("3f3f3f");
                    contentTextBullet.color = ColorHelper.HexToColorRGB("3f3f3f");
                    progressText.color = ColorHelper.HexToColorRGB("282828");
                    receiveButton.Show();
                    receiveButton.SetSubmittable(false);
                }
            }
            else
            {
                background.color = Color.white;
                fillImage.color = ColorHelper.HexToColorRGB("ffffff");
                contentText.color = ColorHelper.HexToColorRGB("e0a491");
                contentTextBullet.color = ColorHelper.HexToColorRGB("e0a491");
                progressText.color = ColorHelper.HexToColorRGB("e0a491");
                receiveButton.Hide();
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
