using System.Collections.Generic;
using System.Threading.Tasks;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
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
            var worldBossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            await foreach (var row in worldBossSheet.Values)
            {
                if (!WorldBossFrontHelper.TryGetRunes(row.BossId, out var runeRows))
                {
                    continue;
                }

                var runes = await GetRunes(runeRows);
                var item = new RuneInventoryItem(runes, row.BossId);
                items.Add(item);
            }

            scroll.UpdateData(items);
        }

        private async Task<List<FungibleAssetValue>> GetRunes(List<RuneSheet.Row> rows)
        {
            var address = States.Instance.CurrentAvatarState.address;
            var task = Task.Run(async () =>
            {
                var runes = new List<FungibleAssetValue>();
                await foreach (var row in rows)
                {
                    var rune = RuneHelper.ToCurrency(row, 0, null);
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
