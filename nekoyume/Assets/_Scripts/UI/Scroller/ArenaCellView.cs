using System;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI.Scroller
{
    public class ArenaCellView : MonoBehaviour
    {
        public Action<ArenaCellView> onClickChallenge;
        public Action<(RectTransform rectTransform, Address avatarAddress)> onClickInfo;

        [SerializeField]
        private GameObject rankImageContainer = null;
        [SerializeField]
        private Image rankImage = null;
        [SerializeField]
        private GameObject rankTextContainer = null;
        [SerializeField]
        private TextMeshProUGUI rankText = null;
        [SerializeField]
        private FramedCharacterView characterView = null;
        [SerializeField]
        private TextMeshProUGUI levelText = null;
        [SerializeField]
        private TextMeshProUGUI nameText = null;
        [SerializeField]
        private TextMeshProUGUI cpText = null;
        [SerializeField]
        private TextMeshProUGUI scoreText = null;
        [SerializeField]
        private GameObject challengeCountTextContainer = null;
        [SerializeField]
        private TextMeshProUGUI challengeCountText = null;
        [SerializeField]
        private Button avatarInfoButton = null;
        [SerializeField]
        private SubmitButton challengeButton = null;

        [SerializeField]
        private DOTweenRectTransformMoveBy tweenMove = null;
        [SerializeField]
        private DOTweenGroupAlpha tweenAlpha = null;

        private RectTransform _rectTransform;
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

        private bool _isCurrentUser;

        public ArenaInfo ArenaInfo { get; private set; }

        private void Awake()
        {
            challengeButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickChallenge?.Invoke(this);
            }).AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickInfo?.Invoke((RectTransform, ArenaInfo.AvatarAddress));
            }).AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int rank, ArenaInfo arenaInfo, bool canChallenge, bool isCurrentUser)
        {
            ArenaInfo = arenaInfo ?? throw new ArgumentNullException(nameof(arenaInfo));
            _isCurrentUser = isCurrentUser;

            UpdateRank(rank);
            levelText.text = arenaInfo.Level.ToString();
            nameText.text = arenaInfo.AvatarName;
            cpText.text = arenaInfo.CombatPoint.ToString();
            scoreText.text = arenaInfo.Score.ToString();
            challengeCountTextContainer.SetActive(_isCurrentUser);
            challengeButton.gameObject.SetActive(!_isCurrentUser);

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

                rank = 1;
                challengeCountText.text = $"{arenaInfo.DailyChallengeCount}/{GameConfig.ArenaChallengeCountMax}";
            }
            else
            {
                characterView.SetByAvatarAddress(arenaInfo.AvatarAddress);
                challengeButton.SetSubmittable(canChallenge);
            }

            tweenMove.StartDelay = rank * 0.16f;
            tweenAlpha.StartDelay = rank * 0.16f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateRank(int rank)
        {
            switch (rank)
            {
                case 1:
                case 2:
                case 3:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(false);
                    rankImage.overrideSprite = SpriteHelper.GetRankIcon(rank);
                    break;
                default:
                    rankImageContainer.SetActive(false);
                    rankTextContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }
        }
    }
}
