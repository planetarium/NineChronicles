using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.GrandFinale;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        private GameObject grandFinaleLogoObject;

        private ArenaSheet.RoundData _roundData;
        private RxProps.ArenaParticipant[] _boundedData;
        private GrandFinaleScheduleSheet.Row _grandFinaleScheduleRow;
        private GrandFinaleStates.GrandFinaleParticipant[] _grandFinaleParticipants;
        private bool _useGrandFinale;

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
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            if (!_useGrandFinale)
            {
                await UniTask.WaitWhile(() =>
                    RxProps.ArenaParticipantsOrderedWithScore.IsUpdating);
                loading.Close();
                Show(
                    RxProps.ArenaParticipantsOrderedWithScore.Value,
                    ignoreShowAnimation);
            }
            else
            {
                await UniTask.WaitWhile(() => States.Instance.GrandFinaleStates.IsUpdating);
                loading.Close();
                Show(
                    _grandFinaleScheduleRow,
                    States.Instance.GrandFinaleStates.GrandFinaleParticipants);
            }
        }

        public void Show(
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false) =>
            Show(_roundData,
                arenaParticipants,
                ignoreShowAnimation);

        public void Show(
            ArenaSheet.RoundData roundData,
            RxProps.ArenaParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            _useGrandFinale = false;
            _roundData = roundData;
            _boundedData = arenaParticipants;
            grandFinaleLogoObject.SetActive(false);
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
            var player = RxProps.PlayersArenaParticipant.Value;
            if (player is null)
            {
                Debug.Log($"{nameof(RxProps.PlayersArenaParticipant)} is null");
                _billboard.SetData();
                return;
            }

            if (player.CurrentArenaInfo is null)
            {
                Debug.Log($"{nameof(player.CurrentArenaInfo)} is null");
                _billboard.SetData();
                return;
            }

            _billboard.SetData(
                "season",
                player.Rank,
                player.CurrentArenaInfo.Win,
                player.CurrentArenaInfo.Lose,
                player.CP,
                player.Score);
        }

        private void InitializeScrolls()
        {
            _playerScroll.OnClickCharacterView.Subscribe(index =>
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
                    var avatarState = _useGrandFinale
                        ? _grandFinaleParticipants[index].AvatarState
                        : _boundedData[index].AvatarState;
                    Find<FriendInfoPopup>().Show(avatarState);
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
                    Close();
                    if (_useGrandFinale)
                    {
                        Find<ArenaBattlePreparation>().Show(
                            _grandFinaleScheduleRow?.Id ?? 0,
                            _grandFinaleParticipants[index].AvatarState);
                    }
                    else
                    {
                        Find<ArenaBattlePreparation>().Show(
                            _roundData,
                            _boundedData[index].AvatarState);
                    }
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
                _boundedData.Select(e =>
                {
                    return new ArenaBoardPlayerItemData
                    {
                        name = e.AvatarState.NameWithHash,
                        level = e.AvatarState.level,
                        fullCostumeOrArmorId =
                            e.AvatarState.inventory.GetEquippedFullCostumeOrArmorId(),
                        titleId = e.AvatarState.inventory.Costumes
                            .FirstOrDefault(costume =>
                                costume.ItemSubType == ItemSubType.Title
                                && costume.Equipped)?
                            .Id,
                        cp = e.AvatarState.GetCP(),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = e.ExpectDeltaScore.win,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                        canFight = true,
                    };
                }).ToList();
            for (var i = 0; i < _boundedData.Length; i++)
            {
                var data = _boundedData[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }

        #region For GrandFinale

        public void Show(
            GrandFinaleScheduleSheet.Row scheduleRow,
            GrandFinaleStates.GrandFinaleParticipant[] arenaParticipants,
            bool ignoreShowAnimation = false)
        {
            _useGrandFinale = true;
            _grandFinaleScheduleRow = scheduleRow;
            grandFinaleLogoObject.SetActive(true);
            _grandFinaleParticipants = GetOrderedGrandFinaleParticipants(arenaParticipants.ToList());
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Battle);
            UpdateBillboardForGrandFinale();
            UpdateScrollsForGrandFinale();

            _noJoinedPlayersGameObject.SetActive(_playerScroll.Data.Count < 1);
            base.Show(ignoreShowAnimation);
        }

        /// <summary>
        /// Participants with battle record are moved back.
        /// If States.Instance.GrandFinaleStates.GrandFinalePlayer is null, return participants.
        /// </summary>
        /// <param name="participants"></param>
        private GrandFinaleStates.GrandFinaleParticipant[] GetOrderedGrandFinaleParticipants(
            List<GrandFinaleStates.GrandFinaleParticipant> participants)
        {
            var grandFinalePlayer = States.Instance.GrandFinaleStates.GrandFinalePlayer;
            if (States.Instance.GrandFinaleStates.GrandFinalePlayer is null)
            {
                return participants.ToArray();
            }

            bool IsFoughtPlayer(GrandFinaleStates.GrandFinaleParticipant data)
            {
                return grandFinalePlayer.CurrentInfo.TryGetBattleRecord(data.AvatarAddr, out _);
            }

            var foughtPlayers = participants.Where(IsFoughtPlayer).ToList();
            participants.RemoveAll(IsFoughtPlayer);
            participants.AddRange(foughtPlayers);
            return participants.ToArray();
        }
        private void UpdateBillboardForGrandFinale()
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

            var player = States.Instance.GrandFinaleStates.GrandFinalePlayer;
            if (player is null)
            {
                Debug.Log($"{nameof(RxProps.PlayersArenaParticipant)} is null");
                _billboard.SetData();
                return;
            }

            if (player.CurrentInfo is null)
            {
                Debug.Log($"{nameof(player.CurrentInfo)} is null");
                _billboard.SetData();
                return;
            }

            var battleRecords = player.CurrentInfo.GetBattleRecordList();
            var winCount = battleRecords.Count(pair => pair.Value);
            _billboard.SetData(
                "GrandFinale",
                player.Rank,
                winCount,
                battleRecords.Count - winCount,
                player.CP,
                player.Score);
        }

        private void UpdateScrollsForGrandFinale()
        {
            var (scrollData, playerIndex) =
                GetScrollDataForGrandFinale();
            _playerScroll.SetData(scrollData, playerIndex);
        }

        private (List<ArenaBoardPlayerItemData> scrollData, int playerIndex)
            GetScrollDataForGrandFinale()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return (_so.ArenaBoardPlayerScrollData, 0);
            }
