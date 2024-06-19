using Nekoyume.ActionExtensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.TableData.AdventureBoss;
using System.Linq;
using UnityEngine;

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
            if (!TableSheets.Instance.AdventureBossSheet.TryGetValue(data.AdventureBossId, out var bossData))
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

            var itemReward = data.FixedReward;
            baseItemView.gameObject.SetActive(true);
            baseItemView.ItemViewSetAdventureBossItemData(itemReward);
        }
    }
}
