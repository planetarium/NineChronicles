using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mixpanel;
using Nekoyume.Blockchain;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using TMPro;
using UnityEngine.UI;
using System;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Nekoyume.UI.Module;
    using System.Linq;
    using UniRx;

    public class AdventureBossPreparation : Widget
    {
        public enum AdventureBossPreparationType
        {
            Challenge,
            BreakThrough,
        }

        [SerializeField]
        private AvatarInformation information;

        [SerializeField]
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private ParticleSystem[] particles;

        [SerializeField]
        private ConditionalCostButton startButton;

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
        private GameObject coverToBlockClick = null;

        [SerializeField]
        private GameObject hasBg;

        [SerializeField]
        private GameObject blockStartingTextObject;

        private AdventureBossPreparationType _type;

        private long _requiredCost;

        private readonly List<IDisposable> _disposables = new();

        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (startButton.Interactable);

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

            BattleRenderer.Instance.OnPrepareStage += GoToPrepareStage;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BattleRenderer.Instance.OnPrepareStage -= GoToPrepareStage;
        }

        public override void Initialize()
        {
            base.Initialize();

            information.Initialize();

            startButton.OnSubmitSubject
                .Where(_ => !BattleRenderer.Instance.IsOnBattle)
                .ThrottleFirst(TimeSpan.FromSeconds(1f))
                .Subscribe(_ => OnClickBattle())
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());
        }

        public void Show(
            string closeButtonName,
            long requiredCost,
            AdventureBossPreparationType type,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _type = type;
            _requiredCost = requiredCost;
            Analyzer.Instance.Track("Unity/Click AdventureBoss Prepareation", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });

            var evt = new AirbridgeEvent("Click_AdventureBoss_Prepareation");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            UpdateStartButton();
            information.UpdateInventory(BattleType.Adventure);
            UpdateRequiredCostByStageId();

            closeButtonText.text = closeButtonName;

            startButton.gameObject.SetActive(true);
            startButton.Interactable = true;
            coverToBlockClick.SetActive(false);

            ReactiveAvatarState.ObservableActionPoint
                .Subscribe(_ => UpdateStartButton())
                .AddTo(_disposables);

            ReactiveAvatarState.Inventory.Subscribe(_ => UpdateStartButton()).AddTo(_disposables);
            if (information.TryGetCellByIndex(0, out var firstCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.InventoryFirstCell,
                    rectTransform = (RectTransform)firstCell.transform
                });
            }

            if (information.TryGetCellByIndex(1, out var secondCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.InventorySecondCell,
                    rectTransform = (RectTransform)secondCell.transform
                });
            }
        }

        public void UpdateInventory()
        {
            information.UpdateInventory(BattleType.Adventure);
        }

        public void UpdateInventoryView()
        {
            information.UpdateInventory(BattleType.Adventure);
            information.UpdateView(BattleType.Adventure);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void UpdateRequiredCostByStageId()
        {
            startButton.SetCost(CostType.ApPotion, _requiredCost);
        }

        private void OnClickBattle()
        {
            AudioController.PlayClick();

            if (BattleRenderer.Instance.IsOnBattle)
            {
                return;
            }

            StartCoroutine(CoBattleStart());

            coverToBlockClick.SetActive(true);
        }

        private IEnumerator CoBattleStart()
        {
            var game = Game.Game.instance;
            game.Stage.IsShowHud = true;
            BattleRenderer.Instance.IsOnBattle = true;

            var headerMenuStatic = Find<HeaderMenuStatic>();

            //todo change AP Potion
            var currencyImage = headerMenuStatic.ApPotion.IconImage;
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

            AdventureBossBattleAction();
        }

        private void AdventureBossBattleAction()
        {
            Find<WorldMap>().Close(true);
            Find<AdventureBoss>().Close(true);
            Widget.Find<LoadingScreen>().Show(loadingType: LoadingScreen.LoadingType.AdventureBoss);
            startButton.gameObject.SetActive(false);
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure]
                .GetEquippedRuneSlotInfos();
            var consumables = information.GetEquippedConsumables().Select(x => x.ItemId).ToList();

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;

            var skillState = States.Instance.CrystalRandomSkillState;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            try
            {
                switch (_type)
                {
                    case AdventureBossPreparationType.Challenge:
                        if (Game.Game.instance.AdventureBossData.SeasonInfo?.Value is null)
                        {
                            NcDebug.LogError("[ExploreAdventureBoss] : Game.Game.instance.AdventureBossData.SeasonInfo is null or States.Instance.CurrentAvatarState is null");
                        }
                        else
                        {
                            ActionManager.Instance.ExploreAdventureBoss(costumes, equipments, consumables, runeInfos, (int)Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season);
                        }
                        break;
                    case AdventureBossPreparationType.BreakThrough:
                        if (Game.Game.instance.AdventureBossData.SeasonInfo?.Value is null)
                        {
                            NcDebug.LogError("[SweepAdventureBoss] : Game.Game.instance.AdventureBossData.SeasonInfo is null or States.Instance.CurrentAvatarState is null");
                        }
                        else
                        {
                            ActionManager.Instance.SweepAdventureBoss(costumes, equipments, runeInfos, (int)Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Widget.Find<LoadingScreen>().Close();
                NcDebug.LogError(e);
            }
        }

        private void GoToPrepareStage(BattleLog battleLog)
        {
            if (!IsActive() || !Find<LoadingScreen>().IsActive())
                return;

            StartCoroutine(CoGoToStage(battleLog));
        }

        private IEnumerator CoGoToStage(BattleLog battleLog)
        {
            yield return BattleRenderer.Instance.LoadStageResources(battleLog);

            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToMarket(TradeType tradeType)
        {
            Close(true);
            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            switch (tradeType)
            {
                case TradeType.Buy:
                    Find<ShopBuy>().Show();
                    break;
                case TradeType.Sell:
                    Find<ShopSell>().Show();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tradeType), tradeType, null);
            }
        }

        private void UpdateStartButton()
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

            const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.Sweep;
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var consumables = information.GetEquippedConsumables().Select(x => x.Id).ToList();
            var canBattle = Util.CanBattle(equipments, costumes, consumables);
            var canSweep = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage);

            startButton.gameObject.SetActive(canBattle);
            blockStartingTextObject.SetActive(!canBattle);
        }

        public void TutorialActionClickBattlePreparationFirstInventoryCellView()
        {
            try
            {
                if (information.TryGetFirstCell(out var item))
                {
                    item.Selected.Value = true;
                }
                else
                {
                    NcDebug.LogError($"TutorialActionClickBattlePreparationFirstInventoryCellView() throw error.");
                }

                Find<EquipmentTooltip>().OnEnterButtonArea(true);
            }
            catch
            {
                NcDebug.LogError($"TryGetFirstCell throw error.");
            }
        }

        public void TutorialActionClickBattlePreparationSecondInventoryCellView()
        {
            try
            {
                var itemCell = information.GetBestEquipmentInventoryItems();
                if (itemCell is null)
                {
                    NcDebug.LogError($"information.GetBestEquipmentInventoryItems().ElementAtOrDefault(0) is null");
                    return;
                }

                itemCell.Selected.Value = true;
                Find<EquipmentTooltip>().OnEnterButtonArea(true);
            }
            catch
            {
                NcDebug.LogError($"GetSecondCell throw error.");
            }
        }

        public void TutorialActionClickBattlePreparationHackAndSlash()
        {
            OnClickBattle();
        }
    }
}
