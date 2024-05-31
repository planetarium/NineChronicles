using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaBoard : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif

        [SerializeField]
        private ArenaBoardBillboard _billboard;

        [SerializeField]
        private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField]
        private GameObject _noJoinedPlayersGameObject;

        [SerializeField]
        private Button _backButton;

        private ArenaSheet.RoundData _roundData;
        private List<ArenaParticipantModel> _boundedData;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<ArenaJoin>().Show();
                Close();
            }).AddTo(gameObject);
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Arena);
            var sw = new Stopwatch();
            sw.Start();
            await UniTask.WhenAll(
                RxProps.ArenaInformationOrderedWithScore.UpdateAsync(Game.Game.instance.Agent.BlockTipStateRootHash),
                RxProps.ArenaInfoTuple.UpdateAsync(Game.Game.instance.Agent.BlockTipStateRootHash));
            loading.Close();
            Show(RxProps.ArenaInformationOrderedWithScore.Value,
                ignoreShowAnimation);
            sw.Stop();
            NcDebug.Log($"[Arena] Loading Complete. {sw.Elapsed}");
        }

        public void Show(
            List<ArenaParticipantModel> arenaParticipants,
            bool ignoreShowAnimation = false) =>
            Show(_roundData,
                arenaParticipants,
                ignoreShowAnimation);

        public void Show(
            ArenaSheet.RoundData roundData,
            List<ArenaParticipantModel> arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            _roundData = roundData;
            _boundedData = arenaParticipants;
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();

            // NOTE: This code assumes that '_playerScroll.Data' contains local player
            //       If `_playerScroll.Data` does not contains local player, change `2` in the line below to `1`.
            //       Not use `_boundedData` here because there is the case to
            //       use the mock data from `_so`.
            _noJoinedPlayersGameObject.SetActive(_playerScroll.Data.Count < 2);

            base.Show(ignoreShowAnimation);
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank,
                    _so.WinCount,
                    _so.LoseCount,
                    _so.CP,
                    _so.Rating);
                return;
            }
#endif
            var player = RxProps.PlayerArenaInfo.Value;
            if (player is null)
            {
                NcDebug.Log($"{nameof(RxProps.PlayerArenaInfo)} is null");
                _billboard.SetData();
                return;
            }

            if (!RxProps.ArenaInfoTuple.HasValue)
            {
                NcDebug.Log($"{nameof(RxProps.ArenaInfoTuple)} is null");
                _billboard.SetData();
                return;
            }

            var win = 0;
            var lose = 0;
            var currentInfo = RxProps.ArenaInfoTuple.Value.current;
            if (currentInfo is { })
            {
                win = currentInfo.Win;
                lose = currentInfo.Lose;
            }

            var cp = player.Cp;
            _billboard.SetData(
                "season",
                player.Rank,
                win,
                lose,
                cp,
                player.Score);
        }

        private void InitializeScrolls()
        {
            _playerScroll.OnClickCharacterView.Subscribe(async index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot open when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif
                    var avatarStates = await Game.Game.instance.Agent.GetAvatarStatesAsync(
                        new[] { _boundedData[index].AvatarAddr });
                    var avatarState = avatarStates.Values.First();
                    Find<FriendInfoPopup>().ShowAsync(avatarState, BattleType.Arena).Forget();
                })
                .AddTo(gameObject);

            _playerScroll.OnClickChoice.Subscribe(index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot battle when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif
                    var data = _boundedData[index];
                    Close();
                    Find<ArenaBattlePreparation>().Show(
                        _roundData,
                        _boundedData[index],
                        data.Cp);
                })
                .AddTo(gameObject);
        }

        private void UpdateScrolls()
        {
            var (scrollData, playerIndex) =
                GetScrollData();
            _playerScroll.SetData(scrollData, playerIndex);
        }

        private (List<ArenaBoardPlayerItemData> scrollData, int playerIndex)
            GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return (_so.ArenaBoardPlayerScrollData, 0);
            }
#endif

            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var scrollData =
                _boundedData.Select(e => new ArenaBoardPlayerItemData
                {
                    name = e.NameWithHash,
                    level = e.Level,
                    fullCostumeOrArmorId = e.PortraitId,
                    titleId = null,
                    cp = e.Cp,
                    score = e.Score,
                    rank = e.Rank,
                    expectWinDeltaScore = e.WinScore,
                    interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                    canFight = true,
                    address = e.AvatarAddr.ToHex(),
                    guildName = e.GuildName,
                }).ToList();
            for (var i = 0; i < _boundedData.Count; i++)
            {
                var data = _boundedData[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }
    }
}
