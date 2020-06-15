using System;
using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankingInfo : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;
        [SerializeField]
        private TextMeshProUGUI rank = null;
        [SerializeField]
        private FramedCharacterView characterView = null;
        [SerializeField]
        private TextMeshProUGUI level = null;
        [SerializeField]
        private TextMeshProUGUI id = null;
        [SerializeField]
        private TextMeshProUGUI stage = null;
        [SerializeField]
        private Tween.DOTweenRectTransformMoveBy tweenMove = null;
        [SerializeField]
        private Tween.DOTweenGroupAlpha tweenAlpha = null;

        public Action<(RectTransform rectTransform, Address avatarAddress)> onClick;

        private RectTransform _rectTransform;
        private bool _isCurrentUser;

        public RectTransform RectTransform
        {
            get
            {
                if (!_rectTransform)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        public Nekoyume.Model.State.RankingInfo AvatarInfo { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClick?.Invoke((RectTransform, AvatarInfo.AvatarAddress));
            }).AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
        }

        public void Set(int ranking, Nekoyume.Model.State.RankingInfo rankingInfo, bool isCurrentUser)
        {
            AvatarInfo = rankingInfo ?? throw new ArgumentNullException(nameof(rankingInfo));
            _isCurrentUser = isCurrentUser;

            rank.text = ranking.ToString();
            level.text = AvatarInfo.Level.ToString();
            id.text = AvatarInfo.AvatarName;
            stage.text = AvatarInfo.Exp.ToString();

            if (_isCurrentUser)
            {
                var player = Game.Game.instance.Stage.selectedPlayer;
                if (player is null)
                {
                    player = Game.Game.instance.Stage.GetPlayer();
                    characterView.SetByPlayer(player);
                    player.gameObject.SetActive(false);
                }
                else
                {
                    characterView.SetByPlayer(player);
                }
            }
            else
            {
                characterView.SetByAvatarAddress(AvatarInfo.AvatarAddress);
            }

            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
        }
    }
}
