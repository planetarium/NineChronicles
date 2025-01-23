using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
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
    using System.Threading.Tasks;
    using GeneratedApiNamespace.ArenaServiceClient;
    using Libplanet.Types.Assets;
    using Nekoyume.ApiClient;
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

        [SerializeField]
        [Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField]
        [Range(0f, 10f)]
        [Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick;

        [SerializeField]
        private TextMeshProUGUI blockStartingText;

        [SerializeField]
        private TextMeshProUGUI enemyCp;

        private GameObject _cachedCharacterTitle;

        private const int TicketCountToUse = 1;
        private SeasonResponse _seasonData;
        private AvailableOpponentResponse _info;

        private readonly List<IDisposable> _disposables = new();

        private int? _chooseAvatarCp;

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable || !EnoughToPlay);

        private bool EnoughToPlay
        {
            get
            {
                var ticketCount = RxProps.ArenaInfo.HasValue
                    ? RxProps.ArenaInfo.Value.BattleTicketStatus.RemainingTicketsPerRound : 0;
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
                .Where(_ => !BattleRenderer.Instance.IsOnBattle)
                .ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            repeatPopupButton.OnClickAsObservable()
                .Subscribe(_ => ShowArenaTicketPopup())
                .AddTo(gameObject);
        }

        public void Show(
            SeasonResponse seasonData,
            AvailableOpponentResponse info,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _seasonData = seasonData;
            _info = info;

            _chooseAvatarCp = (int)info.Cp;
            enemyCp.text = _chooseAvatarCp.ToString();
            UpdateStartButton();
            information.UpdateInventory(BattleType.Arena, _chooseAvatarCp);
            coverToBlockClick.SetActive(false);
            AgentStateSubject.Crystal.Subscribe(_ => ReadyToBattle()).AddTo(_disposables);
        }

        public void UpdateInventory()
        {
            information.UpdateInventory(BattleType.Arena, _chooseAvatarCp);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _chooseAvatarCp = null;
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
            Find<ArenaTicketPopup>().Show();
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (BattleRenderer.Instance.IsOnBattle)
            {
                return;
            }

            var arenaTicketCost = startButton.GetCost(CostType.ArenaTicket);
            var hasEnoughTickets =
                RxProps.ArenaTicketsProgress.HasValue &&
                RxProps.ArenaTicketsProgress.Value.currentTickets >= arenaTicketCost;
            if (hasEnoughTickets)
            {
                StartCoroutine(CoBattleStart(CostType.ArenaTicket));
                return;
            }

            var balance = States.Instance.GoldBalanceState.Gold;
            var currentArenaInfo = RxProps.ArenaInfo.Value;

            Find<ArenaTicketPurchasePopup>().Show(
                CostType.ArenaTicket,
                CostType.NCG,
                balance,
                new FungibleAssetValue(),
                () => StartCoroutine(CoBattleStart(CostType.NCG)),
                currentArenaInfo.BattleTicketStatus.RemainingTicketsPerRound,
                _seasonData.BattleTicketPolicy.DefaultTicketsPerRound,
                RxProps.ArenaTicketsProgress.Value.purchasedCountDuringInterval,
                _seasonData.BattleTicketPolicy.MaxPurchasableTicketsPerRound
            );
        }

        private IEnumerator CoBattleStart(CostType costType)
        {
            coverToBlockClick.SetActive(true);
            var game = Game.Game.instance;
            game.Stage.IsShowHud = true;
            BattleRenderer.Instance.IsOnBattle = true;

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
                new Libplanet.Crypto.Address(_info.AvatarAddress));

            var tokenTask = ApiClients.Instance.Arenaservicemanager.GetBattleTokenAsync(_info.AvatarAddress, playerAvatar.address.ToHex());
            tokenTask.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    var token = task.Result;
                    RxProps.LastBattleId = token.BattleId;
                    // 성공시 호출할 콜백
                    try
                    {
                        var costumes = States.Instance.CurrentItemSlotStates[BattleType.Arena].Costumes;
                        var equipments = States.Instance.CurrentItemSlotStates[BattleType.Arena].Equipments;
                        var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                            .GetEquippedRuneSlotInfos();
                        ActionRenderHandler.Instance.Pending = true;
                        ActionManager.Instance.BattleArena(
                                new Libplanet.Crypto.Address(_info.AvatarAddress),
                                costumes,
                                equipments,
                                runeInfos,
                                _seasonData.Id,
                                _seasonData.Id,
                                token);
                    }
                    catch (Exception e)
                    {
                        Game.Game.BackToMainAsync(e).Forget();
                    }
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // 오류 처리
                    NcDebug.LogError("토큰 요청에 실패했습니다. 오류: " + task.Exception?.Message);
                    Game.Game.BackToMainAsync(task.Exception).Forget();
                }
            });
        }

        public void OnRenderBattleArena(ActionEvaluation<Action.Arena.Battle> eval)
        {
            if (eval.Exception is not null)
            {
                Find<ArenaBattleLoadingScreen>().Close();
                return;
            }

            Close(true);
            Find<ArenaBattleLoadingScreen>().Close();
        }

        // This method subscribe BlockIndexSubject. Be careful of duplicate subscription.
        private void UpdateStartButton()
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Arena);
            var consumables = information.GetEquippedConsumables().Select(x => x.Id).ToList();

            var isEquipmentValid = Util.CanBattle(equipments, costumes, consumables);
            var isIntervalValid = IsIntervalValid(Game.Game.instance.Agent.BlockIndex);

            SetStartButton(isEquipmentValid && isIntervalValid, isEquipmentValid);
            if (isEquipmentValid && !isIntervalValid)
            {
                Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                    .Subscribe(blockIndex => { SetStartButton(IsIntervalValid(blockIndex), true); }).AddTo(_disposables);
            }

            startButton.Interactable = true;
        }

        private static bool IsIntervalValid(long blockIndex)
        {
            var lastBattleBlockIndex = RxProps.LastArenaBattleBlockIndex.Value;
            var battleArenaInterval = States.Instance.GameConfigState.BattleArenaInterval;

            return blockIndex - lastBattleBlockIndex >= battleArenaInterval;
        }

        private void SetStartButton(bool canBattle, bool isEquipValid)
        {
            startButton.gameObject.SetActive(canBattle);
            blockStartingText.gameObject.SetActive(!canBattle);
            repeatPopupButton.gameObject.SetActive(canBattle &&
                _seasonData.ArenaType == ArenaType.OFF_SEASON);

            if (!canBattle)
            {
                var battleArenaInterval = States.Instance.GameConfigState.BattleArenaInterval;
                blockStartingText.text = isEquipValid
                    ? L10nManager.Localize("UI_BATTLE_INTERVAL", battleArenaInterval)
                    : L10nManager.Localize("UI_EQUIP_FAILED");
            }
        }
    }
}
