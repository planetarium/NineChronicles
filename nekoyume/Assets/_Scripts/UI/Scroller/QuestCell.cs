using System;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class QuestCell : RectCell<QuestModel, QuestScroll.ContextModel>
    {
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

        private QuestModel _quest = null;

        public event System.Action onClickSubmitButton = null;

        #region Mono

        private void Awake()
        {
            receiveButton.SetSubmitText(
                L10nManager.Localize("UI_PROGRESS"),
                L10nManager.Localize("UI_RECEIVE"));
            receiveButton.SetSubmitTextColor(ColorHelper.HexToColorRGB("955c4a"));
            receiveButton.OnSubmitClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnReceiveClick)
                .AddTo(gameObject);
        }

        #endregion

        public override void UpdateContent(QuestModel itemData)
        {
            _quest = itemData;
            UpdateView();
        }

        private void OnReceiveClick(SubmitButton submitButton)
        {
            var questWidget = Widget.Find<Quest>();
            questWidget.EnqueueCompletedQuest(_quest);

            AudioController.PlayClick();

            QuestRewardVFX lastVFX = null;
            foreach (var rewardView in rewardViews)
            {
                if (!(rewardView.Model is null) && rewardView.gameObject.activeSelf)
                {
                    lastVFX = VFXController.instance.CreateAndChaseCam<QuestRewardVFX>(rewardView
                        .transform.position);
                }
            }
            ShowAsComplete();
            if (lastVFX != null)
            {
                lastVFX.OnFinished = () =>
                {
                    var rectTransform = (RectTransform) transform;

                    questWidget.DisappearAnimation(
                        Mathf.FloorToInt(-rectTransform.anchoredPosition.y /
                                         rectTransform.sizeDelta.y));
                    lastVFX.OnFinished = null;
                };
            }

            onClickSubmitButton?.Invoke();
        }

        private void UpdateView()
        {
            if (_quest is null)
            {
                Hide();
                return;
            }

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

        public void UpdateTab()
        {
            UpdateView();
            Widget.Find<Quest>().DoneScrollAnimation();
        }

        public void ShowAsComplete()
        {
            fillImage.color = ColorHelper.HexToColorRGB("282828");
            background.color = ColorHelper.HexToColorRGB("7b7b7b");
            titleText.color = ColorHelper.HexToColorRGB("614037");
            contentText.color = ColorHelper.HexToColorRGB("38251e");
            progressText.color = ColorHelper.HexToColorRGB("282828");
            receiveButton.Hide();
        }
    }
}
