using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
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

        [SerializeField]
        private TextMeshProUGUI contentText = null;

        [SerializeField]
        private List<VanillaItemView> rewards = null;

        [SerializeField]
        private Button bodyButton = null;

        [SerializeField]
        private Image mainQuestImage = null;

        [SerializeField]
        private Image subQuestImage = null;

        public readonly ISubject<GuidedQuestCell> onClick = new Subject<GuidedQuestCell>();

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

        public void Show(Nekoyume.Model.Quest.Quest quest, bool ignoreAnimation = false)
        {
            if (quest is null)
            {
                return;
            }

            SetContent(quest);
            SetRewards(quest.Reward.ItemMap);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetContent(Nekoyume.Model.Quest.Quest quest)
        {
            mainQuestImage.gameObject.SetActive(false);
            subQuestImage.gameObject.SetActive(false);
            switch (quest)
            {
                default:
                    contentText.text = string.Format(SubContentFormat, quest.GetContent());
                    subQuestImage.gameObject.SetActive(true);
                    break;
                case WorldQuest _:
                    contentText.text = string.Format(MainContentFormat, quest.GetContent());
                    mainQuestImage.gameObject.SetActive(true);
                    break;
            }
        }

        private void SetRewards(IReadOnlyDictionary<int, int> rewardMap)
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
                    reward.Show();
                }
                else
                {
                    reward.Hide();
                }
            }
        }
    }
}
