using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRank : WorldBossDetailItem
    {
        public enum Status
        {
            PreviousSeason,
            Season,
        }

        private const int LimitCount = 100;

        [SerializeField]
        private WorldBossRankScroll scroll;

        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private TextMeshProUGUI bossName;

        [SerializeField]
        private TextMeshProUGUI rankTitle;

        [SerializeField]
        private TextMeshProUGUI totalUsers;

        [SerializeField]
        private GameObject apiMissing;

        [SerializeField]
        private GameObject loadingIndicator;

        [SerializeField]
        private WorldBossRankItemView myInfo;

        private readonly Dictionary<Status, WorldBossRankItems> _cachedItems = new();

        public async void ShowAsync(int raidId, Status status)
        {
            Reset(status);

            var apiClient = Game.Game.instance.ApiClient;
            if (!apiClient.IsInitialized)
            {
                apiMissing.SetActive(true);
                return;
            }

            // for test
            // var records = new List<WorldBossRankingRecord>();
            // for (var i = 0; i < 100; i++)
            // {
            //     var record = new WorldBossRankingRecord
            //     {
            //         Ranking = i+1,
            //         Level =  i+1,
            //         Cp = i * 100,
            //         IconId = 10210000,
            //         AvatarName = $"{i}+{i}",
            //         HighScore = i * 1000,
            //         TotalScore = i * 10000,
            //     };
            //     records.Add(record);
            // }
            //
            // if (raidId == 0)
            // {
            //     records.Clear();
            // }

            loadingIndicator.SetActive(true);
            var finish = await SetItemsAsync(raidId, status);
            UpdateBossInformation(raidId);
            UpdateRecord(status);
            loadingIndicator.SetActive(false);
        }

        private void Reset(Status status)
        {
            rankTitle.text = status == Status.PreviousSeason
                ? $"{L10nManager.Localize("UI_PREVIOUS")} {L10nManager.Localize("UI_RANK")}"
                : L10nManager.Localize("UI_RANK");
            bossName.text = "-";
            totalUsers.text = $"-";
            bossImage.enabled = false;
            myInfo.gameObject.SetActive(false);
        }

        private void UpdateBossInformation(int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetBossData(row.BossId, out var data))
            {
                return;
            }

            bossName.text = data.name;
            bossImage.enabled = true;
            bossImage.sprite = data.illustration;
        }

        private async Task<bool> SetItemsAsync(int raidId, Status status)
        {
            if (_cachedItems.ContainsKey(status))
            {
                return true;
                // todo : refresh 기능 추가해야함
                // _cachedItems.Add(status, items);
            }


            var avatarState = States.Instance.CurrentAvatarState;
            var response = await WorldBossQuery.QueryRankingAsync(raidId, avatarState.address);
            var records = response?.WorldBossRanking ?? new List<WorldBossRankingRecord>();
            var userCount = response?.WorldBossTotalUsers ?? 0;

            var avatarAddress = "C54d5b047bb87bd4F71af42456ac2d499FBCe767"; // for test
            // var avatarAddress = avatarState.address.ToHex();
            var myRecord = records.FirstOrDefault(record => record.Address == avatarAddress);

            if (records.Count > LimitCount)
            {
                records = records.Where(record => record.Address != avatarAddress)
                    .ToList();
            }

            var items = new WorldBossRankItems(
                records.Select(record => new WorldBossRankItem(record)).ToList(),
                myRecord != null ? new WorldBossRankItem(myRecord) : null,
                userCount);

            _cachedItems[status] = items;
            return true;
        }

        private void UpdateRecord(Status status)
        {
            var items = _cachedItems[status];
            myInfo.gameObject.SetActive(items.MyItem != null);
            myInfo.Set(items.MyItem, null);
            scroll.UpdateData(items.UserItems);
            totalUsers.text = items.UserCount > 0 ? $"{items.UserCount:#,0}" : "-";
        }
    }
}
