using System;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
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

        private Stage _stage;
        private Player _player;
        private Nekoyume.Model.Avatar[] _avatars;

        protected override void Awake()
        {
            base.Awake();

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
            GetFilteredRanking();
        }

        public override void Close()
        {
            _avatars = null;
            ClearBoard();

            Find<Status>()?.Show();
            Find<Menu>()?.Show();
            _stage.LoadBackground("room");
            _stage.GetPlayer(_stage.RoomPosition);
            _player.gameObject.SetActive(true);

            base.Close();
        }

        private void GetAvatars(DateTimeOffset? dt)
        {
            _avatars = ActionManager.instance.rankingBoard.GetAvatars(dt);
        }

        private void UpdateBoard(DateTimeOffset? dt)
        {
            ClearBoard();
            GetAvatars(dt);
            for (var index = 0; index < _avatars.Length; index++)
            {
                var avatar = _avatars[index];
                if (avatar != null)
                {
                    RankingInfo rankingInfo = Instantiate(rankingBase, board.content);
                    var bg = rankingInfo.GetComponent<Image>();
                    if (index % 2 == 1)
                    {
                        bg.enabled = false;
                    }
                    rankingInfo.Set(index +1, avatar);
                    rankingInfo.gameObject.SetActive(true);
                }
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
