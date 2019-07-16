using System;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
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

        private Stage _stage;
        private Player _player;
        private AvatarState[] _avatarStates;

        protected override void Awake()
        {
            base.Awake();

            foreach (var rankingButtonText in rankingButtonTexts)
            {
                rankingButtonText.text = LocalizationManager.Localize("UI_ALL_USERS");    
            }

            foreach (var filteredRankingButtonText in filteredRankingButtonTexts)
            {
                filteredRankingButtonText.text = LocalizationManager.Localize("UI_USERS_WHO_CONNECTED_WITHIN_24HOURS");    
            }
            
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
        }

        public override void Show()
        {
            base.Show();

            _stage.LoadBackground("ranking");
            _player = _stage.GetPlayer();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _player.gameObject.SetActive(false);
            // Call from animation GetFilteredRanking(); 

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
        }

        public override void Close()
        {
            _avatarStates = null;
            ClearBoard();

            Find<Menu>()?.ShowRoom();
            _stage.LoadBackground("room");
            _stage.GetPlayer(_stage.roomPosition);
            _player.gameObject.SetActive(true);

            base.Close();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        private void GetAvatars(DateTimeOffset? dt)
        {
            var rankingBoard = States.Instance.rankingState.Value;
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
                rankingInfo.Set(index +1, avatarState);
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
    }
}
