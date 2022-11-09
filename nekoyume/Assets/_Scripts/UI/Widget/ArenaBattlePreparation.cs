using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.State.Subjects;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaBattlePreparation : Widget
    {
        [SerializeField]
        private AvatarInformation information;

        [SerializeField] private ParticleSystem[] particles;

        [SerializeField] private ConditionalCostButton startButton;

        [SerializeField] private Button closeButton;

        [SerializeField] private Transform buttonStarImageTransform;

        [SerializeField, Range(.5f, 3.0f)] private float animationTime = 1f;

        [SerializeField] private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField] private GameObject coverToBlockClick;

        [SerializeField] private GameObject blockStartingTextObject;

        private ArenaSheet.RoundData _roundData;
        private AvatarState _chooseAvatarState;
        private const int _ticketCountToUse = 1;

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
                var ticketCount = RxProps.PlayersArenaParticipant.HasValue
                    ? RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo.GetTicketCount(
                        Game.Game.instance.Agent.BlockIndex,
                        currentRound.StartBlockIndex,
                        States.Instance.GameConfigState.DailyArenaInterval)
                    : 0;
                return ticketCount >= _ticketCountToUse;
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

            information.Initialize(true);

            startButton.SetCost(CostType.ArenaTicket, _ticketCountToUse);
            startButton.OnSubmitSubject
                .Where(_ => !Game.Game.instance.IsInWorld)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            ArenaSheet.RoundData roundData,
            AvatarState chooseAvatarState,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _roundData = roundData;
            _chooseAvatarState = chooseAvatarState;
            UpdateStartButton();
            information.UpdateInventory(BattleType.Arena);

            coverToBlockClick.SetActive(false);
            AgentStateSubject.Crystal
                .Subscribe(_ => ReadyToBattle())
                .AddTo(_disposables);

        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateArenaAvatarState()
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var arenaAvatarState = RxProps.PlayersArenaParticipant.Value.AvatarState;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

            for (int i = arenaAvatarState.inventory.Items.Count - 1; i > 0; i--)
            {
                Nekoyume.Model.Item.Inventory.Item existItem;
                switch (arenaAvatarState.inventory.Items[i].item)
                {
                    case Equipment arenaEquipment:
                    {
                        existItem = avatarState.inventory.Items.FirstOrDefault(item =>
                            item.item is Equipment equipment
                            && equipment.ItemId == arenaEquipment.ItemId);
                        break;
                    }
                    case Costume arenaCostume:
                    {
                        existItem = avatarState.inventory.Items.FirstOrDefault(item =>
                            item.item is Costume costume
                            && costume.ItemId == arenaCostume.ItemId);
                        break;
                    }
                    default:
                        continue;
                }

                if (existItem is null)
                {
                    // It cause modifying arenaAvatarState.inventory collection in a loop
                    arenaAvatarState.inventory.RemoveItem(arenaAvatarState.inventory.Items[i]);
                }
                else
                {
                    var isValid = existItem is { Locked: false, item: ITradableItem tradableItem }
                                  && tradableItem.RequiredBlockIndex <= currentBlockIndex;

                    if (arenaAvatarState.inventory.Items[i] is { item: IEquippableItem { Equipped: true } equippedItem }
                        && !isValid)
                    {
                        equippedItem.Unequip();
                    }
                }
            }
        }

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
            var cost = ArenaHelper.GetTicketPrice(
                _roundData,
                RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo,
                balance.Currency);
            var arenaInformation = RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo;

            Find<ArenaTicketPurchasePopup>().Show(
                CostType.ArenaTicket,
                CostType.NCG,
                balance,
                cost,
                () => StartCoroutine(CoBattleStart(CostType.NCG)),
                GoToMarket,
                arenaInformation.PurchasedTicketCount,
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

        private void SendBattleArenaAction()
        {
            startButton.gameObject.SetActive(false);
            var playerAvatar = RxProps.PlayersArenaParticipant.Value.AvatarState;
            Find<ArenaBattleLoadingScreen>().Show(
                playerAvatar.NameWithHash,
                playerAvatar.level,
                playerAvatar.inventory.GetEquippedFullCostumeOrArmorId(),
                _chooseAvatarState.NameWithHash,
                _chooseAvatarState.level,
                _chooseAvatarState.inventory.GetEquippedFullCostumeOrArmorId());

            // todo : 룬도 넣어줘야함.
            var runes = States.Instance.GetEquippedRuneStates(BattleType.Arena)
                .Select(x=> x.RuneId).ToList();

            var equipments = States.Instance.ItemSlotStates[BattleType.Arena].Equipments;
            var costumes = States.Instance.ItemSlotStates[BattleType.Arena].Costumes;
            ActionRenderHandler.Instance.Pending = true;
            ActionManager.Instance.BattleArena(
                    _chooseAvatarState.address,
                    equipments,
                    costumes,
                    _roundData.ChampionshipId,
                    _roundData.Round,
                    _ticketCountToUse)
                .Subscribe();
        }

        public void OnRenderBattleArena(ActionBase.ActionEvaluation<BattleArena> eval)
        {
            if (eval.Exception is { })
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close(true);
            Find<ArenaBattleLoadingScreen>().Close();
        }

        private void UpdateStartButton()
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Arena);
            var runes = States.Instance.GetEquippedRuneStates(BattleType.Arena)
                .Select(x=> x.RuneId).ToList();
            var consumables = information.GetEquippedConsumables().Select(x=> x.Id).ToList();
            var canBattle = Util.CanBattle(equipments, costumes, consumables);
            startButton.gameObject.SetActive(canBattle);
            startButton.Interactable = true;
            blockStartingTextObject.SetActive(!canBattle);
        }

        private void GoToMarket()
        {
            Close(true);
            Find<ShopBuy>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
        }
    }
}
