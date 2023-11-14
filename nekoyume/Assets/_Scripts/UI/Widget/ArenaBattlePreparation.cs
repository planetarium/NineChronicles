using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.GraphQL;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.State.Subjects;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaBattlePreparation : Widget
    {
        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private ParticleSystem[] particles;

        [SerializeField]
        private ConditionalCostButton startButton;

        [SerializeField]
        private Button repeatPopupButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Transform buttonStarImageTransform;

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick;

        [SerializeField]
        private TextMeshProUGUI blockStartingText;

        [SerializeField]
        private TextMeshProUGUI enemyCp;

        [SerializeField]
        private ConditionalButton grandFinaleStartButton;

        private GameObject _cachedCharacterTitle;
        private int _grandFinaleId;

        private const int TicketCountToUse = 1;
        private ArenaSheet.RoundData _roundData;
        private ArenaParticipantModel _info;

        private readonly List<IDisposable> _disposables = new();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay
        {
            get
            {
                var blockIndex = Game.Game.instance.Agent.BlockIndex;
                var currentRound =
                    TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
                var ticketCount = RxProps.ArenaInfoTuple.HasValue
                    ? RxProps.ArenaInfoTuple.Value.current.GetTicketCount(
                        blockIndex,
                        currentRound.StartBlockIndex,
                        States.Instance.GameConfigState.DailyArenaInterval)
                    : 0;
                return ticketCount >= TicketCountToUse;
            }
        }

        #region override

        protected override void Awake()
        {
            closeButton.onClick.AddListener(() =>
            {
                Close();
                Find<ArenaBoard>().Show();
            });

            CloseWidget = () => Close(true);
            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize();
            startButton.SetCost(CostType.ArenaTicket, TicketCountToUse);
            startButton.OnSubmitSubject
                .Where(_ => !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            repeatPopupButton.OnClickAsObservable()
                .Subscribe(_ => ShowArenaTicketPopup())
                .AddTo(gameObject);

            grandFinaleStartButton.OnSubmitSubject
                .Where(_ => !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickGrandFinale())
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            ArenaSheet.RoundData roundData,
            ArenaParticipantModel info,
            int chooseAvatarCp,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _roundData = roundData;
            _info = info;
            enemyCp.text = chooseAvatarCp.ToString();
            UpdateStartButton(false);
            information.UpdateInventory(BattleType.Arena, chooseAvatarCp);
            coverToBlockClick.SetActive(false);
            AgentStateSubject.Crystal.Subscribe(_ => ReadyToBattle()).AddTo(_disposables);
        }

        public void Show(
            int grandFinaleId,
            ArenaParticipantModel info,
            int chooseAvatarCp,
            bool ignoreShowAnimation = false)
        {
            _grandFinaleId = grandFinaleId;
            enemyCp.text = chooseAvatarCp.ToString();
            _info = info;
            UpdateStartButton(true);
            information.UpdateInventory(BattleType.Arena, chooseAvatarCp);
            coverToBlockClick.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        public void UpdateInventory()
        {
            information.UpdateInventory(BattleType.Arena);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void ReadyToBattle()
        {
            startButton.UpdateObjects();
            foreach (var particle in particles)
            {
                if (startButton.IsSubmittable)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }
        }

        private void ShowArenaTicketPopup()
        {
            Find<ArenaTicketPopup>().Show(SendBattleArenaAction);
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            var arenaTicketCost = startButton.ArenaTicketCost;
            var hasEnoughTickets =
                RxProps.ArenaTicketsProgress.HasValue &&
                RxProps.ArenaTicketsProgress.Value.currentTickets >= arenaTicketCost;
            if (hasEnoughTickets)
            {
                StartCoroutine(CoBattleStart(CostType.ArenaTicket));
                return;
            }

            var balance = States.Instance.GoldBalanceState.Gold;
            var currentArenaInfo = RxProps.ArenaInfoTuple.Value.current;
            var cost = ArenaHelper.GetTicketPrice(
                _roundData,
                currentArenaInfo,
                balance.Currency);

            Find<ArenaTicketPurchasePopup>().Show(
                CostType.ArenaTicket,
                CostType.NCG,
                balance,
                cost,
                () => StartCoroutine(CoBattleStart(CostType.NCG)),
                GoToMarket,
                currentArenaInfo.PurchasedTicketCount,
                _roundData.MaxPurchaseCount,
                RxProps.ArenaTicketsProgress.Value.purchasedCountDuringInterval,
                _roundData.MaxPurchaseCountWithInterval
            );
        }

        private IEnumerator CoBattleStart(CostType costType)
        {
            coverToBlockClick.SetActive(true);
            var game = Game.Game.instance;
            game.IsInWorld = true;
            game.Stage.IsShowHud = true;

            var headerMenuStatic = Find<HeaderMenuStatic>();
            var currencyImage = costType switch
            {
                CostType.NCG => headerMenuStatic.Gold.IconImage,
                CostType.ActionPoint => headerMenuStatic.ActionPoint.IconImage,
                CostType.Hourglass => headerMenuStatic.Hourglass.IconImage,
                CostType.Crystal => headerMenuStatic.Crystal.IconImage,
                CostType.ArenaTicket => headerMenuStatic.ArenaTickets.IconImage,
                _ or CostType.None => throw new ArgumentOutOfRangeException(
                    nameof(costType), costType, null)
            };
            var itemMoveAnimation = ItemMoveAnimation.Show(
                currencyImage.sprite,
                currencyImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            yield return new WaitWhile(() => itemMoveAnimation.IsPlaying);

            SendBattleArenaAction();
            AudioController.PlayClick();
        }

        private void SendBattleArenaAction(int ticket = TicketCountToUse)
        {
            startButton.gameObject.SetActive(false);
            var playerAvatar = States.Instance.CurrentAvatarState;
            Find<ArenaBattleLoadingScreen>().Show(
                playerAvatar.NameWithHash,
                playerAvatar.level,
                Util.GetPortraitId(BattleType.Arena),
                playerAvatar.address,
                _info.NameWithHash,
                _info.Level,
                _info.PortraitId,
                _info.AvatarAddr);

            var costumes = States.Instance.CurrentItemSlotStates[BattleType.Arena].Costumes;
            var equipments = States.Instance.CurrentItemSlotStates[BattleType.Arena].Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                .GetEquippedRuneSlotInfos();
            ActionRenderHandler.Instance.Pending = true;
            ActionManager.Instance.BattleArena(
                    _info.AvatarAddr,
                    costumes,
                    equipments,
                    runeInfos,
                    _roundData.ChampionshipId,
                    _roundData.Round,
                    ticket)
                .Subscribe();
        }

        private void SendBattleGrandFinaleAction()
        {
            startButton.gameObject.SetActive(false);
            var playerAvatar = States.Instance.CurrentAvatarState;
            Find<ArenaBattleLoadingScreen>().Show(
                playerAvatar.NameWithHash,
                playerAvatar.level,
                Util.GetPortraitId(BattleType.Arena),
                playerAvatar.address,
                _info.NameWithHash,
                _info.Level,
                _info.PortraitId,
                _info.AvatarAddr);

            var costumes = States.Instance.CurrentItemSlotStates[BattleType.Arena].Costumes;
            var equipments = States.Instance.CurrentItemSlotStates[BattleType.Arena].Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                .GetEquippedRuneSlotInfos();

            ActionRenderHandler.Instance.Pending = true;
            ActionManager.Instance.BattleGrandFinale(
                    _info.AvatarAddr,
                    costumes,
                    equipments,
                    _grandFinaleId)
                .Subscribe();
        }

        private void OnClickGrandFinale()
        {
            AudioController.PlayClick();

            if (Game.Game.instance.IsInWorld)
            {
                return;
            }

            var game = Game.Game.instance;
            game.IsInWorld = true;
            game.Stage.IsShowHud = true;
            SendBattleGrandFinaleAction();
        }

        public void OnRenderBattleArena(ActionEvaluation<BattleArena> eval)
        {
            if (eval.Exception is { })
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close(true);
            Find<ArenaBattleLoadingScreen>().Close();
        }

        public void OnRenderBattleArena(ActionEvaluation<BattleGrandFinale> eval)
        {
            if (eval.Exception is { })
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close(true);
            Find<ArenaBattleLoadingScreen>().Close();
        }

        // This method subscribe BlockIndexSubject. Be careful of duplicate subscription.
        private void UpdateStartButton(bool isGrandFinale)
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Arena);
            var runes = States.Instance.GetEquippedRuneStates(BattleType.Arena)
                .Select(x => x.RuneId).ToList();
            var consumables = information.GetEquippedConsumables().Select(x => x.Id).ToList();

            var isEquipmentValid = Util.CanBattle(equipments, costumes, consumables);
            var isIntervalValid = IsIntervalValid(Game.Game.instance.Agent.BlockIndex);

            SetStartButton(isGrandFinale, isEquipmentValid && isIntervalValid, isEquipmentValid);
            if (isEquipmentValid && !isIntervalValid)
            {
                Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                    .Subscribe(blockIndex =>
                    {
                        SetStartButton(isGrandFinale, IsIntervalValid(blockIndex), true);
                    }).AddTo(_disposables);
            }

            grandFinaleStartButton.Interactable = isGrandFinale;
            startButton.Interactable = !isGrandFinale;
        }

        private static bool IsIntervalValid(long blockIndex)
        {
            var lastBattleBlockIndex = RxProps.LastBattleBlockIndex.Value;
            var battleArenaInterval = States.Instance.GameConfigState.BattleArenaInterval;

            return blockIndex - lastBattleBlockIndex >= battleArenaInterval;
        }

        private void SetStartButton(bool isGrandFinale, bool canBattle, bool isEquipValid)
        {
            startButton.gameObject.SetActive(!isGrandFinale && canBattle);
            grandFinaleStartButton.gameObject.SetActive(isGrandFinale && canBattle);
            blockStartingText.gameObject.SetActive(!canBattle);
            repeatPopupButton.gameObject.SetActive(!isGrandFinale && canBattle &&
                                                   _roundData.ArenaType == ArenaType.OffSeason);

            if (!canBattle)
            {
                var battleArenaInterval = States.Instance.GameConfigState.BattleArenaInterval;
                blockStartingText.text = isEquipValid
                    ? L10nManager.Localize("UI_BATTLE_INTERVAL", battleArenaInterval)
                    : L10nManager.Localize("UI_EQUIP_FAILED");
            }
        }

        private void GoToMarket()
        {
            Close(true);
            Find<ShopBuy>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
        }
    }
}
