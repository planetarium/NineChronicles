using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using NUnit.Framework;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class GuidedQuestCell : MonoBehaviour
    {
        private const string MainContentFormat = "<color=red>[Main]</color> {0}";
        private const string SubContentFormat = "<color=red>[Sub]</color> {0}";

        // NOTE: 콘텐츠 텍스트의 길이가 UI를 넘어갈 수 있기 때문에 flowing text 처리를 해주는 것이 좋겠습니다.
        [SerializeField]
        private TextMeshProUGUI contentText = null;

        // NOTE: 가이드 퀘스트 보상 아이콘의 연출 스펙에 따라서 별도로 XxxItemView를 만들어서 사용합니다.
        [SerializeField]
        private List<VanillaItemView> rewards = null;

        [SerializeField]
        private Button bodyButton = null;

        // NOTE: 셀이 더해지고 빠지는 연출이 정해지면 더욱 개선됩니다.
        [SerializeField]
        private AnchoredPositionXTweener showingAndHidingTweener = null;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();

        public Nekoyume.Model.Quest.Quest Quest { get; private set; }

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
        }

        private void OnDisable()
        {
            showingAndHidingTweener.KillTween();
        }

        #endregion

        #region Controll

        public void ShowAsNew(Nekoyume.Model.Quest.Quest quest, bool ignoreAnimation = false)
        {
            if (quest is null)
            {
                return;
            }

            Quest = quest;

            SetContent(quest);

            if (ignoreAnimation)
            {
                SetRewards(quest.Reward.ItemMap, true);
            }
            else
            {
                ClearRewards();
                showingAndHidingTweener
                    .PlayTween()
                    .OnPlay(() => gameObject.SetActive(true))
                    .OnComplete(() => SetRewards(quest.Reward.ItemMap));
            }
        }

        public void HideAsClear(bool ignoreAnimation = false)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
            }
            else
            {
                showingAndHidingTweener
                    .PlayReverse()
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
            }

            Quest = null;
        }

        #endregion

        private void SetContent(Nekoyume.Model.Quest.Quest quest)
        {
            switch (quest)
            {
                default:
                    contentText.text = string.Format(SubContentFormat, quest.GetContent());
                    break;
                case WorldQuest _:
                    contentText.text = string.Format(MainContentFormat, quest.GetContent());
                    break;
            }
        }

        private void SetRewards(IReadOnlyDictionary<int, int> rewardMap, bool ignoreAnimation = false)
        {
            var sheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if (i < rewardMap.Count)
                {
                    var pair = rewardMap.ElementAt(i);
                    var row = sheet.OrderedList.FirstOrDefault(itemRow => itemRow.Id == pair.Key);
                    Assert.NotNull(row);

                    reward.SetData(row);

                    if (ignoreAnimation)
                    {
                        reward.Show();
                    }
                    else
                    {
                        // TODO: 바로 reward.Show()를 호출하지 말고 애니메이션을 재생합니다.
                        reward.Show();
                    }
                }
                else
                {
                    reward.Hide();
                }
            }
        }

        private void ClearRewards()
        {
            foreach (var reward in rewards)
            {
                reward.Hide();
            }
        }
    }
}
