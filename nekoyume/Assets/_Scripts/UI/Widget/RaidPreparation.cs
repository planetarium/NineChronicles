using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using Nekoyume.Model;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Model.EnumType;
using UnityEngine.Serialization;
using Toggle = UnityEngine.UI.Toggle;

namespace Nekoyume.UI
{
    using UniRx;
    public class RaidPreparation : Widget
    {
        private class PracticeRandom : System.Random, IRandom
        {
            public PracticeRandom() : base(Guid.NewGuid().GetHashCode())
            {
            }

            public int Seed => throw new NotImplementedException();
        }

        [SerializeField]
        private Toggle toggle;

        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private Button startButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private TextMeshProUGUI crystalText;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private Transform crystalImage;

        [SerializeField]
        private Transform ticketImage;

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        [SerializeField]
        private GameObject coverToBlockClick = null;

        [SerializeField]
        private TMP_Text blockStartingText;

        [SerializeField]
        private GameObject crystalContainer;

        [SerializeField]
        private GameObject currencyContainer;

        private int _requiredCost;
        private int _bossId;
        private readonly List<IDisposable> _disposables = new();
        private HeaderMenuStatic _headerMenu;

        private long _txStageBlockIndex;

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent && startButton.enabled;

        public bool IsSkipRender => toggle.isOn;

        #region override

        protected override void Awake()
        {
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                AudioController.PlayClick();
            });

            CloseWidget = () => Close(true);
            base.Awake();
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize();

            startButton.onClick.AddListener(OnClickStartButton);

