using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;
using System;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using GeneratedApiNamespace.ArenaServiceClient;
    using Nekoyume.ApiClient;
    using UniRx;

    public class ClanRankPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private ClanRankCell myInfo;

        [SerializeField]
        private ClanRankScroll clanRankScroll = null;

        [SerializeField]
        private GameObject preloadingObject = null;

        [SerializeField]
        private GameObject missingObject = null;

        [SerializeField]
        private TextMeshProUGUI missingText = null;

        public const int RankingBoardDisplayCount = 100;

        public override void Initialize()
        {
            base.Initialize();
            closeButton.onClick.AsObservable()
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            FillDataAsync().Forget();
        }

        public async UniTask FillDataAsync()
        {
            preloadingObject.SetActive(true);
            missingObject.SetActive(false);

            try
            {
                // 클랜 리더보드 데이터를 비동기로 가져옵니다.
                ClanLeaderboardResponse response = null;
                await ApiClients.Instance.Arenaservicemanager.Client.GetClansLeaderboardAsync(ArenaServiceManager.CreateCurrentJwt(),
                    on200: (result) =>
                    {
                        response = result;
                    });

                if (response != null)
                {
                    // 클랜 랭크 스크롤에 데이터를 설정합니다.
                    clanRankScroll.Show(response.Leaderboard, true);
                    myInfo.UpdateContent(response.MyClan);
                }
                else
                {
                    missingObject.SetActive(true);
                    missingText.text = L10nManager.Localize("UI_CLAN_RANK_DATA_NOT_FOUND");
                }
            }
            catch (Exception ex)
            {
                NcDebug.LogError(ex.Message);
                missingObject.SetActive(true);
                missingText.text = L10nManager.Localize("UI_CLAN_RANK_DATA_NOT_FOUND");
            }
            finally
            {
                preloadingObject.SetActive(false);
            }
        }
    }
}
