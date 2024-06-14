using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.TableData.AdventureBoss;
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

        public void SetData(AdventureBossSheet.Row adventureBossRow)
        {
            SetBossData(adventureBossRow.BossId);
            if(!TableSheets.Instance.AdventureBossWantedRewardSheet.TryGetValue(adventureBossRow.Id,out var wantedRewardRow))
            {
                NcDebug.LogError($"AdventureBossWantedRewardSheet not found id:{adventureBossRow.Id}");
                return;
            }
            var fixedReward = wantedRewardRow.FixedRewards.First();
            if(fixedReward == null)
            {
                confirmRewardItemView.gameObject.SetActive(false);
            }
            else
            {
                switch (fixedReward.ItemType)
                {
                    case "Material":
                        confirmRewardItemView.ItemViewSetItemData(fixedReward.ItemId, 0);
                        break;
                    case "Rune":
                        confirmRewardItemView.ItemViewSetCurrencyData(fixedReward.ItemId, 0);
                        break;
                }
            }
            var rendomRewards = wantedRewardRow.RandomRewards;
            for (int i = 0; i < randomRewardItemViews.Length; i++)
            {
                if (i < rendomRewards.Count)
                {
                    var randomReward = rendomRewards[i];
                    switch (randomReward.ItemType)
                    {
                        case "Material":
                            randomRewardItemViews[i].ItemViewSetItemData(randomReward.ItemId, 0);
                            break;
                        case "Rune":
                            randomRewardItemViews[i].ItemViewSetCurrencyData(randomReward.ItemId, 0);
                            break;
                    }
                }
                else
                {
                    randomRewardItemViews[i].gameObject.SetActive(false);
                }                
            }
        }
    }
}
