using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    /// <summary>
    /// 새로운 퀘스트가 추가하거나 이미 설정된 퀘스트를 완료할 때의 연출을 책임집니다.
    /// 이때 연출은 QuestResult를 다루는 것까지 포함합니다.
    /// </summary>
    public class GuidedQuestCell : MonoBehaviour
    {
        // NOTE: 콘텐츠 텍스트의 길이가 UI를 넘어갈 수 있기 때문에 flowing text 처리를 해주는 것이 좋겠습니다.
        [SerializeField]
        private TextMeshProUGUI contentText;

        // NOTE: 콘텐츠 텍스트의 길이가 UI를 넘어갈 수 있기 때문에 flowing text 처리를 해주는 것이 좋겠습니다.
        [SerializeField]
        private TextMeshProUGUI effectedContentText;

        [SerializeField]
        private Image effectedBodyImage;

        // NOTE: 가이드 퀘스트 보상 아이콘의 연출 스펙에 따라서 별도로 XxxItemView를 만들어서 사용합니다.
        [SerializeField]
        private List<StageRewardItemView> rewards;

        [SerializeField]
        private Button bodyButton;

        // NOTE: 셀이 더해지고 빠지는 연출이 정해지면 더욱 개선됩니다.
        [SerializeField]
        private AnchoredPositionSingleTweener showingAndHidingTweener;

        [SerializeField]
        private TransformLocalScaleTweener inProgressTweener;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Image effectedIconImage;

        [SerializeField]
        private Image gradientImage;

        private bool _inProgress;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();
        private readonly List<IDisposable> _disposables = new();

        public Quest Quest { get; private set; }

        #region MonoBehaviour

        private void Awake()
        {
            bodyButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    onClick.OnNext(this);
                })
                .AddTo(gameObject);
            L10nManager.OnLanguageChange
                .Subscribe(_ => { if (Quest != null) SetContent(Quest); })
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            showingAndHidingTweener.Stop();
        }

        #endregion

        #region Control

        public void ShowAsNew(
            Quest quest,
            Action<GuidedQuestCell> onComplete = null,
            bool ignoreAnimation = false,
            bool isEvent = false)
        {
            if (quest is null)
            {
                onComplete?.Invoke(this);
                return;
            }

            SetToInProgress(false);
            Quest = quest;
            SetContent(Quest);

            if (ignoreAnimation)
            {
                SetRewards(Quest.Reward.ItemMap, true);
                onComplete?.Invoke(this);
            }
            else
            {
                ClearRewards();
                showingAndHidingTweener
                    .PlayTween()
                    .OnPlay(() => gameObject.SetActive(true))
                    .OnComplete(() =>
                    {
                        SetRewards(Quest.Reward.ItemMap);
                        onComplete?.Invoke(this);
                    });
            }

            var eventInfo = EventManager.GetEventInfo() as EventDungeonIdBasedEventInfo;
            if (isEvent && eventInfo is not null)
            {
                var guidedQuestData = Quest switch
                {
                    WorldQuest => eventInfo.EventDungeonGuidedQuest,
                    CombinationEquipmentQuest => eventInfo.EventRecipeGuidedQuest,
                };
                iconImage.sprite = guidedQuestData.icon;
                effectedIconImage.sprite = guidedQuestData.icon;
                gradientImage.color = guidedQuestData.gradiantColor;
            }
        }

        public void Show(Quest quest, bool isEvent = false)
        {
            ShowAsNew(quest, null, true, isEvent);
        }

        public void SetToInProgress(bool inProgress)
        {
            _inProgress = inProgress;
            if (_inProgress)
            {
                contentText.gameObject.SetActive(false);
                effectedContentText.gameObject.SetActive(true);
                effectedBodyImage.gameObject.SetActive(true);
                showingAndHidingTweener.Stop();
                inProgressTweener.PlayTween();
            }
            else
            {
                contentText.gameObject.SetActive(true);
                effectedContentText.gameObject.SetActive(false);
                effectedBodyImage.gameObject.SetActive(false);
                inProgressTweener.KillTween();
                inProgressTweener.ResetToOriginalLocalScale();
            }
        }

        public void HideAsClear(
            Action<GuidedQuestCell> onComplete = null,
            bool ignoreAnimation = false,
            bool ignoreQuestResult = false)
        {
            SetToInProgress(false);

            if (ignoreAnimation)
            {
                PostHideAsClear(onComplete);
            }
            else
            {
                showingAndHidingTweener
                    .PlayReverse()
                    .OnComplete(() =>
                    {
                        if (ignoreQuestResult)
                        {
                            PostHideAsClear(onComplete);
                            return;
                        }

                        StartCoroutine(CoShowQuestResult(() =>
                            PostHideAsClear(onComplete)));
                    });
            }
        }

        public void Hide()
        {
            HideAsClear(null, true, true);
        }

        private void PostHideAsClear(Action<GuidedQuestCell> onComplete)
        {
            Quest = null;
            gameObject.SetActive(false);
            onComplete?.Invoke(this);
        }

        #endregion

        private IEnumerator CoShowQuestResult(System.Action onComplete)
        {
            var questResult = Widget.Find<CelebratesPopup>();
            questResult.Show(Quest);
            yield return new WaitWhile(() => questResult.IsActive());
            onComplete?.Invoke();
        }

        #region Update view objects

        private void SetContent(Quest quest)
        {
            contentText.text = effectedContentText.text = quest.GetContent();
        }

        private void SetRewards(
            IReadOnlyDictionary<int, int> rewardMap,
            bool ignoreAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            var sheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var delay = .3f;
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if (i < rewardMap.Count)
                {
                    var pair = rewardMap.ElementAt(i);
                    var row = sheet.OrderedList.FirstOrDefault(itemRow => itemRow.Id == pair.Key);
                    Assert.NotNull(row);
                    reward.SetData(row, () => ShowTooltip(reward));

                    if (ignoreAnimation)
                    {
                        reward.Show();
                    }
                    else
                    {
                        reward.ShowWithScaleTween(delay);
                        delay += .3f;
                    }
                }
                else
                {
                    reward.Hide();
                }
            }
        }

        private static void ShowTooltip(StageRewardItemView reward)
        {
            AudioController.PlayClick();
            ItemTooltip.Find(reward.Data.ItemType)
                .Show(reward.Data, string.Empty, false, null);
        }

        private void ClearRewards()
        {
            foreach (var reward in rewards)
            {
                reward.Hide();
            }
        }

        #endregion
    }
}
