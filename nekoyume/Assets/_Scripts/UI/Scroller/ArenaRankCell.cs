using System;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankCell : RectCell<
        ArenaRankCell.ViewModel,
        ArenaRankScroll.ContextModel>
    {
        public class ViewModel
        {
            public int rank;
            public ArenaInfo arenaInfo;
            public ArenaInfo currentAvatarArenaInfo;
        }

        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField]
        private bool controlBackgroundImage = false;

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
        private readonly Subject<ArenaRankCell> _onClickAvatarInfo = new Subject<ArenaRankCell>();
        private readonly Subject<ArenaRankCell> _onClickChallenge = new Subject<ArenaRankCell>();

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        public ArenaInfo ArenaInfo { get; private set; }

        public IObservable<ArenaRankCell> OnClickAvatarInfo => _onClickAvatarInfo;

        public IObservable<ArenaRankCell> OnClickChallenge => _onClickChallenge;

        private void Awake()
        {
            avatarInfoButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickAvatarInfo.OnNext(this);
                    _onClickAvatarInfo.OnNext(this);
                })
                .AddTo(gameObject);

            challengeButton.OnSubmitClick
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickChallenge.OnNext(this);
                    _onClickChallenge.OnNext(this);
                })
                .AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
        }

        public void Show((
            int rank,
            ArenaInfo arenaInfo,
            ArenaInfo currentAvatarArenaInfo) itemData)
        {
            Show(new ViewModel
            {
                rank = itemData.rank,
                arenaInfo = itemData.arenaInfo,
                currentAvatarArenaInfo = itemData.currentAvatarArenaInfo
            });
        }

        public override void UpdateContent(ViewModel itemData)
        {
            var rank = itemData.rank;
            var arenaInfo = itemData.arenaInfo;
            var currentAvatarArenaInfo = itemData.currentAvatarArenaInfo;

            ArenaInfo = arenaInfo ?? throw new ArgumentNullException(nameof(arenaInfo));
            _isCurrentUser = States.Instance.CurrentAvatarState?.address == ArenaInfo.AvatarAddress;

            if (controlBackgroundImage)
            {
                backgroundImage.enabled = Index % 2 == 1;
            }

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
                challengeButton.SetSubmittable(!(currentAvatarArenaInfo is null) &&
                                               currentAvatarArenaInfo.Active &&
                                               currentAvatarArenaInfo.DailyChallengeCount > 0);
            }

            tweenMove.StartDelay = rank * 0.16f;
            tweenAlpha.StartDelay = rank * 0.16f;
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
