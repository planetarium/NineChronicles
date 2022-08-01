using System.Collections.Generic;
using System.Threading.Tasks;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRuneInventory : WorldBossDetailItem
    {
        [SerializeField]
        private RuneInventoryScroll scroll;

        public async void ShowAsync()
        {
            var items = new List<RuneInventoryItem>();

            var runes1 = await GetRunes(new List<int>() { 800000, 800001, 800002});
            var item1 = new RuneInventoryItem(runes1, 205007);
            items.Add(item1);

            var runes2 = await GetRunes(new List<int>() { 800010, 800011, 800012});
            var item2 = new RuneInventoryItem(runes2, 203007);
            items.Add(item2);
            scroll.UpdateData(items);
        }

        private async Task<List<FungibleAssetValue>> GetRunes(List<int> runeIds)
        {
            var address = States.Instance.CurrentAvatarState.address;
            var task = Task.Run(async () =>
            {
                var runes = new List<FungibleAssetValue>();
                await foreach (var runeId in runeIds)
                {
                    var rune = RuneHelper.ToCurrency(runeId);
                    var fungibleAsset = await Game.Game.instance.Agent.GetBalanceAsync(address, rune);
                    runes.Add(fungibleAsset);
                }

                return runes;
            });

            await task;
            return task.Result;
        }
    }
}
