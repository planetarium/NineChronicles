using Nekoyume.Action.AdventureBoss;
using Nekoyume.Data;
using Nekoyume.Helper;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Nekoyume.Data.AdventureBossGameData;

namespace Nekoyume
{
    public class BountyBossCell : MonoBehaviour
    {
        [SerializeField]
        private Transform bossImageRoot;
        [SerializeField]
        private BaseItemView baseItemView;

        private int _bossId;
        private GameObject _bossImage;

        public void SetData(AdventureBossReward data)
        {
            if (_bossId != data.BossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }
                _bossId = data.BossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconFace(_bossId), bossImageRoot);
                _bossImage.transform.localPosition = Vector3.zero;
            }

            if(data.wantedReward.FixedRewardItemIdDict.Count > 0)
            {
                var item = data.wantedReward.FixedRewardItemIdDict.First();
                baseItemView.ItemViewSetItemData(item.Key, 0);
            }
            if(data.wantedReward.FixedRewardFavIdDict.Count > 0)
            {
                var item = data.wantedReward.FixedRewardFavIdDict.First();
                baseItemView.ItemViewSetCurrencyData(item.Key, 0);
            }
        }
    }
}
