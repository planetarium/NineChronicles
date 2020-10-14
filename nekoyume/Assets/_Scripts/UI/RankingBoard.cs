using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class RankingBoard : Widget
    {
        public enum StateType
        {
            Arena,
            Filtered,
            Overall
        }

        private const int NPCId = 300002;
        private static readonly Vector3 NPCPosition = new Vector3(1.2f, -1.72f);

        [SerializeField]
        private CategoryButton arenaButton = null;

        [SerializeField]
        private CategoryButton filteredButton = null;

        [SerializeField]
        private CategoryButton overallButton = null;

        [SerializeField]
        private GameObject arenaRankingHeader = null;

        [SerializeField]
        private GameObject expRankingHeader = null;

        [SerializeField]
        private ArenaRankScroll arenaRankScroll = null;

        [SerializeField]
        private ExpRankScroll expRankScroll = null;

        [SerializeField]
        private ArenaRankCell currentAvatarCellView = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        [SerializeField]
        private TextMeshProUGUI rewardText = null;

        [SerializeField]
        private TextMeshProUGUI winText = null;

        [SerializeField]
        private TextMeshProUGUI loseText = null;

        private Nekoyume.Model.State.RankingInfo[] _avatarRankingStates;
        private NPC _npc;
        private Player _player;

        private readonly ReactiveProperty<StateType> _state =
            new ReactiveProperty<StateType>(StateType.Arena);

        private readonly List<IDisposable> _disposablesAtClose = new List<IDisposable>();

        private HashSet<Nekoyume.Model.State.RankingInfo> _rankingInfos =
            new HashSet<Nekoyume.Model.State.RankingInfo>();

        protected override void Awake()
        {
            base.Awake();

            _state.Subscribe(SubscribeState).AddTo(gameObject);

            arenaButton.OnClick
                .Subscribe(_ =>
                {
                    _state.Value = StateType.Arena;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_ARENA_");
                }).AddTo(gameObject);

            filteredButton.OnClick
                .Subscribe(_ =>
                {
                    _state.Value = StateType.Filtered;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_FILTERED_");
                })
                .AddTo(gameObject);

            overallButton.OnClick
                .Subscribe(_ =>
                {
                    _state.Value = StateType.Overall;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_ALL_");
                })
                .AddTo(gameObject);

            arenaRankScroll.OnClickAvatarInfo
                .Subscribe(cell => OnClickAvatarInfo(
                    cell.RectTransform,
                    cell.ArenaInfo.AvatarAddress))
                .AddTo(gameObject);
            arenaRankScroll.OnClickChallenge.Subscribe(OnClickChallenge).AddTo(gameObject);
            expRankScroll.OnClick
                .Subscribe(cell => OnClickAvatarInfo(
                    cell.RectTransform,
                    cell.RankingInfo.AvatarAddress))
                .AddTo(gameObject);
            currentAvatarCellView.OnClickAvatarInfo
                .Subscribe(cell => OnClickAvatarInfo(
                    cell.RectTransform,
                    cell.ArenaInfo.AvatarAddress))
                .AddTo(gameObject);

            rewardText.text = L10nManager.Localize("UI_REWARDS");
            winText.text = L10nManager.Localize("UI_WIN");
            loseText.text = L10nManager.Localize("UI_LOSE");

            CloseWidget = null;
            SubmitWidget = null;
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            _npc.gameObject.SetActive(true);
            _npc.SpineController.Appear();
            ShowSpeech("SPEECH_RANKING_BOARD_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public void Show(StateType stateType = StateType.Arena)
        {
            base.Show();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("ranking");
            _player = stage.GetPlayer();
            _player.gameObject.SetActive(false);

            _state.SetValueAndForceNotify(stateType);

            Find<BottomMenu>()?.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Character);

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.InGameBackground,
                3);
            _npc = go.GetComponent<NPC>();
            _npc.gameObject.SetActive(false);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);

            // NOTE: It only updates once when player open the RankingBoard without subscribing to the status.
            // WeeklyArenaStateSubject.WeeklyArenaState
            //     .Subscribe(state => UpdateArena())
            //     .AddTo(_disposablesAtClose);
            // RankingMapStatesSubject.RankingMapStates
            //     .Subscribe(SetRankingInfos)
            //     .AddTo(_disposablesAtClose);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            // Cancel Subscriptions.
            _disposablesAtClose.DisposeAllAndClear();

            Find<BottomMenu>()?.Close();

            base.Close(ignoreCloseAnimation);

            _npc.gameObject.SetActive(false);
            speechBubble.Hide();
        }

        private void SubscribeState(StateType stateType)
        {
            switch (stateType)
            {
                case StateType.Arena:
                    arenaButton.SetToggledOn();
                    filteredButton.SetToggledOff();
                    overallButton.SetToggledOff();
                    UpdateArena();
                    arenaRankingHeader.SetActive(true);
                    expRankingHeader.SetActive(false);
                    break;
                case StateType.Filtered:
                    arenaButton.SetToggledOff();
                    filteredButton.SetToggledOn();
                    overallButton.SetToggledOff();
                    currentAvatarCellView.Hide();
                    arenaRankingHeader.SetActive(false);
                    expRankingHeader.SetActive(true);
                    UpdateBoard(stateType);
                    break;
                case StateType.Overall:
                    arenaButton.SetToggledOff();
                    filteredButton.SetToggledOff();
                    overallButton.SetToggledOn();
                    currentAvatarCellView.Hide();
                    arenaRankingHeader.SetActive(false);
                    expRankingHeader.SetActive(true);
                    UpdateBoard(stateType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }
        }

        private void UpdateArena()
        {
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            if (weeklyArenaState is null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return;
            }

            var arenaInfos = weeklyArenaState
                .GetArenaInfos(avatarAddress.Value, 0, 0);
            if (arenaInfos.Count == 0)
            {
                currentAvatarCellView.Hide();

                UpdateBoard(StateType.Arena);
                return;
            }

            var (rank, arenaInfo) = arenaInfos[0];
            var record = arenaInfo.ArenaRecord;
            if (arenaInfo.Active)
            {
                currentAvatarCellView.Show((rank, arenaInfo, arenaInfo));
            }
            else
            {
                currentAvatarCellView.Hide();

                var gold = Game.Game.instance.States.GoldBalanceState.Gold;
                if (gold >= gold.Currency * GameConfig.ArenaActivationCostNCG)
                {
                    LocalStateModifier.AddWeeklyArenaInfoActivator(Game.Game.instance.TableSheets
                        .CharacterSheet);
                }
            }

            UpdateBoard(StateType.Arena);
        }

        private void UpdateBoard(StateType stateType)
        {
            if (stateType == StateType.Arena)
            {
                expRankScroll.Hide();

                var weeklyArenaState = States.Instance.WeeklyArenaState;
                if (weeklyArenaState is null)
                {
                    arenaRankScroll.ClearData();
                    arenaRankScroll.Show();
                    return;
                }

                var currentAvatarAddress = States.Instance.CurrentAvatarState?.address;
                if (!currentAvatarAddress.HasValue ||
                    !weeklyArenaState.ContainsKey(currentAvatarAddress.Value))
                {
                    currentAvatarCellView.Hide();

                    arenaRankScroll.Show(weeklyArenaState
                        .GetArenaInfos(1, 100)
                        .Select(tuple => new ArenaRankCell.ViewModel
                        {
                            rank = tuple.rank,
                            arenaInfo = tuple.arenaInfo,
                            currentAvatarArenaInfo = null
                        }).ToList(), true);
                    // NOTE: If you want to test many arena cells, use below instead of above.
                    // arenaRankScroll.Show(Enumerable
                    //     .Range(1, 1000)
                    //     .Select(rank => new ArenaRankCell.ViewModel
                    //     {
                    //         rank = rank,
                    //         arenaInfo = new ArenaInfo(
                    //             States.Instance.CurrentAvatarState,
                    //             Game.Game.instance.TableSheets.CharacterSheet,
                    //             true),
                    //         currentAvatarArenaInfo = null
                    //     }).ToList(), true);

                    return;
                }

                var (currentAvatarRank, currentAvatarArenaInfo) = weeklyArenaState
                    .GetArenaInfos(currentAvatarAddress.Value, 0, 0)
                    .FirstOrDefault(info =>
                        info.arenaInfo.AvatarAddress.Equals(currentAvatarAddress));

                currentAvatarCellView.Show((
                    currentAvatarRank,
                    currentAvatarArenaInfo,
                    currentAvatarArenaInfo));

                arenaRankScroll.Show(weeklyArenaState
                    .GetArenaInfos(1, 100)
                    .Select(tuple => new ArenaRankCell.ViewModel
                    {
                        rank = tuple.rank,
                        arenaInfo = tuple.arenaInfo,
                        currentAvatarArenaInfo = currentAvatarArenaInfo
                    }).ToList(), true);
            }
            else
            {
                arenaRankScroll.Hide();

                var states = States.Instance;
                var rankingState = states.RankingMapStates;
                if (rankingState is null)
                {
                    expRankScroll.ClearData();
                    expRankScroll.Show();
                    return;
                }

                SetRankingInfos(rankingState);

                var rank = 1;
                var rankingInfos = _rankingInfos
                    .OrderByDescending(c => c.Exp)
                    .ThenBy(c => c.StageClearedBlockIndex)
                    .ToList();
                var gameConfigState = states.GameConfigState;
                if (stateType == StateType.Filtered)
                {
                    var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    rankingInfos = rankingInfos
                        .Where(context =>
                            currentBlockIndex - context.UpdatedAt <= gameConfigState.DailyRewardInterval)
                        .ToList();
                }

                expRankScroll.Show(rankingInfos
                    .Select(rankingInfo => new ExpRankCell.ViewModel
                    {
                        rank = rank++,
                        rankingInfo = rankingInfo
                    }).ToList());
            }
        }

        private static void OnClickAvatarInfo(RectTransform rectTransform, Address address)
        {
            // NOTE: 블록 익스플로러 연결 코드. 이후에 참고하기 위해 남겨 둡니다.
            // Application.OpenURL(string.Format(GameConfig.BlockExplorerLinkFormat, avatarAddress));
            Find<AvatarTooltip>().Show(rectTransform, address);
        }

        private void OnClickChallenge(ArenaRankCell arenaRankCell)
        {
            //TODO 소모품장착
            Game.Game.instance.ActionManager.RankingBattle(
                arenaRankCell.ArenaInfo.AvatarAddress,
                _player.Costumes.Select(i => i.Id).ToList(),
                _player.Equipments.Select(i => i.ItemId).ToList(),
                new List<Guid>()
            );
            Find<ArenaBattleLoadingScreen>().Show(arenaRankCell.ArenaInfo);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            var avatarInfo = Find<AvatarInfo>();
            if (avatarInfo.gameObject.activeSelf)
            {
                avatarInfo.Close();
            }
            else
            {
                if (!CanClose)
                {
                    return;
                }

                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }
        }

        private void ShowSpeech(string key,
            CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (_npc)
            {
                if (type == CharacterAnimation.Type.Greeting)
                {
                    _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
                }
                else
                {
                    _npc.PlayAnimation(NPCAnimation.Type.Emotion_01);
                }

                speechBubble.SetKey(key);
                StartCoroutine(speechBubble.CoShowText());
            }
        }

        public void GoToStage(ActionBase.ActionEvaluation<RankingBattle> eval)
        {
            Game.Event.OnRankingBattleStart.Invoke(eval.Action.Result);
            Close();
        }

        private void SetRankingInfos(Dictionary<Address, RankingMapState> states)
        {
            var rankingInfos = new HashSet<Nekoyume.Model.State.RankingInfo>();
            foreach (var pair in states)
            {
                rankingInfos.UnionWith(pair.Value.GetRankingInfos(null));
            }

            _rankingInfos = rankingInfos;
        }
    }
}
