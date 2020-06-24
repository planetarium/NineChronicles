using System.Linq;
using Assets.SimpleLocalization;
using FancyScrollView;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestCell : FancyScrollRectCell<QuestModel, QuestScroll.ContextModel>
    {
        public event System.Action onClickSubmitButton;

        [SerializeField]
        private Image background = null;

        [SerializeField]
        private Image fillImage = null;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private TextMeshProUGUI contentText = null;

        [SerializeField]
        private TextMeshProUGUI progressText = null;

        [SerializeField]
        private Slider progressBar = null;

        [SerializeField]
        private SimpleCountableItemView[] rewardViews = null;

        [SerializeField]
        private SubmitButton receiveButton = null;

        [SerializeField]
        private Animator animator;

        [Header("ItemMoveAnimation")]
        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField,
         Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        private QuestModel _quest;

        #region Mono

        private void Awake()
        {
            receiveButton.SetSubmitText(
                LocalizationManager.Localize("UI_PROGRESS"),
                LocalizationManager.Localize("UI_RECEIVE"));
            receiveButton.SetSubmitTextColor(ColorHelper.HexToColorRGB("955c4a"));
            receiveButton.OnSubmitClick.Subscribe(OnReceiveClick).AddTo(gameObject);
        }

        #endregion

        public override void UpdateContent(QuestModel itemData)
        {
            _quest = itemData;
            UpdateView();
        }

        private void OnReceiveClick(SubmitButton submitButton)
        {
            AudioController.PlayClick();
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            // 퀘스트 보상 창 위젯이 현재 퀘스트 완료 시 표시되므로 잠시 주석 처리합니다.
            //foreach (var view in rewardViews)
            //{
            //    if (!(view.Model is null) &&
            //        view.gameObject.activeSelf)
            //    {
            //        ItemMoveAnimation.Show(
            //            SpriteHelper.GetItemIcon(view.Model.ItemBase.Value.Id),
            //            view.transform.position,
            //            Widget.Find<BottomMenu>().characterButton.transform.position,
            //            moveToLeft,
            //            animationTime,
            //            middleXGap,
            //            true);
            //    }
            //}

            var rewards = rewardViews
                .Select(view => view.Model)
                .Where(item => !(item is null))
                .ToList();
            Widget.Find<QuestResult>().Show(rewards);
            animator.Play("Disappear");
            onClickSubmitButton?.Invoke();
        }

        private void RequestReward()
        {
            UpdateView();

            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REQUEST_REWARD");
            var msg = string.Format(format, _quest.GetContent());
            Notification.Push(MailType.System, msg);

            // 로컬 아바타의 퀘스트 상태 업데이트.
            var quest =
                States.Instance.CurrentAvatarState.questList.FirstOrDefault(q => q == _quest);
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

                LocalStateModifier.AddItem(
                    avatarAddress,
                    materialRow.Value.ItemId,
                    reward.Value,
                    false);
            }

            LocalStateModifier.RemoveReceivableQuest(avatarAddress, quest.Id);
        }

        private void UpdateView()
        {
            var isReceived = false;
            titleText.text = _quest.GetTitle();
            contentText.text = _quest.GetContent();

            var text = _quest.GetProgressText();
            var showProgressBar = !string.IsNullOrEmpty(text);
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
                    var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values.First(
                        itemRow => itemRow.Id == pair.Key);
                    var item = ItemFactory.CreateMaterial(row);
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

        private void UpdateTab()
        {
            RequestReward();
            Widget.Find<Quest>().UpdateTabs();
        }
    }
}
