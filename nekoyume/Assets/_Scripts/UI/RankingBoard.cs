using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Battle;
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
    using UniRx;

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

        private List<Nekoyume.Model.State.RankingInfo> _rankingInfos =
            new List<Nekoyume.Model.State.RankingInfo>();

        private List<(int rank, ArenaInfo arenaInfo)> _weeklyCachedInfo =
            new List<(int rank, ArenaInfo arenaInfo)>();

        private readonly List<IDisposable> _disposablesFromShow = new List<IDisposable>();

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
            SetRankingInfos(States.Instance.RankingMapStates);
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
            var agent = Game.Game.instance.Agent;
            var gameConfigState = States.Instance.GameConfigState;
            var weeklyArenaIndex = (int) agent.BlockIndex / gameConfigState.WeeklyArenaInterval;
            var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(weeklyArenaIndex);
            var weeklyArenaState =
                new WeeklyArenaState(
                    (Bencodex.Types.Dictionary) agent.GetState(weeklyArenaAddress));
            States.Instance.SetWeeklyArenaState(weeklyArenaState);

            for (var i = 0; i < RankingState.RankingMapCapacity; ++i)
            {
                var rankingMapAddress = RankingState.Derive(i);
                var rankingMapState = agent.GetState(rankingMapAddress) is Bencodex.Types.Dictionary serialized
                    ? new RankingMapState(serialized)
                    : new RankingMapState(rankingMapAddress);
                States.Instance.SetRankingMapStates(rankingMapState);
            }

            base.Show();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("ranking");
            _player = stage.GetPlayer();
            _player.gameObject.SetActive(false);
            UpdateWeeklyCache(States.Instance.WeeklyArenaState);

            _state.SetValueAndForceNotify(stateType);

            Find<BottomMenu>()?.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Ranking,
                BottomMenu.ToggleableType.Character);

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.InGameBackground,
                3);
            _npc = go.GetComponent<NPC>();
            _npc.gameObject.SetActive(false);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            WeeklyArenaStateSubject.WeeklyArenaState
                .Subscribe(SubscribeWeeklyArenaState)
                .AddTo(_disposablesFromShow);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesFromShow.DisposeAllAndClear();

            Find<BottomMenu>()?.Close();

            base.Close(ignoreCloseAnimation);

            _npc?.gameObject.SetActive(false);
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

        private void SubscribeWeeklyArenaState(WeeklyArenaState state)
        {
            UpdateWeeklyCache(state);
            UpdateArena();
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

            if (!_weeklyCachedInfo.Any())
            {
                currentAvatarCellView.ShowMyDefaultInfo();

                UpdateBoard(StateType.Arena);
                return;
            }

            var arenaInfo = _weeklyCachedInfo[0].arenaInfo;
            if (!arenaInfo.Active)
            {
                currentAvatarCellView.ShowMyDefaultInfo();
                LocalLayerModifier.AddWeeklyArenaInfoActivator(Game.Game.instance.TableSheets.CharacterSheet);
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
                    currentAvatarCellView.ShowMyDefaultInfo();

                    arenaRankScroll.Show(_weeklyCachedInfo
                        .Select(tuple => new ArenaRankCell.ViewModel
                        {
                            rank = tuple.rank,
                            arenaInfo = tuple.arenaInfo,
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

                var (currentAvatarRank, currentAvatarArenaInfo) = _weeklyCachedInfo
                    .FirstOrDefault(info =>
                        info.arenaInfo.AvatarAddress.Equals(currentAvatarAddress));
                if (currentAvatarArenaInfo is null)
                {
                    currentAvatarRank = -1;
                    currentAvatarArenaInfo = new ArenaInfo(
                        States.Instance.CurrentAvatarState,
                        Game.Game.instance.TableSheets.CharacterSheet,
                        false);
                }
                
                currentAvatarCellView.Show((
                    currentAvatarRank,
                    currentAvatarArenaInfo,
                    currentAvatarArenaInfo));

                arenaRankScroll.Show(_weeklyCachedInfo
                    .Select(tuple => new ArenaRankCell.ViewModel
                    {
                        rank = tuple.rank,
                        arenaInfo = tuple.arenaInfo,
                        currentAvatarArenaInfo = currentAvatarArenaInfo,
                    }).ToList(), true);
            }
            else
            {
                arenaRankScroll.Hide();

                var states = States.Instance;
                if (!_rankingInfos.Any())
                {
                    expRankScroll.ClearData();
                    expRankScroll.Show();
                    return;
                }

                var rank = 1;
                List<Nekoyume.Model.State.RankingInfo> rankingInfos;
                var gameConfigState = states.GameConfigState;
                if (stateType == StateType.Filtered)
                {
                    var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    rankingInfos = _rankingInfos
                        .Where(context =>
                            currentBlockIndex - context.UpdatedAt <= gameConfigState.DailyRewardInterval)
                        .Take(100)
                        .ToList();
                }
                else
                {
                    rankingInfos = _rankingInfos
                        .Take(100)
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
                _player.Costumes.Select(i => i.ItemId).ToList(),
                _player.Equipments.Select(i => i.ItemId).ToList(),
                new List<Guid>()
            );
            Find<ArenaBattleLoadingScreen>().Show(arenaRankCell.ArenaInfo);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            var avatarInfo = Find<AvatarInfo>();
            var friendInfoPopup = Find<FriendInfoPopup>();
            if (avatarInfo.gameObject.activeSelf)
            {
                avatarInfo.Close();
            }
            else if(friendInfoPopup.gameObject.activeSelf)
            {
                friendInfoPopup.Close();
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

            _rankingInfos = rankingInfos
                .OrderByDescending(c => c.Exp)
                .ThenBy(c => c.StageClearedBlockIndex)
                .ToList();
        }

        private void UpdateWeeklyCache(WeeklyArenaState state)
        {
            var infos = state.GetArenaInfos(1, 3);

            if (States.Instance.CurrentAvatarState != null)
            {
                var currentAvatarAddress = States.Instance.CurrentAvatarState.address;
                var infos2 = state.GetArenaInfos(currentAvatarAddress, 20, 20);
                // Player does not play prev & this week arena.
                if (!infos2.Any() && state.OrderedArenaInfos.Any())
                {
                    var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
                    var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                    var cp = CPHelper.GetCPV2(States.Instance.CurrentAvatarState, characterSheet, costumeStatSheet);
                    var address = state.OrderedArenaInfos.First(i => i.CombatPoint <= cp).AvatarAddress;
                    infos2 = state.GetArenaInfos(address, 20, 20);
                }

                infos.AddRange(infos2);
                infos = infos.ToImmutableHashSet().OrderBy(tuple => tuple.rank).ToList();
            }
            _weeklyCachedInfo = infos;
        }
    }
}
