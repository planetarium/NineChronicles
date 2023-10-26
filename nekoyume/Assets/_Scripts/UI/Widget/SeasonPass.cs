using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Nekoyume.UI.Module;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SeasonPass : Widget
    {
        [SerializeField]
        private ConditionalButton receiveBtn;
        [SerializeField]
        private Animator lastRewardAnim;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI remainingText;
        [SerializeField]
        private TextMeshProUGUI expText;
        [SerializeField]
        private SeasonPassRewardCell[] rewardCells;
        [SerializeField]
        private Image lineImage;

        protected override void Awake()
        {
            base.Awake();
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarInfo.Subscribe((seasonPassInfo) => {
                if(seasonPassInfo == null)
                    return;

                levelText.text = seasonPassInfo.Level.ToString();
                seasonPassManager.GetExp(seasonPassInfo.Level, out var minExp, out var maxExp);
                expText.text = $"{seasonPassInfo.Exp - minExp} / {maxExp - minExp}";

                receiveBtn.Interactable = seasonPassInfo.Level > seasonPassInfo.LastNormalClaim;

                lineImage.fillAmount = (float)(seasonPassInfo.Level - 1f) / (float)seasonPassManager.CurrentSeasonPassData.RewardList.Count;
            }).AddTo(gameObject);

            seasonPassManager.RemainingDateTime.Subscribe((endDate) =>
            {
                remainingText.text = $"Remaining Time <Style=Clock> {endDate}";
            });

            seasonPassManager.SeasonEndDate.Subscribe((endTime) =>
            {
                for (int i = 0; i < rewardCells.Length; i++)
                {
                    if(i < seasonPassManager.CurrentSeasonPassData.RewardList.Count)
                    {
                        rewardCells[i].gameObject.SetActive(true);
                        rewardCells[i].SetData(seasonPassManager.CurrentSeasonPassData.RewardList[i]);
                    }
                    else
                    {
                        rewardCells[i].gameObject.SetActive(false);
                    }
                }
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            lastRewardAnim.Play("SeasonPassLastReward@Close", -1, 1);

            base.Show(ignoreShowAnimation);

            /*lastRewardAnim.SetBool("Show", false);*/
        }


        public void ReceiveAllBtn()
        {
            Game.Game.instance.SeasonPassServiceManager.ReceiveAll((result) =>
            {

            }, null);
        }
    }
}
