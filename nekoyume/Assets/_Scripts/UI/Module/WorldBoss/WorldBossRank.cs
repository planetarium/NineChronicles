using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
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
        private TextMeshProUGUI lastUpdatedText;

        [SerializeField]
        private Button refreshButton;

        [SerializeField]
        private GameObject refreshBlocker;

        [SerializeField]
        private GameObject apiMissing;

        [SerializeField]
        private GameObject noSeasonInfo;

        [SerializeField]
        private GameObject updateContainer;

        [SerializeField]
        private Transform bossNameContainer;

        [SerializeField]
        private List<GameObject> queryLoadingObjects;

        [SerializeField]
        private WorldBossRankItemView myInfo;

        [SerializeField]
        private GameObject myInfoObject;

        [SerializeField]
        private GameObject noneObject;

        private readonly Dictionary<Status, WorldBossRankItems> _cachedItems = new();
        private Status _status;
        private GameObject _bossNameObject;
        private Address _currentAvatarAddress;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            refreshButton.OnClickAsObservable()
                .Where(_=> !refreshBlocker.activeSelf)
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
                updateContainer.SetActive(false);
                bossImage.enabled = false;
                return;
            }

            if (!Game.Game.instance.ApiClient.IsInitialized)
            {
                apiMissing.SetActive(true);
                updateContainer.SetActive(false);
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
            lastUpdatedText.text = string.Empty;
            bossImage.enabled = false;
            scroll.UpdateData(new List<WorldBossRankItem>());
            myInfoObject.gameObject.SetActive(false);
            noneObject.gameObject.SetActive(false);
            noSeasonInfo.SetActive(false);
            apiMissing.SetActive(false);
            updateContainer.SetActive(true);
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
            var blockIndex = response?.WorldBossRanking?.BlockIndex ?? 0;
            var myRecord = records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());

            if (records.Count > LimitCount)
            {
                records = records.Where(record => record.Address != avatarAddress.ToHex())
                    .ToList();
            }


            if (Game.Game.instance.TableSheets
                .WorldBossCharacterSheet.TryGetValue(row.BossId, out var bossRow))
            {
                var items = new WorldBossRankItems(
                records.Select(record => new WorldBossRankItem(bossRow, record)).ToList(),
                myRecord != null ? new WorldBossRankItem(bossRow, myRecord) : null,
                avatarAddress,
                blockIndex,
                userCount);

                _cachedItems[status] = items;
            }
        }

        private void UpdateRecord(Status status)
        {
            var items = _cachedItems[status];
            myInfoObject.SetActive(items.MyItem != null);
            noneObject.gameObject.SetActive(items.MyItem == null);
            myInfo.Set(items.MyItem, null);
            _disposables.DisposeAllAndClear();
            scroll.UpdateData(items.UserItems);
            scroll.OnClick.Subscribe(x =>
            {
                ShowAsync(x.Address).Forget();
            }).AddTo(_disposables);
            totalUsers.text = items.UserCount > 0 ? $"{items.UserCount:#,0}" : string.Empty;;
            lastUpdatedText.text = $"{items.LastUpdatedBlockIndex:#,0}";
        }

        private async UniTaskVoid ShowAsync(string addressString)
        {
            var address = new Address(addressString);
            var avatarState = (await Game.Game.instance.Agent.GetAvatarStatesAsync(
                new[] { address }))[address];
            var popup = Widget.Find<FriendInfoPopup>();
            if (popup.isActiveAndEnabled)
            {
                popup.Close(true);
            }
            popup.ShowAsync(avatarState, BattleType.Raid).Forget();
        }


        private void SetActiveQueryLoading(bool value)
        {
            refreshBlocker.SetActive(value);
            foreach (var o in queryLoadingObjects)
            {
                o.SetActive(value);
            }
        }
    }
}
