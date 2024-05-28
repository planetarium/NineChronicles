using Nekoyume.Action.AdventureBoss;
using Nekoyume.Helper;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BountyBossCell : MonoBehaviour
    {
        [SerializeField]
        private Image bossImage;
        [SerializeField]
        private BaseItemView baseItemView;
        public void SetData(AdventureBossReward data)
        {
            bossImage.sprite = SpriteHelper.GetBigCharacterIcon(data.BossId);
            bossImage.SetNativeSize();
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