#endif

            var isParticipant = States.Instance.GrandFinaleStates.GrandFinalePlayer is not null;
            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var scrollData =
                _grandFinaleParticipants.Select(e =>
                {
                    var hasBattleRecord = false;
                    var win = false;
                    if (isParticipant)
                    {
                        hasBattleRecord = States.Instance.GrandFinaleStates.GrandFinalePlayer
                            .CurrentInfo
                            .TryGetBattleRecord(e.AvatarAddr, out win);
                    }

                    return new ArenaBoardPlayerItemData
                    {
                        name = e.AvatarState.NameWithHash,
                        level = e.AvatarState.level,
                        fullCostumeOrArmorId =
                            e.AvatarState.inventory.GetEquippedFullCostumeOrArmorId(),
                        titleId = e.AvatarState.inventory.Costumes
                            .FirstOrDefault(costume =>
                                costume.ItemSubType == ItemSubType.Title
                                && costume.Equipped)?
                            .Id,
                        cp = e.AvatarState.GetCP(),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = BattleGrandFinale.WinScore,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                        canFight = isParticipant,
                        winAtGrandFinale = hasBattleRecord ? win : null,
                    };
                }).ToList();

            for (var i = 0; i < _grandFinaleParticipants.Length; i++)
            {
                var data = _grandFinaleParticipants[i];
                if (data.AvatarAddr.Equals(currentAvatarAddr))
                {
                    return (scrollData, i);
                }
            }

            return (scrollData, 0);
        }

        #endregion
    }
}
