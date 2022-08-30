using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
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
    using Cysharp.Threading.Tasks;
    using Nekoyume.TableData;
    using UniRx;

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
        private TextMeshProUGUI rankTitle;

        [SerializeField]
        private TextMeshProUGUI totalUsers;

        [SerializeField]
        private Button refreshButton;

        [SerializeField]
        private GameObject apiMissing;

        [SerializeField]
        private GameObject noSeasonInfo;

        [SerializeField]
        private Transform bossNameContainer;

        [SerializeField]
        private List<GameObject> queryLoadingObjects;

        [SerializeField]
        private WorldBossRankItemView myInfo;

        private readonly Dictionary<Status, WorldBossRankItems> _cachedItems = new();
        private Status _status;
        private GameObject _bossNameObject;
        private Address currentAvatarAddress;

        private void Awake()
        {
            refreshButton.OnClickAsObservable()
                .Subscribe(_ => RefreshAsync()).AddTo(gameObject);
        }

        public async void ShowAsync(Status status)
        {
            _status = status;
            ResetInformation(status);

            var raidId = GetRaidId(status);
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var raidRow))
            {
                if (_bossNameObject != null)
                {
                    Destroy(_bossNameObject);
                }
                noSeasonInfo.SetActive(true);
                bossImage.enabled = false;
                return;
            }

            if (!Game.Game.instance.ApiClient.IsInitialized)
            {
                apiMissing.SetActive(true);
                return;
            }

            SetActiveQueryLoading(true);
            await SetItemsAsync(raidRow, status);
            UpdateBossInformation(raidId);
            UpdateRecord(status);
            SetActiveQueryLoading(false);
        }

        private void RefreshAsync()
        {
            _cachedItems.Remove(_status);
            ShowAsync(_status);
        }

        private static int GetRaidId(Status status)
        {
            return status switch
            {
                Status.PreviousSeason => WorldBossFrontHelper.TryGetPreviousRow(
                    Game.Game.instance.Agent.BlockIndex, out var row)
                    ? row.Id
                    : 0,
                Status.Season => WorldBossFrontHelper.TryGetCurrentRow(
                    Game.Game.instance.Agent.BlockIndex, out var row)
                    ? row.Id
                    : 0,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }

        private void ResetInformation(Status status)
        {
            if (_bossNameObject != null)
            {
                Destroy(_bossNameObject);
            }

            rankTitle.text = status == Status.PreviousSeason
                ? L10nManager.Localize("UI_PREVIOUS_SEASON_RANK")
                : L10nManager.Localize("UI_LEADERBOARD");
            totalUsers.text = string.Empty;
            bossImage.enabled = false;
            scroll.UpdateData(new List<WorldBossRankItem>());
            myInfo.gameObject.SetActive(false);
            noSeasonInfo.SetActive(false);
            apiMissing.SetActive(false);
            SetActiveQueryLoading(false);
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

            if (_bossNameObject != null)
            {
                Destroy(_bossNameObject);
            }

            _bossNameObject = Instantiate(data.nameWithBackgroundPrefab, bossNameContainer);

            bossImage.enabled = true;
            bossImage.sprite = data.illustration;
        }

        private async Task SetItemsAsync(WorldBossListSheet.Row row, Status status)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (_cachedItems.ContainsKey(status) && _cachedItems[status].AvatarAddress == avatarAddress)
            {
                return;
            }

            var response = await WorldBossQuery.QueryRankingAsync(row.Id, avatarAddress);
            var records = response?.WorldBossRanking.RankingInfo ?? new List<WorldBossRankingRecord>();
            var userCount = response?.WorldBossTotalUsers ?? 0;
            var myRecord = records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());

            if (records.Count > LimitCount)
            {
                records = records.Where(record => record.Address != avatarAddress.ToHex())
                    .ToList();
            }

            if (Game.Game.instance.TableSheets
                .WorldBossCharacterSheet.TryGetValue(row.BossId, out var bossrow))
            {
                var items = new WorldBossRankItems(
                    records.Select(record => new WorldBossRankItem(bossrow, record)).ToList(),
                    myRecord != null ? new WorldBossRankItem(bossrow, myRecord) : null,
                    avatarAddress,
                    userCount);

                _cachedItems[status] = items;
            }
        }

        private void UpdateRecord(Status status)
        {
            var items = _cachedItems[status];
            myInfo.gameObject.SetActive(items.MyItem != null);
            myInfo.Set(items.MyItem, null);
            scroll.UpdateData(items.UserItems);
            totalUsers.text = items.UserCount > 0 ? $"{items.UserCount:#,0}" : string.Empty;;
        }

        private void SetActiveQueryLoading(bool value)
        {
            foreach (var o in queryLoadingObjects)
            {
                o.SetActive(value);
            }
        }
    }
}
