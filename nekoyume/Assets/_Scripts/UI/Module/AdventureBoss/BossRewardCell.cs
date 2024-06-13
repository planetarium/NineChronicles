using Nekoyume.Helper;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Nekoyume.Data.AdventureBossGameData;

namespace Nekoyume
{
    public class BossRewardCell : MonoBehaviour
    {
        [SerializeField] private Transform bossImgRoot;
        [SerializeField] private BaseItemView confirmRewardItemView;
        [SerializeField] private BaseItemView[] randomRewardItemViews;

        private int _bossId;
        private GameObject _bossImage;
        private void SetBossData(int bossId)
        {
            if (_bossId != bossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }

                _bossId = bossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconFace(_bossId),
                    bossImgRoot);
                _bossImage.transform.localPosition = Vector3.zero;
            }
        }

        public void SetData(AdventureBossReward adventureBossReward)
        {
            SetBossData(adventureBossReward.BossId);
            if (adventureBossReward.wantedReward.FixedRewardItemIdDict.Count > 0)
            {
                confirmRewardItemView.ItemViewSetItemData(adventureBossReward.wantedReward.FixedRewardItemIdDict.First().Key,0);
            }
            else if(adventureBossReward.wantedReward.FixedRewardFavIdDict.Count > 0)
            {
                confirmRewardItemView.ItemViewSetCurrencyData(adventureBossReward.wantedReward.FixedRewardFavIdDict.First().Key, 1);
            }
            else
            {
                confirmRewardItemView.gameObject.SetActive(false);
            }

            int i = 0;
            foreach (var randomReward in adventureBossReward.wantedReward.RandomRewardItemIdDict)
            {
                if(i < randomRewardItemViews.Length)
                {
                    randomRewardItemViews[i].ItemViewSetItemData(randomReward.Key, 0);
                    i++;
                }
            }
            foreach (var randomReward in adventureBossReward.wantedReward.RandomRewardFavTickerDict)
            {
                if (i < randomRewardItemViews.Length)
                {
                    if (randomRewardItemViews[i].ItemViewSetCurrencyData(randomReward.Key, 1))
                    {
                        i++;
                    }
                }
            }
            for (; i < randomRewardItemViews.Length; i++)
            {
                randomRewardItemViews[i].gameObject.SetActive(false);
            }
        }
    }
}
