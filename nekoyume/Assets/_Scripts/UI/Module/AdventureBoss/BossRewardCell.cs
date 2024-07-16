using Nekoyume.ActionExtensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.TableData.AdventureBoss;
using System.Linq;
using UnityEngine;

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
            if (!TableSheets.Instance.AdventureBossWantedRewardSheet.TryGetValue(adventureBossRow.Id, out var wantedRewardRow))
            {
                NcDebug.LogError($"AdventureBossWantedRewardSheet not found id:{adventureBossRow.Id}");
                return;
            }

            var fixedReward = wantedRewardRow.FixedReward;
            if (fixedReward == null)
            {
                confirmRewardItemView.gameObject.SetActive(false);
            }
            else
            {
                confirmRewardItemView.ItemViewSetAdventureBossItemData(fixedReward);
            }

            var rendomRewards = wantedRewardRow.RandomRewards;
            for (var i = 0; i < randomRewardItemViews.Length; i++)
            {
                if (i < rendomRewards.Count)
                {
                    var randomReward = rendomRewards[i];
                    randomRewardItemViews[i].ItemViewSetAdventureBossItemData(randomReward);
                }
                else
                {
                    randomRewardItemViews[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
