using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
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

        [SerializeField] private ArenaBoardBillboard _billboard;

        [SerializeField] private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField] private Button _backButton;

        private ArenaSheet.RoundData _roundData;

        private RxProps.ArenaParticipant[] _boundedData;

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

        public async UniTaskVoid ShowAsync(
            ArenaSheet.RoundData roundData,
            bool ignoreShowAnimation = false) =>
            Show(
                roundData,
                await RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync(),
                ignoreShowAnimation);

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
            _roundData = roundData;
            _boundedData = arenaParticipants;
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();
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
            _playerScroll.OnClickChoice.Subscribe(index =>
                {
                    Debug.Log($"{index} choose!");

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
                        _roundData.ChampionshipId,
                        _roundData.Round,
                        data.AvatarState);
                })
                .AddTo(gameObject);
        }

        private void UpdateScrolls()
        {
            _playerScroll.SetData(GetScrollData(), 0);
        }

        private List<ArenaBoardPlayerItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.ArenaBoardPlayerScrollData;
            }
#endif

            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            return RxProps.ArenaParticipantsOrderedWithScore.Value.Select(e =>
            {
                return new ArenaBoardPlayerItemData
                {
                    name = e.AvatarState.NameWithHash,
                    level = e.AvatarState.level,
                    fullCostumeOrArmorId = e.AvatarState.inventory.GetEquippedFullCostumeOrArmorId(),
                    titleId = e.AvatarState.inventory.Costumes
                        .FirstOrDefault(costume =>
                            costume.ItemSubType == ItemSubType.Title
                            && costume.Equipped)?
                        .Id,
                    cp = e.AvatarState.GetCP(),
                    score = e.Score,
                    expectWinDeltaScore = e.ExpectDeltaScore.win,
                    interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                };
            }).ToList();
        }
    }
}