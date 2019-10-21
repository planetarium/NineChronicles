using System;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingBoard : Widget
    {
        public RankingInfo rankingBase;
        public ScrollRect board;
        public GameObject filterHeader;
        public GameObject allHeader;
        public Text[] rankingButtonTexts;
        public Text[] filteredRankingButtonTexts;

        private Player _player;
        private AvatarState[] _avatarStates;

        protected override void Awake()
        {
            base.Awake();

            foreach (var rankingButtonText in rankingButtonTexts)
            {
                rankingButtonText.text = LocalizationManager.Localize("UI_OVERALL_RANKING");
            }

            foreach (var filteredRankingButtonText in filteredRankingButtonTexts)
            {
                filteredRankingButtonText.text = LocalizationManager.Localize("UI_RANKING_IN_24HOURS");
            }
        }

        public override void Show()
        {
            base.Show();

            var stage = Game.Game.instance.stage;
            stage.LoadBackground("ranking");
            stage.GetPlayer().gameObject.SetActive(false);
            // Call from animation GetFilteredRanking();
            
            Find<BottomMenu>()?.Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick, true);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>()?.Close();
            
            _avatarStates = null;
            ClearBoard();

            base.Close(ignoreCloseAnimation);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        private void GetAvatars(DateTimeOffset? dt)
        {
            var rankingBoard = (RankingState) Game.Game.instance.agent.GetState(RankingState.Address);
            Debug.LogWarningFormat("rankingBoard: {0}", rankingBoard);
            _avatarStates = rankingBoard?.GetAvatars(dt) ?? new AvatarState[0];
        }

        private void UpdateBoard(DateTimeOffset? dt)
        {
            ClearBoard();
            GetAvatars(dt);
            for (var index = 0; index < _avatarStates.Length; index++)
            {
                var avatarState = _avatarStates[index];
                if (avatarState == null)
                {
                    continue;
                }

                RankingInfo rankingInfo = Instantiate(rankingBase, board.content);
                var bg = rankingInfo.GetComponent<Image>();
                if (index % 2 == 1)
                {
                    bg.enabled = false;
                }

                rankingInfo.Set(index + 1, avatarState);
                rankingInfo.gameObject.SetActive(true);
            }
        }

        public void GetFilteredRanking()
        {
            filterHeader.SetActive(true);
            allHeader.SetActive(false);
            UpdateBoard(DateTimeOffset.UtcNow);
        }

        public void GetRanking()
        {
            filterHeader.SetActive(false);
            allHeader.SetActive(true);
            UpdateBoard(null);
        }

        private void ClearBoard()
        {
            foreach (Transform child in board.content.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            Close();
            Find<Menu>().ShowRoom();
        }
    }
}
