using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using RankingInfo = Nekoyume.UI.Scroller.RankingInfo;

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

        public CategoryButton arenaButton;
        public CategoryButton filteredButton;
        public CategoryButton overallButton;
        public GameObject arenaRankingHeader;
        public GameObject expRankingHeader;
        public ArenaRankingCellView arenaRankingCellViewPrefab;
        public RankingInfo rankingCellViewPrefab;
        public ScrollRect board;
        public ArenaPendingNCG arenaPendingNCG;
        public GameObject arenaRecordContainer;
        public TextMeshProUGUI arenaRecordText;
        public ArenaCellView arenaCellView;
        public SubmitButton arenaActivationButton;
        public RankingRewards rankingRewards;
        public SpeechBubble speechBubble;

        private List<(int rank, ArenaInfo arenaInfo)> _arenaAvatarStates;
        private Nekoyume.Model.State.RankingInfo[] _avatarRankingStates;
        private NPC _npc;

        private readonly ReactiveProperty<StateType> _state = new ReactiveProperty<StateType>(StateType.Arena);

        private readonly List<IDisposable> _disposablesAtClose = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            _state.Subscribe(SubscribeState).AddTo(gameObject);

            arenaButton.button.OnClickAsObservable()
            .Subscribe(_ =>
            {
                _state.Value = StateType.Arena;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_ARENA_");
            })
            .AddTo(gameObject);

            filteredButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _state.Value = StateType.Filtered;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_FILTERED_");
                })
                .AddTo(gameObject);

            overallButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _state.Value = StateType.Overall;
                    // SubScribeState대신 밖에서 처리하는 이유는 랭킹보드 진입시에도 상태가 상태가 바뀌기 때문
                    ShowSpeech("SPEECH_RANKING_BOARD_ALL_");
                })
                .AddTo(gameObject);

            arenaActivationButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    // todo: 아레나 참가하기.
                    // fixme: 지금 개발 단계에서는 참가 액션이 분리되어 있지 않기 때문에, 참가할 때 골드를 깍지 못함.
                    // LocalStateModifier.ModifyAgentGold(States.Instance.AgentState.address,
                    //     -GameConfig.ArenaActivationCostNCG);
                    // fixme: 지금 개발 단계에서는 참가 액션이 분리되어 있지 않기 때문에, 참가할 때 골드를 더하지 못함.
                    // LocalStateModifier.ModifyWeeklyArenaGold(GameConfig.ArenaActivationCostNCG);
                    LocalStateModifier.AddWeeklyArenaInfoActivator();
                }).AddTo(gameObject);
        }

        protected override void OnCompleteOfShowAnimation()
        {
            base.OnCompleteOfShowAnimation();

            _npc.gameObject.SetActive(true);
            _npc.SpineController.Appear();
            ShowSpeech("SPEECH_RANKING_BOARD_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public void Show(StateType stateType = StateType.Arena)
        {
            base.Show();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("ranking");
            stage.GetPlayer().gameObject.SetActive(false);

            _state.SetValueAndForceNotify(stateType);

            Find<BottomMenu>()?.Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick, true);

            var go = Game.Game.instance.Stage.npcFactory.Create(NPCId, NPCPosition);
            _npc = go.GetComponent<NPC>();
            _npc.gameObject.SetActive(false);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            
            // 구독.
            ReactiveAgentState.Gold.Subscribe(gold =>
                    arenaActivationButton.SetSubmittable(gold >= GameConfig.ArenaActivationCostNCG))
                .AddTo(_disposablesAtClose);
            WeeklyArenaStateSubject.WeeklyArenaState.Subscribe(state => UpdateArena())
                .AddTo(_disposablesAtClose);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            // 구독 취소.
            _disposablesAtClose.DisposeAllAndClear();
            
            Find<BottomMenu>()?.Close();

            _arenaAvatarStates = null;
            ClearBoard();

            base.Close(ignoreCloseAnimation);

            _npc.gameObject.SetActive(false);
            speechBubble.Hide();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        private void SubscribeState(StateType stateType)
        {
            switch (stateType)
            {
                case StateType.Arena:
                    arenaButton.SetToggledOn();
                    filteredButton.SetToggledOff();
                    overallButton.SetToggledOff();
                    rankingRewards.Hide();
                    arenaPendingNCG.Show(false);
                    arenaCellView.Show();
                    UpdateArena();
                    arenaRankingHeader.SetActive(true);
                    expRankingHeader.SetActive(false);
                    UpdateBoard(stateType);
                    break;
                case StateType.Filtered:
                    arenaButton.SetToggledOff();
                    filteredButton.SetToggledOn();
                    overallButton.SetToggledOff();
                    rankingRewards.Show();
                    arenaPendingNCG.Hide();
                    arenaCellView.Hide();
                    arenaRecordContainer.SetActive(false);
                    arenaRankingHeader.SetActive(false);
                    expRankingHeader.SetActive(true);
                    UpdateBoard(stateType);
                    break;
                case StateType.Overall:
                    arenaButton.SetToggledOff();
                    filteredButton.SetToggledOff();
                    overallButton.SetToggledOn();
                    rankingRewards.Show();
                    arenaPendingNCG.Hide();
                    arenaCellView.Hide();
                    arenaRecordContainer.SetActive(false);
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
            if (States.Instance.CurrentAvatarState is null)
                return;

            var weeklyArenaState = States.Instance.WeeklyArenaState;
            arenaPendingNCG.Show(weeklyArenaState);

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var infos = weeklyArenaState.GetArenaInfos(avatarAddress, 0, 0);
            try
            {
                var (rank, arenaInfo) = infos.First(tuple => tuple.arenaInfo.AvatarAddress.Equals(avatarAddress));
                var record = arenaInfo.ArenaRecord;
                if (arenaInfo.Active)
                {
                    arenaRecordContainer.SetActive(true);
                    arenaRecordText.text = string.Format(
                        LocalizationManager.Localize("UI_WIN_DRAW_LOSE_FORMAT"), record.Win, record.Draw, record.Lose);
                    arenaCellView.Show(rank, arenaInfo);
                    arenaActivationButton.Hide();
                }
                else
                    throw new Exception();
            }
            catch
            {
                arenaRecordContainer.SetActive(false);
                arenaCellView.Hide();
                arenaActivationButton.Show();
            }
        }

        private void UpdateBoard(StateType stateType)
        {
            ClearBoard();

            if (stateType == StateType.Arena)
            {
                SetArenaInfos();

                if (States.Instance.CurrentAvatarState is null)
                    return;

                var weeklyArenaState = States.Instance.WeeklyArenaState;
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var currentAvatarArenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);

                var canChallenge = (currentAvatarArenaInfo is null) ?
                                    true : currentAvatarArenaInfo.DailyChallengeCount > 0;

                for (var index = 0; index < _arenaAvatarStates.Count; index++)
                {
                    var avatarState = _arenaAvatarStates[index].arenaInfo;
                    if (avatarState is null)
                    {
                        continue;
                    }

                    ArenaRankingCellView rankingInfo = Instantiate(arenaRankingCellViewPrefab, board.content);
                    var bg = rankingInfo.GetComponent<Image>();
                    if (index % 2 == 1)
                    {
                        bg.enabled = false;
                    }

                    rankingInfo.Set(index + 1, avatarState, canChallenge);
                    rankingInfo.onClickChallenge = OnClickChallenge;
                    rankingInfo.onClickInfo = OnClickAvatarInfo;
                    rankingInfo.gameObject.SetActive(true);
                }
            }
            else
            {
                if (stateType == StateType.Filtered)
                {
                    SetAvatars(DateTimeOffset.UtcNow);
                }
                else
                {
                    SetAvatars(null);
                }

                for (var index = 0; index < _avatarRankingStates.Length; index++)
                {
                    var avatarState = _avatarRankingStates[index];
                    if (avatarState is null)
                    {
                        continue;
                    }

                    RankingInfo rankingInfo = Instantiate(rankingCellViewPrefab, board.content);
                    var bg = rankingInfo.GetComponent<Image>();
                    if (index % 2 == 1)
                    {
                        bg.enabled = false;
                    }

                    rankingInfo.Set(index + 1, avatarState);
                    rankingInfo.onClick = OnClickAvatarInfo;
                    rankingInfo.gameObject.SetActive(true);
                }
            }
        }

        private void OnClickAvatarInfo(Address avatarAddress)
        {
            Application.OpenURL(string.Format(GameConfig.BlockExplorerLinkFormat, avatarAddress));
        }

        private void OnClickChallenge(ArenaRankingCellView info)
        {
            ActionManager.RankingBattle(info.AvatarInfo.AvatarAddress);
            Find<LoadingScreen>().Show();
            Find<RankingBattleLoadingScreen>().Show(info.AvatarInfo);
        }

        private void SetAvatars(DateTimeOffset? dt)
        {
            _avatarRankingStates = States.Instance.RankingState?.GetAvatars(dt) ?? new State.RankingInfo[0];
        }

        private void SetArenaInfos()
        {
            var currentAvatarState = States.Instance.CurrentAvatarState;

            if (currentAvatarState is null)
            {
                _arenaAvatarStates = new List<(int rank, ArenaInfo arenaInfo)>();
                return;
            }

            var avatarAddress = currentAvatarState.address;

            _arenaAvatarStates = States.Instance.WeeklyArenaState?.GetArenaInfos(avatarAddress)
                ?? new List<(int rank, ArenaInfo arenaInfo)>();
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
            Game.Event.OnRoomEnter.Invoke();
        }

        private void ShowSpeech(string key, CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
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
            Find<LoadingScreen>().Close();
            Find<RankingBattleLoadingScreen>().Close();
            Close();
        }
    }
}
