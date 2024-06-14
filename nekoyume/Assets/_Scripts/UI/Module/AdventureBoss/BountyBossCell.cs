using Nekoyume.Action.AdventureBoss;
using Nekoyume.Data;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.TableData.AdventureBoss;
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

        public void SetData(AdventureBossWantedRewardSheet.Row data)
        {
            if(!TableSheets.Instance.AdventureBossSheet.TryGetValue(data.AdventureBossId, out var bossData))
            {
                NcDebug.LogError($"Not found boss data. bossId: {data.AdventureBossId}");
                gameObject.SetActive(false);
                return;
            }
            if (_bossId != bossData.BossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }
                _bossId = bossData.BossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconFace(_bossId), bossImageRoot);
                _bossImage.transform.localPosition = Vector3.zero;
            }

            var itemReward = data.FixedRewards.FirstOrDefault();

            switch(itemReward.ItemType)
            {
                case "Material":
                    baseItemView.ItemViewSetItemData(itemReward.ItemId, 0);
                    break;
                case "Rune":
                    baseItemView.ItemViewSetCurrencyData(itemReward.ItemId, 0);
                    break;
            }
        }
    }
}
