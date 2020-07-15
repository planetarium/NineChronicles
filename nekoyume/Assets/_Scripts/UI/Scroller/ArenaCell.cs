using System;
using FancyScrollView;
using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ArenaCell : FancyScrollRectCell<(int rank, ArenaInfo arenaInfo, bool canChallenge,
        bool isCurrentUser), MailScroll.ContextModel>
    {
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
        private TextMeshProUGUI scoreText = null;
        
        [SerializeField]
        private TextMeshProUGUI cpText = null;

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

        private RectTransform _rectTransformCache;
        private bool _isCurrentUser;

        public Action<ArenaCell> onClickChallenge;
        public Action<(RectTransform rectTransform, Address avatarAddress)> onClickInfo;

        public RectTransform RectTransformCache
        {
            get
            {
                if (!_rectTransformCache)
                {
                    _rectTransformCache = GetComponent<RectTransform>();
                }

                return _rectTransformCache;
            }
        }

        public ArenaInfo ArenaInfo { get; private set; }

        private void Awake()
        {
            challengeButton.OnSubmitClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    onClickChallenge?.Invoke(this);
                })
                .AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    onClickInfo?.Invoke((RectTransformCache, ArenaInfo.AvatarAddress));
                })
                .AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
        }

        public override void UpdateContent(
            (int rank, ArenaInfo arenaInfo, bool canChallenge, bool isCurrentUser) itemData)
        {
            var (rank, arenaInfo, canChallenge, isCurrentUser) = itemData;

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
                challengeCountText.text =
                    $"{arenaInfo.DailyChallengeCount}/{GameConfig.ArenaChallengeCountMax}";
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
