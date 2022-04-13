using System;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class ArenaRankCell : RectCell<
        ArenaRankCell.ViewModel,
        ArenaRankScroll.ContextModel>
    {
        public class ViewModel
        {
            public int rank;
            public ArenaInfo arenaInfo;
            public ArenaInfo currentAvatarArenaInfo;
            public bool currentAvatarCanBattle;
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
        private DetailedCharacterView characterView = null;

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
        private ConditionalButton challengeButton = null;

        private RectTransform _rectTransformCache;
        private bool _isCurrentUser;
        private ViewModel _viewModel;
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
            characterView.OnClickCharacterIcon
                .Subscribe(async avatarState =>
                {
                    if (avatarState is null)
                    {
                        var (exist, state) =
                            await States.TryGetAvatarStateAsync(ArenaInfo.AvatarAddress);
                        avatarState = exist ? state : null;
                        if (avatarState is null)
                        {
                            return;
                        }
                    }

                    Widget.Find<FriendInfoPopup>().Show(avatarState);
                })
                .AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickAvatarInfo.OnNext(this);
                    _onClickAvatarInfo.OnNext(this);
                })
                .AddTo(gameObject);

            challengeButton.OnSubmitSubject
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClickChallenge.OnNext(this);
                    _onClickChallenge.OnNext(this);
                })
                .AddTo(gameObject);

            challengeButton.OnClickDisabledSubject
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_EQUIP_FAILED"),
                        NotificationCell.NotificationType.Alert);
                }).AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(player =>
                {
                    characterView.SetByPlayer(player);
                    cpText.text = CPHelper.GetCPV2(
                        States.Instance.CurrentAvatarState,
                        Game.Game.instance.TableSheets.CharacterSheet,
                        Game.Game.instance.TableSheets.CostumeStatSheet).ToString();
                })
                .AddTo(gameObject);
        }

        public void Show((
            int rank,
            ArenaInfo arenaInfo,
            ArenaInfo currentAvatarArenaInfo,
            bool currentAvatarCanBattle) itemData)
        {
            Show(new ViewModel
            {
                rank = itemData.rank,
                arenaInfo = itemData.arenaInfo,
                currentAvatarArenaInfo = itemData.currentAvatarArenaInfo,
                currentAvatarCanBattle = itemData.currentAvatarCanBattle,
            });
        }

        private void Start()
        {
            Context?.UpdateConditionalStateOfChallengeButtons
                .Subscribe(UpdateChallengeButton)
                .AddTo(gameObject);
        }

        public void ShowMyDefaultInfo()
        {
            UpdateRank(-1);

            var currentAvatarState = States.Instance.CurrentAvatarState;
            characterView.SetByAvatarState(currentAvatarState);
            nameText.text = currentAvatarState.NameWithHash;
            scoreText.text = "-";
            cpText.text = "-";

            challengeCountTextContainer.SetActive(true);
            challengeButton.gameObject.SetActive(false);
            challengeCountText.text =
                $"<color=orange>{GameConfig.ArenaChallengeCountMax}</color>/{GameConfig.ArenaChallengeCountMax}";
        }

        public override void UpdateContent(ViewModel itemData)
        {
            _viewModel = itemData;
            if (_viewModel is null)
            {
                Debug.LogError($"Argument is null. {nameof(itemData)}");
                return;
            }

            ArenaInfo = _viewModel.arenaInfo ?? throw new ArgumentNullException(nameof(_viewModel.arenaInfo));
            var currentAvatarArenaInfo = _viewModel.currentAvatarArenaInfo;
            _isCurrentUser = currentAvatarArenaInfo is { } &&
                             ArenaInfo.AvatarAddress == currentAvatarArenaInfo.AvatarAddress;

            if (controlBackgroundImage)
            {
                backgroundImage.enabled = Index % 2 == 1;
            }

            UpdateRank(_viewModel.rank);
            nameText.text = ArenaInfo.AvatarName;
            scoreText.text = ArenaInfo.Score.ToString();
            cpText.text = GetCP(ArenaInfo);

            challengeCountTextContainer.SetActive(_isCurrentUser);
            challengeButton.gameObject.SetActive(!_isCurrentUser);

            if (_isCurrentUser)
            {
                var player = Game.Game.instance.Stage.SelectedPlayer;
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

                challengeCountText.text =
                    $"<color=orange>{ArenaInfo.DailyChallengeCount}</color>/{GameConfig.ArenaChallengeCountMax}";
            }
            else
            {
                characterView.SetByArenaInfo(ArenaInfo);
                UpdateChallengeButton(_viewModel.currentAvatarCanBattle);
            }

            characterView.Show();
        }

        private void UpdateRank(int rank)
        {
            switch (rank)
            {
                case -1:
                    rankImageContainer.SetActive(false);
                    rankTextContainer.SetActive(true);
                    rankText.text = "-";
                    break;
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

        private void UpdateChallengeButton(bool canBattle)
        {
            if (_viewModel != null)
            {
                _viewModel.currentAvatarCanBattle = canBattle;
            }

            if (_viewModel?.currentAvatarArenaInfo is null)
            {
                challengeButton.SetConditionalState(canBattle);
            }
            else
            {
                challengeButton.SetConditionalState(_viewModel.currentAvatarArenaInfo.DailyChallengeCount > 0 && canBattle);
            }
        }

        private static string GetCP(ArenaInfo arenaInfo)
        {
            if (States.Instance.CurrentAvatarState?.address == arenaInfo.AvatarAddress)
            {
                return CPHelper.GetCPV2(States.Instance.CurrentAvatarState,
                    Game.Game.instance.TableSheets.CharacterSheet,
                    Game.Game.instance.TableSheets.CostumeStatSheet).ToString();
            }

            return arenaInfo.CombatPoint.ToString();
        }
    }
}