            Game.Event.OnRoomEnter.AddListener(b => Close());
            toggle.gameObject.SetActive(GameConfig.IsEditor);
        }

        public void Show(int bossId, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _bossId = bossId;
            _headerMenu = Find<HeaderMenuStatic>();
            var avatarState = States.Instance.CurrentAvatarState;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var raiderState = WorldBossStates.GetRaiderState(avatarState.address);
            startButton.gameObject.SetActive(true);
            startButton.enabled = true;
            if (WorldBossFrontHelper.IsItInSeason(currentBlockIndex))
            {
                currencyContainer.SetActive(true);
                ticketText.color = _headerMenu.WorldBossTickets.RemainTicket > 0 ?
                    Palette.GetColor(ColorType.ButtonEnabled) :
                    Palette.GetColor(ColorType.TextDenial);
                if (raiderState is null)
                {
                    crystalContainer.SetActive(true);
                    UpdateCrystalCost();
                }
                else
                {
                    crystalContainer.SetActive(false);
                }
            }
            else
            {
                currencyContainer.SetActive(false);
                crystalContainer.SetActive(false);
            }

            UpdateStartButton(currentBlockIndex);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(UpdateStartButton).AddTo(_disposables);

            information.UpdateInventory(BattleType.Raid);

            coverToBlockClick.SetActive(false);

            AgentStateSubject.Crystal
                .Subscribe(_ => UpdateCrystalCost())
                .AddTo(_disposables);
        }

        public void UpdateInventory()
        {
            information.UpdateInventory(BattleType.Raid);
        }

        private void UpdateCrystalCost()
        {
            var crystalCost = GetEntranceFee(Game.Game.instance.States.CurrentAvatarState);
            crystalText.text = $"{crystalCost:#,0}";
            crystalText.color = States.Instance.CrystalBalance.MajorUnit >= crystalCost ?
                Palette.GetColor(ColorType.ButtonEnabled) :
                Palette.GetColor(ColorType.TextDenial);
        }

        private static int GetEntranceFee(AvatarState currentAvatarState)
        {
            var worldBossListSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var row = worldBossListSheet.FindRowByBlockIndex(currentBlockIndex);
            var fee = CrystalCalculator.CalculateEntranceFee(
                currentAvatarState.level, row.EntranceFee);
            var cost = MathematicsExtensions.ConvertToInt32(fee.GetQuantityString());
            return cost;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void OnClickStartButton()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            AudioController.PlayClick();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);

            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    PracticeRaid();
                    break;
                case WorldBossStatus.Season:
                    var raiderState = WorldBossStates.GetRaiderState(avatarState.address);
                    if (raiderState is null)
                    {
                        var cost = GetEntranceFee(avatarState);
                        if (States.Instance.CrystalBalance.MajorUnit < cost)
                        {
                            Find<PaymentPopup>().ShowAttract(
                                CostType.Crystal,
                                cost,
                                L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                                L10nManager.Localize("UI_GO_GRINDING"),
                                () =>
                                {
                                    Find<Grind>().Show();
                                    Find<WorldBoss>().ForceClose();
                                    Close();
                                });
                        }
                        else
                        {
                            Find<PaymentPopup>()
                                .ShowWithAddCost("UI_TOTAL_COST", "UI_BOSS_JOIN_THE_SEASON",
                                    CostType.Crystal, cost,
                                    CostType.WorldBossTicket, 1,
                                    () => StartCoroutine(CoRaid()));
                        }
                    }
                    else
                    {
                        if (_headerMenu.WorldBossTickets.RemainTicket > 0)
                        {
                            StartCoroutine(CoRaid());
                        }
                        else
                        {
                            ShowTicketPurchasePopup(currentBlockIndex);
                        }
                    }

                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PracticeRaid()
        {
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Raid];
            var equipments = itemSlotState.Equipments;
            var costumes = itemSlotState.Costumes;
            var allRuneState = States.Instance.AllRuneState;
            var runeSlotState = States.Instance.CurrentRuneSlotStates[BattleType.Raid];
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();
            var tableSheets = Game.Game.instance.TableSheets;
            var avatarState = States.Instance.CurrentAvatarState;
            var collectionState = Game.Game.instance.States.CollectionState;
            var items = new List<Guid>();
            items.AddRange(equipments);
            items.AddRange(costumes);
            avatarState.EquipItems(items);
            var simulator = new RaidSimulator(
                _bossId,
                new PracticeRandom(),
                avatarState,
                consumables,
                allRuneState,
                runeSlotState,
                tableSheets.GetRaidSimulatorSheets(),
                tableSheets.CostumeStatSheet,
                collectionState.GetEffects(tableSheets.CollectionSheet),
                tableSheets.DeBuffLimitSheet,
                tableSheets.BuffLinkSheet
            );
            var log = simulator.Simulate();
            var digest = new ArenaPlayerDigest(
                avatarState,
                itemSlotState.Equipments,
                itemSlotState.Costumes,
                allRuneState,
                runeSlotState);
            var raidStage = Game.Game.instance.RaidStage;
            var raidStartData = new RaidStage.RaidStartData(
                avatarState.address,
                simulator.BossId,
                log,
                digest,
                simulator.DamageDealt,
                false,
                true,
                null,
                new List<FungibleAssetValue>());

            raidStage.Play(raidStartData);

            Find<WorldBoss>().ForceClose();
            Close();
        }

        private IEnumerator CoRaid()
        {
            startButton.enabled = false;
            coverToBlockClick.SetActive(true);

            var ticketAnimation = ShowMoveTicketAnimation();
            var avatarState = States.Instance.CurrentAvatarState;
            var raiderState = WorldBossStates.GetRaiderState(avatarState.address);
            if (raiderState is null)
            {
                var crystalAnimation = ShowMoveCrystalAnimation();
                yield return new WaitWhile(() => ticketAnimation.IsPlaying || crystalAnimation.IsPlaying);
            }
            else
            {
                yield return new WaitWhile(() => ticketAnimation.IsPlaying);
            }

            _txStageBlockIndex = Game.Game.instance.Agent.BlockIndex;

            Raid(false);
        }

        private ItemMoveAnimation ShowMoveTicketAnimation()
        {
            var tickets = _headerMenu.WorldBossTickets;
            return ItemMoveAnimation.Show(tickets.IconImage.sprite,
                tickets.transform.position,
                ticketImage.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
        }

        private ItemMoveAnimation ShowMoveCrystalAnimation()
        {
            var crystal = _headerMenu.Crystal;
            return ItemMoveAnimation.Show(crystal.IconImage.sprite,
                crystal.transform.position,
                crystalImage.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
        }

        private void Raid(bool payNcg)
        {
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Raid];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Raid]
                .GetEquippedRuneSlotInfos();

            ActionManager.Instance.Raid(costumes, equipments, consumables, runeInfos, payNcg).Subscribe();
            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.WorldBoss);
            Find<WorldBoss>().ForceClose(true);
            Close();
        }

        private void ShowTicketPurchasePopup(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
            {
                return;
            }

            var avatarState = States.Instance.CurrentAvatarState;
            var raiderState = WorldBossStates.GetRaiderState(avatarState.address);
            var cur = States.Instance.GoldBalanceState.Gold.Currency;
            var cost = WorldBossHelper.CalculateTicketPrice(row, raiderState, cur);
            var balance = States.Instance.GoldBalanceState;
            Find<TicketPurchasePopup>().Show(
                CostType.WorldBossTicket,
                CostType.NCG,
                balance.Gold,
                cost,
                raiderState.PurchaseCount,
                row.MaxPurchaseCount,
                () =>
                {
                    coverToBlockClick.SetActive(true);
                    Raid(true);
                },
                GoToMarket
            );
        }

        private void GoToMarket()
        {
            Find<WorldBoss>().ForceClose();
            Find<RaidPreparation>().Close(true);
            Find<ShopBuy>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
        }

        /// <summary>
        /// 마지막으로 Raid 전투를 수행했던 BlockIndex값을 가져옵니다.
        /// RaidState가 없는경우 0을 리턴합니다.
        /// </summary>
        /// <returns>마지막으로 Raid전투를 수행했떤 BlockIndex, raidState가 없는경우 0리턴</returns>
        private long GetUpdatedRaidBlockIndex()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var raiderState = WorldBossStates.GetRaiderState(avatarState.address);
            var raidBlockIndex = Math.Max(raiderState?.UpdatedBlockIndex ?? 0, _txStageBlockIndex);
            return raidBlockIndex;
        }

        private void UpdateStartButton(long blockIndex)
        {
            var worldBossRequiredInterval = States.Instance.GameConfigState.WorldBossRequiredInterval;
            var isIntervalValid = blockIndex - GetUpdatedRaidBlockIndex() >= worldBossRequiredInterval;

            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Raid);
            var consumables = information.GetEquippedConsumables().Select(x=> x.Id).ToList();

            var isEquipValid = Util.CanBattle(equipments, costumes, consumables);
            var canBattle = isEquipValid && isIntervalValid;
            startButton.gameObject.SetActive(canBattle);
            blockStartingText.gameObject.SetActive(!canBattle);

            if (canBattle)
            {
                return;
            }

            blockStartingText.text = isEquipValid
                ? L10nManager.Localize("UI_BATTLE_INTERVAL", worldBossRequiredInterval)
                : L10nManager.Localize("UI_EQUIP_FAILED");
        }
    }
}
