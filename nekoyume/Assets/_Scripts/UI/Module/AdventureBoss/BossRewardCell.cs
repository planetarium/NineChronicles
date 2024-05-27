using Codice.CM.WorkspaceServer;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.TableData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BossRewardCell : MonoBehaviour
    {
        [SerializeField] private Image bossImg;
        [SerializeField] private BaseItemView confirmRewardItemView;
        [SerializeField] private BaseItemView[] randomRewardItemViews;

        public void SetData(AdventureBossReward adventureBossReward)
        {
            RuneSheet runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            bossImg.sprite = SpriteHelper.GetBigCharacterIcon(adventureBossReward.BossId);
            bossImg.SetNativeSize();
            if(adventureBossReward.wantedReward.FixedRewardItemIdDict.Count > 0)
            {
                confirmRewardItemView.ItemViewSetItemData(adventureBossReward.wantedReward.FixedRewardItemIdDict.First().Key,0);
            }
            else if(adventureBossReward.wantedReward.FixedRewardFavIdDict.Count > 0)
            {
                runeSheet.TryGetValue(adventureBossReward.wantedReward.FixedRewardFavIdDict.First().Key, out var runeRow);
                if (runeRow != null)
                {
                    confirmRewardItemView.ItemViewSetCurrencyData(runeRow.Ticker, 1);
                }
            }
            else
            {
                confirmRewardItemView.gameObject.SetActive(false);
            }

            int i = 0;
            foreach (var randomReward in adventureBossReward.wantedReward.RandomRewardItemIdDict)
            {
                randomRewardItemViews[i].ItemViewSetItemData(randomReward.Key, 0);
                i++;
            }
            foreach (var randomReward in adventureBossReward.wantedReward.RandomRewardFavTickerDict)
            {
                runeSheet.TryGetValue(randomReward.Key, out var runeRow);
                if (runeRow != null)
                {
                    randomRewardItemViews[i].ItemViewSetCurrencyData(runeRow.Ticker, 1);
                    i++;
                }
            }
            for (; i < randomRewardItemViews.Length; i++)
            {
                randomRewardItemViews[i].gameObject.SetActive(false);
            }
        }
    }
}
