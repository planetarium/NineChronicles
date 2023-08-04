using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
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
using NineChronicles.ExternalServices.ArenaService.Runtime.Models;

namespace Nekoyume.UI
{
    using Libplanet.Crypto;
    using Nekoyume.Arena;
    using Nekoyume.Model.State;
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
        private GrandFinaleScheduleSheet.Row _grandFinaleScheduleRow;
        private GrandFinaleStates.GrandFinaleParticipant[] _grandFinaleParticipants;
        private bool _useGrandFinale;
        private ArenaParticipantSchema[] _arenaBoardDatas;

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
                var currentRoundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(Game.Game.instance.Agent.BlockIndex);
                var dummyArenaBoardDatas = await Game.Game.instance.ArenaServiceManager.GetArenaParticipantListAsync(currentRoundData.ChampionshipId, currentRoundData.Round, Game.Game.instance.Agent.Address);
                _arenaBoardDatas = dummyArenaBoardDatas.ToArray();

                loading.Close();
                Show(ignoreShowAnimation);
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
            bool ignoreShowAnimation = false) =>
            Show(_roundData,
                _arenaBoardDatas,
                ignoreShowAnimation);

        public void Show(
            ArenaSheet.RoundData roundData,
            ArenaParticipantSchema[] arenaBoardDatas,
            bool ignoreShowAnimation = false)
        {
            _useGrandFinale = false;
            _roundData = roundData;
            _arenaBoardDatas = arenaBoardDatas;
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

            var equipments = player.ItemSlotState.Equipments
                .Select(guid =>
                    player.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumes = player.ItemSlotState.Costumes
                .Select(guid =>
                    player.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(player.RuneStates, runeOptionSheet);
            var lv = player.AvatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(player.AvatarState.characterId, out var row))
            {
                return;
            }

            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet);
            _billboard.SetData(
                "season",
                player.Rank,
                player.CurrentArenaInfo.Win,
                player.CurrentArenaInfo.Lose,
                cp,
                player.Score);
        }

        private async UniTaskVoid ShowAvaterStateInfoAsync(string address)
        {
            var loadingScreen = Widget.Find<GrayLoadingScreen>();
            loadingScreen.Show("UI_LOADING_STATES", true);
            var (exist, state) = await States.TryGetAvatarStateAsync(new Address(address));
            if (!exist)
            {
                NotificationSystem.Push(
                        MailType.System,
                        "Cannot Find AvartarState",
                        NotificationCell.NotificationType.Alert);
                loadingScreen.Close();
                return;
            }

            loadingScreen.Close();
            var avatarState = exist ? state : null;
            var popup = Widget.Find<FriendInfoPopup>();
            popup.ShowAsync(avatarState, BattleType.Arena).Forget();
        }

        private async UniTaskVoid ShowArenaBattlePreperation(ArenaParticipantSchema arenaBoardData)
        {
            var loadingScreen = Widget.Find<GrayLoadingScreen>();
            loadingScreen.Show("UI_LOADING_STATES", true);
            var (exist, avatarState) = await States.TryGetAvatarStateAsync(new Address(arenaBoardData.AvartarAddr));
            if (!exist)
            {
                NotificationSystem.Push(
                        MailType.System,
                        "Cannot Find AvartarState",
                        NotificationCell.NotificationType.Alert);
                loadingScreen.Close();
                return;
            }

            var (itemSlotStates, runeSlotStates) = await avatarState.GetSlotStatesAsync();
            var runeStates = await avatarState.GetRuneStatesAsync();

            loadingScreen.Close();

            var itemSlotState = itemSlotStates.Find(itemSlotState => itemSlotState.BattleType == BattleType.Arena);
            var runSlotState = runeSlotStates.Find(runeSlotState => runeSlotState.BattleType == BattleType.Arena);
            var equipments = itemSlotState.Equipments
                .Select(guid =>
                    avatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var costumes = itemSlotState.Costumes
                .Select(guid =>
                    avatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(runeStates, runeOptionSheet);
            var lv = avatarState.level;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet",
                    $"{avatarState.characterId}");
            }

            Close();

            Find<ArenaBattlePreparation>().Show(
                _roundData,
                avatarState,
                equipments,
                costumes,
                CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet));
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
                    var avatarAddress = _arenaBoardDatas[index].AvartarAddr;
                    ShowAvaterStateInfoAsync(avatarAddress).Forget();
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
                    ShowArenaBattlePreperation(_arenaBoardDatas[index]).Forget();
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
            List<ArenaBoardPlayerItemData> scrollData = new List<ArenaBoardPlayerItemData>();
            foreach (var item in _arenaBoardDatas)
            {
                var (win, lose, _) = ArenaHelper.GetScores(RxProps.PlayersArenaParticipant.Value.Score, item.Score);
                scrollData.Add(new ArenaBoardPlayerItemData
                {
                    name = item.Name,
                    level = item.Level,
                    fullCostumeOrArmorId = item.CostumeId,
                    titleId = item.TitleId,
                    cp = 0,
                    score = item.Score,
                    rank = item.Rank,
                    expectWinDeltaScore = win,
                    interactableChoiceButton = !item.AvartarAddr.Equals(States.Instance.CurrentAvatarState.address),
                    canFight = true,
                    address = item.AvartarAddr,
                });

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
            var equipments = player.ItemSlotState.Equipments
                .Select(guid =>
                    player.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumes = player.ItemSlotState.Costumes
                .Select(guid =>
                    player.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var runeOptions = Util.GetRuneOptions(player.RuneStates, runeOptionSheet);
            var lv = player.AvatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(player.AvatarState.characterId, out var row))
            {
                return;
            }

            var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet);

            _billboard.SetData(
                "GrandFinale",
                player.Rank,
                winCount,
                battleRecords.Count - winCount,
                cp,
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
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
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

                    var equipments = e.ItemSlotState.Equipments
                        .Select(guid =>
                            e.AvatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var costumes = e.ItemSlotState.Costumes
                        .Select(guid =>
                            e.AvatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                        .Where(item => item != null).ToList();
                    var runeOptions = Util.GetRuneOptions(e.RuneStates, runeOptionSheet);
                    var lv = e.AvatarState.level;
                    if (!characterSheet.TryGetValue(e.AvatarState.characterId, out var row))
                    {
                        throw new SheetRowNotFoundException("CharacterSheet",
                            $"{e.AvatarState.characterId}");
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
                        cp = CPHelper.TotalCP(equipments, costumes, runeOptions, lv, row, costumeSheet),
                        score = e.Score,
                        rank = e.Rank,
                        expectWinDeltaScore = BattleGrandFinale.WinScore,
                        interactableChoiceButton = !e.AvatarAddr.Equals(currentAvatarAddr),
                        canFight = isParticipant,
                        winAtGrandFinale = hasBattleRecord ? win : null,
                        address = e.AvatarAddr.ToHex()
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
