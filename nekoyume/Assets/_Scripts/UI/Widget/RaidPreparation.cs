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
using Nekoyume.Model.EnumType;
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
        private GameObject blockStartingTextObject;

        [SerializeField]
        private GameObject crystalContainer;

        [SerializeField]
        private GameObject currencyContainer;

        private int _requiredCost;
        private int _bossId;
        private readonly List<IDisposable> _disposables = new();
        private HeaderMenuStatic _headerMenu;

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

            closeButtonText.text = L10nManager.LocalizeCharacterName(bossId);

            UpdateStartButton();
            information.UpdateInventory(BattleType.Raid);


            coverToBlockClick.SetActive(false);

            AgentStateSubject.Crystal
                .Subscribe(_ => UpdateCrystalCost())
                .AddTo(_disposables);

            // DevCra - iOS Memory Optimization
            this.transform.SetAsLastSibling();
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
            var cost = Convert.ToInt32(fee.GetQuantityString());
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
            var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Raid);
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();
            var tableSheets = Game.Game.instance.TableSheets;
            var avatarState = States.Instance.CurrentAvatarState;
            var items = new List<Guid>();
            items.AddRange(equipments);
            items.AddRange(costumes);
            avatarState.EquipItems(items);
            var simulator = new RaidSimulator(
                _bossId,
                new PracticeRandom(),
                avatarState,
                consumables,
                runeStates,
                tableSheets.GetRaidSimulatorSheets(),
                tableSheets.CostumeStatSheet
            );
            var log = simulator.Simulate();
            var digest = new ArenaPlayerDigest(avatarState,
                itemSlotState.Equipments,
                itemSlotState.Costumes,
                runeStates);
            var raidStage = Game.Game.instance.RaidStage;
            raidStage.Play(
                avatarState.address,
                simulator.BossId,
                log,
                digest,
                simulator.DamageDealt,
                false,
                true,
                null,
                new List<FungibleAssetValue>());

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
            Find<LoadingScreen>().Show();
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

        private void UpdateStartButton()
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Raid);
            var runes = States.Instance.GetEquippedRuneStates(BattleType.Raid)
                .Select(x=> x.RuneId).ToList();
            var consumables = information.GetEquippedConsumables().Select(x=> x.Id).ToList();
            var canBattle = Util.CanBattle(equipments, costumes, consumables);
            startButton.gameObject.SetActive(canBattle);
            blockStartingTextObject.SetActive(!canBattle);
        }
    }
}
