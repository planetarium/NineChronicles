using System;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingBoard : Widget
    {
        public enum StateType
        {
            Filtered,
            Overall
        }
        private const int NpcId = 300002;
        private static readonly Vector3 NpcPosition = new Vector3(1.2f, -1.72f);

        public CategoryButton filteredButton;
        public CategoryButton overallButton;
        public RankingInfo rankingBase;
        public ScrollRect board;
        public SpeechBubble speechBubble;

        private State.RankingInfo[] _avatarStates;
        private Npc _npc;

        private readonly ReactiveProperty<StateType> _state = new ReactiveProperty<StateType>(StateType.Filtered);

        protected override void Awake()
        {
            base.Awake();

            _state.Subscribe(SubscribeState).AddTo(gameObject);

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
        }

        protected override void OnCompleteOfShowAnimation()
        {
            base.OnCompleteOfShowAnimation();

            _npc.gameObject.SetActive(true);
            _npc.Appear();
            ShowSpeech("SPEECH_RANKING_BOARD_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public void Show(StateType stateType = StateType.Filtered)
        {
            base.Show();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("ranking");
            stage.GetPlayer().gameObject.SetActive(false);

            _state.SetValueAndForceNotify(stateType);

            Find<BottomMenu>()?.Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick, true);

            var go = Game.Game.instance.Stage.npcFactory.Create(NpcId, NpcPosition);
            _npc = go.GetComponent<Npc>();
            _npc.gameObject.SetActive(false);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>()?.Close();

            _avatarStates = null;
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
                case StateType.Filtered:
                    filteredButton.SetToggledOn();
                    overallButton.SetToggledOff();
                    UpdateBoard(DateTimeOffset.UtcNow);
                    break;
                case StateType.Overall:
                    filteredButton.SetToggledOff();
                    overallButton.SetToggledOn();
                    UpdateBoard(null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }
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

                rankingInfo.Set(index + 1, avatarState);
                rankingInfo.onClick = OnClickRankingInfo;
                rankingInfo.gameObject.SetActive(true);
            }
        }

        private void OnClickRankingInfo(RankingInfo info)
        {
//            Application.OpenURL(string.Format(GameConfig.BlockExplorerLinkFormat, info.AvatarInfo.AvatarAddress));
            ActionManager.RankingBattle(info.AvatarInfo.AvatarAddress);
            Find<LoadingScreen>().Show();
            Find<RankingBattleLoadingScreen>().Show(info.AvatarInfo);
        }

        private void GetAvatars(DateTimeOffset? dt)
        {
            _avatarStates = States.Instance.RankingState?.GetAvatars(dt) ?? new State.RankingInfo[0];
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
                    _npc.Greeting();
                }
                else
                {
                    _npc.Emotion();
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
