using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
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
    using Nekoyume.L10n;
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
        private ConditionalButton receiveButton = null;

        private QuestModel _quest = null;

        public event System.Action onClickSubmitButton = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            receiveButton.OnSubmitSubject
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnReceiveClick)
                .AddTo(gameObject);

            receiveButton.SetText(
                ConditionalButton.State.Normal,
                L10nManager.Localize("UI_RECEIVE"));

            receiveButton.SetText(
                ConditionalButton.State.Disabled,
                L10nManager.Localize("UI_PROGRESS"));
        }

        #endregion

        public override void UpdateContent(QuestModel itemData)
        {
            _quest = itemData;
            UpdateView();
        }

        private void OnReceiveClick(Unit unit)
        {
            var questWidget = Widget.Find<QuestPopup>();
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
                    contentText.color = ColorHelper.HexToColorRGB("B99289");
                    progressText.color = ColorHelper.HexToColorRGB("e0a491");
                    receiveButton.gameObject.SetActive(true);
                    receiveButton.Interactable = true;
                }
                else
                {
                    isReceived = true;
                    fillImage.color = ColorHelper.HexToColorRGB("282828");
                    background.color = ColorHelper.HexToColorRGB("7b7b7b");
                    titleText.color = ColorHelper.HexToColorRGB("614037");
                    contentText.color = ColorHelper.HexToColorRGB("B99289");
                    progressText.color = ColorHelper.HexToColorRGB("282828");
                    receiveButton.gameObject.SetActive(false);
                }
            }
            else
            {
                background.color = Color.white;
                fillImage.color = ColorHelper.HexToColorRGB("ffffff");
                titleText.color = ColorHelper.HexToColorRGB("ffa78b");
                contentText.color = ColorHelper.HexToColorRGB("B99289");
                progressText.color = ColorHelper.HexToColorRGB("e0a491");
                receiveButton.gameObject.SetActive(true);
                receiveButton.Interactable = false;
            }

            _disposables.DisposeAllAndClear();
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
                    rewardView.touchHandler.OnClick.Subscribe(_ =>
                    {
                        AudioController.PlayClick();
                        var tooltip = ItemTooltip.Find(item.ItemType);
                        tooltip.Show(item,
                            string.Empty,
                            false,
                            null);
                    }).AddTo(_disposables);
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
            Widget.Find<QuestPopup>().DoneScrollAnimation();
        }

        public void ShowAsComplete()
        {
            fillImage.color = ColorHelper.HexToColorRGB("282828");
            background.color = ColorHelper.HexToColorRGB("7b7b7b");
            titleText.color = ColorHelper.HexToColorRGB("614037");
            contentText.color = ColorHelper.HexToColorRGB("B99289");
            progressText.color = ColorHelper.HexToColorRGB("282828");
            receiveButton.gameObject.SetActive(false);
        }
    }
}
