using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.Helper;
    using UniRx;
    using UnityEngine.Serialization;

    public class BattleResultPopup : PopupWidget
    {
        public enum NextState
        {
            None,
            GoToMain,
            RepeatStage,
            NextStage,
        }

        public class Model
        {
            private readonly List<CountableItem> _rewards = new();

            public StageType StageType;
            public NextState NextState;
            public BattleLog.Result State;
            public string WorldName;
            public long Exp;
            public int WorldID;
            public int StageID;
            public int ClearedWaveNumber;
            public long ActionPoint;
            public int LastClearedStageId;
            public bool ActionPointNotEnough;
            public bool IsClear;
            public bool IsEndStage;
            public int LastClearedStageIdBeforeResponse;

            /// <summary>
            /// [0]: The number of times a `BattleLog.clearedWaveNumber` is 0.
            /// [1]: The number of times a `BattleLog.clearedWaveNumber` is 1.
            /// [2]: The number of times a `BattleLog.clearedWaveNumber` is 2.
            /// [3]: The number of times a `BattleLog.clearedWaveNumber` is 3.
            /// </summary>
            public int[] ClearedCountForEachWaves = new int[4];

            public IReadOnlyList<CountableItem> Rewards => _rewards;

            public void AddReward(CountableItem reward)
            {
                var sameReward = _rewards.FirstOrDefault(e =>
                    e.ItemBase.Value.Equals(reward.ItemBase.Value));
                if (sameReward is null)
                {
                    _rewards.Add(reward);
                    return;
                }

                sameReward.Count.Value += reward.Count.Value;
            }
        }

        [Serializable]
        public struct RecipeItems
        {
            public GameObject gameObject;
            public RecipeCell[] items;
            public TextMeshProUGUI[] itemNames;
            public TextMeshProUGUI[] itemMainStats;
        }

        [Serializable]
        public struct RewardItems
        {
            public GameObject gameObject;
            public SimpleCountableItemView[] items;

            public void Set(IReadOnlyList<CountableItem> rewardItems)
            {
                foreach (var view in items)
                {
                    view.gameObject.SetActive(false);
                }

                for (var i = 0; i < rewardItems.Count; i++)
                {
                    var rt = items[i].RectTransform;
                    var itemBase = rewardItems[i].ItemBase.Value;
                    items[i].SetData(rewardItems[i], () => ShowTooltip(itemBase));
                    items[i].gameObject.SetActive(true);
                }
            }

            private static void ShowTooltip(ItemBase itemBase)
            {
                AudioController.PlayClick();
                var tooltip = ItemTooltip.Find(itemBase.ItemType);
                tooltip.Show(itemBase, string.Empty, false, null);
            }
        }

        [Serializable]
        public struct StarForMulti
        {
            public TextMeshProUGUI StarCountText;
            public TextMeshProUGUI[] WaveStarTexts;
            public TextMeshProUGUI RemainingStarText;
        }
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameObject victoryImageContainer;

        [SerializeField]
        private GameObject defeatImageContainer;

        [SerializeField]
        private TextMeshProUGUI worldStageId;

        [SerializeField]
        private GameObject[] enableVictorys;

        [SerializeField]
        private GameObject[] enableDefeats;

        [SerializeField]
        private GameObject cpUp;

        [SerializeField]
        private GameObject rewardArea;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private RewardItems rewardItems;

        [SerializeField]
        private RecipeItems recipeItems;

        [SerializeField]
        private GameObject startRewards;

        [SerializeField]
        private StarForMulti starForMulti;

        [SerializeField]
        private TextMeshProUGUI bottomText;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button stagePreparationButton;

        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private Button repeatButton;

        [SerializeField]
        private Button shopButton;

        [SerializeField]
        private Button craftButton;

        [SerializeField]
        [FormerlySerializedAs("foodButton")]
        private Button starButton;

        [SerializeField]
        private ActionPoint actionPoint;

        [SerializeField]
        private GameObject[] seasonPassObjs;
        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        private Coroutine _coUpdateBottomText;

        private readonly WaitForSeconds _battleWinVFXYield = new(0.2f);
        private static readonly int ClearedWave = Animator.StringToHash("ClearedWave");
        private static readonly Vector3 VfxBattleWinOffset = new(-0.05f, 1.2f, 10f);
        private const int Timer = 10;

        private Animator _victoryImageAnimator;

        private bool _IsAlreadyOut;

        private Model SharedModel { get; set; }

        public Model ModelForMultiHackAndSlash { get; set; }

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable()
                .Subscribe(_ => StartCoroutine(OnClickClose()))
                .AddTo(gameObject);

            stagePreparationButton.OnClickAsObservable()
                .Subscribe(_ => OnClickStage())
                .AddTo(gameObject);

            shopButton.OnClickAsObservable().Subscribe(_ => GoToProduct()).AddTo(gameObject);
            craftButton.OnClickAsObservable().Subscribe(_ => GoToCraft()).AddTo(gameObject);
            starButton.OnClickAsObservable().Subscribe(_ => OnClickStage()).AddTo(gameObject);

            nextButton.OnClickAsObservable()
                .Subscribe(_ => StartCoroutine(OnClickNext()))
                .AddTo(gameObject);

            repeatButton.OnClickAsObservable()
                .Subscribe(_ => StartCoroutine(OnClickRepeat()))
                .AddTo(gameObject);

            CloseWidget = closeButton.onClick.Invoke;
            SubmitWidget = nextButton.onClick.Invoke;

            _victoryImageAnimator = victoryImageContainer.GetComponent<Animator>();

            BattleRenderer.Instance.OnPrepareStage += NextPrepareStage;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BattleRenderer.Instance.OnPrepareStage -= NextPrepareStage;
        }

        private IEnumerator OnClickClose()
        {
            _IsAlreadyOut = true;
            AudioController.PlayClick();

            if (SharedModel.IsClear && SharedModel.StageType != StageType.EventDungeon)
            {
                yield return CoDialog(SharedModel.StageID);
            }

            var worldClear = false;
            if (States.Instance.CurrentAvatarState.worldInformation
                .TryGetLastClearedStageId(out var lastClearedStageId))
            {
                if (SharedModel.IsClear
                    && SharedModel.IsEndStage
                    && lastClearedStageId == SharedModel.StageID
                    && !Find<WorldMap>().SharedViewModel.UnlockedWorldIds.Contains(SharedModel.WorldID + 1)
                    && SharedModel.StageID - 1 == SharedModel.LastClearedStageIdBeforeResponse)
                {
                    worldClear = true;
                }
            }

            if (worldClear)
            {
                var worldClearPopup = Find<WorldClearPopup>();
                worldClearPopup.Show(SharedModel.WorldID, SharedModel.WorldName);
                yield return new WaitUntil(() => !worldClearPopup.Displaying);
            }

            GoToMain(worldClear);
        }

        private void OnClickStage()
        {
            _IsAlreadyOut = true;
            AudioController.PlayClick();
            GoToPreparation();
        }

        private IEnumerator OnClickNext()
        {
            if (_IsAlreadyOut)
            {
                yield break;
            }

            if (SharedModel.StageType == StageType.EventDungeon)
            {
                if (RxProps.EventScheduleRowForDungeon.Value is null)
                {
                    NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                        NotificationCell.NotificationType.Information);
                    yield break;
                }

                if (SharedModel.ActionPointNotEnough)
                {
                    var balance = States.Instance.GoldBalanceState.Gold;
                    var cost = RxProps.EventScheduleRowForDungeon.Value
                        .GetDungeonTicketCost(
                            RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0,
                            States.Instance.GoldBalanceState.Gold.Currency);
                    var purchasedCount =
                        RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0;

                    Find<TicketPurchasePopup>().Show(
                        CostType.EventDungeonTicket,
                        CostType.NCG,
                        balance,
                        cost,
                        purchasedCount,
                        1,
                        () => StartCoroutine(CoProceedNextStage(true)),
                        GoToMarket,
                        true
                    );
                    yield break;
                }
            }

            AudioController.PlayClick();
            yield return CoProceedNextStage();
        }

        private IEnumerator OnClickRepeat()
        {
            if (_IsAlreadyOut)
            {
                yield break;
            }

            if (SharedModel.StageType == StageType.EventDungeon)
            {
                if (RxProps.EventScheduleRowForDungeon.Value is null)
                {
                    NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                        NotificationCell.NotificationType.Information);
                    yield break;
                }

                if (SharedModel.ActionPointNotEnough)
                {
                    var balance = States.Instance.GoldBalanceState.Gold;
                    var cost = RxProps.EventScheduleRowForDungeon.Value
                        .GetDungeonTicketCost(
                            RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0,
                            States.Instance.GoldBalanceState.Gold.Currency);
                    var purchasedCount =
                        RxProps.EventDungeonInfo.Value?.NumberOfTicketPurchases ?? 0;

                    Find<TicketPurchasePopup>().Show(
                        CostType.EventDungeonTicket,
                        CostType.NCG,
                        balance,
                        cost,
                        purchasedCount,
                        1,
                        () => StartCoroutine(CoRepeatStage(true)),
                        GoToMarket,
                        true
                    );
                    yield break;
                }
            }

            AudioController.PlayClick();
            yield return CoRepeatStage();
        }

        private IEnumerator CoDialog(int stageId)
        {
            var stageDialogs = TableSheets.Instance.StageDialogSheet
                .OrderedList
                .Where(i => i.StageId == stageId)
                .OrderBy(i => i.DialogId)
                .ToArray();
            if (!stageDialogs.Any())
            {
                yield break;
            }

            var dialog = Find<DialogPopup>();
            foreach (var stageDialog in stageDialogs)
            {
                dialog.Show(stageDialog.DialogId);
                yield return new WaitWhile(() => dialog.gameObject.activeSelf);
            }
        }

        public void Show(Model model, bool isBoosted, List<TableData.EquipmentItemRecipeSheet.Row> newRecipes)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            if (isBoosted && model.StageType == StageType.HackAndSlash)
            {
                model = new Model
                {
                    NextState = model.NextState,
                    State = model.State,
                    WorldName = model.WorldName,
                    Exp = ModelForMultiHackAndSlash.Exp,
                    WorldID = model.WorldID,
                    StageID = model.StageID,
                    ClearedWaveNumber = model.ClearedWaveNumber,
                    ActionPoint = model.ActionPoint,
                    LastClearedStageId = model.LastClearedStageId,
                    ActionPointNotEnough = model.ActionPointNotEnough,
                    IsClear = model.IsClear,
                    IsEndStage = model.IsEndStage,
                    LastClearedStageIdBeforeResponse = model.LastClearedStageIdBeforeResponse,
                    ClearedCountForEachWaves = ModelForMultiHackAndSlash.ClearedCountForEachWaves,
                };
                foreach (var item in ModelForMultiHackAndSlash.Rewards)
                {
                    model.AddReward(item);
                }
            }

            SharedModel = model;
            _IsAlreadyOut = false;

            var stageText = StageInformation.GetStageIdString(
                SharedModel.StageType,
                SharedModel.StageID,
                true);
            worldStageId.text = $"{SharedModel.WorldName}" +
                                $" {stageText}";
            actionPoint.SetEventTriggerEnabled(true);

            base.Show();

            RefreshSeasonPassCourageAmount();

            closeButton.gameObject.SetActive(
                model.StageID >= Battle.RequiredStageForExitButton ||
                model.LastClearedStageId >= Battle.RequiredStageForExitButton);
            stagePreparationButton.gameObject.SetActive(false);
            repeatButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);

            if(newRecipes != null && newRecipes.Count > 0)
            {
                recipeItems.gameObject.SetActive(true);
                for (int i = 0; i < recipeItems.items.Length; i++)
                {
                    if(i < newRecipes.Count)
                    {
                        recipeItems.items[i].transform.parent.gameObject.SetActive(true);
                        recipeItems.items[i].Show(newRecipes[i], false);

                        var resultItem = newRecipes[i].GetResultEquipmentItemRow();
                        recipeItems.itemNames[i].text = resultItem.GetLocalizedName(true);
                        recipeItems.itemMainStats[i].text = resultItem.GetUniqueStat().DecimalStatToString();
                    }
                    else
                    {
                        recipeItems.items[i].transform.parent.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                recipeItems.gameObject.SetActive(false);
            }

            UpdateView(isBoosted);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateView(bool isBoosted)
        {
            expText.text = $"EXP + {SharedModel.Exp}";

            rewardItems.Set(SharedModel.Rewards);

            switch (SharedModel.State)
            {
                case BattleLog.Result.Win:
                    StartCoroutine(CoUpdateViewAsVictory(isBoosted));
                    break;
                case BattleLog.Result.Lose:
                    UpdateViewAsDefeat(SharedModel.State);
                    break;
                case BattleLog.Result.TimeOver:
                    if (SharedModel.ClearedWaveNumber > 0)
                    {
                        StartCoroutine(CoUpdateViewAsVictory(isBoosted));
                    }
                    else
                    {
                        UpdateViewAsDefeat(SharedModel.State);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CoUpdateViewAsVictory(bool isBoosted)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
            StartCoroutine(EmitBattleWinVFX());

            victoryImageContainer.SetActive(true);
            // 4 is index of animation about multi-has.
            // if not use multi-has, set animation index to SharedModel.ClearedWaveNumber (1/2/3).
            _victoryImageAnimator.SetInteger(
                ClearedWave,
                isBoosted
                    ? 4
                    : SharedModel.ClearedWaveNumber);

            defeatImageContainer.SetActive(false);

            foreach (var item in enableVictorys)
            {
                item.SetActive(true);
            }

            foreach (var item in enableDefeats)
            {
                item.SetActive(false);
            }

            if(SharedModel.ClearedWaveNumber == 1 && !isBoosted)
            {
                cpUp.SetActive(true);
                rewardArea.SetActive(false);
            }
            else
            {
                cpUp.SetActive(false);
                rewardArea.SetActive(true);
            }

            if (SharedModel.ClearedCountForEachWaves[3] <= 0 && (SharedModel.ClearedWaveNumber == 2 || isBoosted))
            {
                startRewards.SetActive(true);
                Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet.TryGetValue(
                            SharedModel.StageID, out var row);
                var starCount = States.Instance.CrystalRandomSkillState?.StarCount ?? 0;
                var maxStarCount = row?.MaxStar ?? 0;

                starForMulti.StarCountText.text = $"{starCount}/{maxStarCount}";
                for (int i = 0; i < starForMulti.WaveStarTexts.Length; i++)
                {
                    starForMulti.WaveStarTexts[i].text = SharedModel.ClearedCountForEachWaves[i].ToString();
                }
            }
            else
            {
                startRewards.SetActive(false);
            }

            _coUpdateBottomText = StartCoroutine(CoUpdateBottom(Timer));

            yield return null;
        }

        private IEnumerator EmitBattleWinVFX()
        {
            yield return _battleWinVFXYield;
            AudioController.instance.PlaySfx(AudioController.SfxCode.Win);
        }

        private void UpdateViewAsDefeat(BattleLog.Result result)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);

            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);

            cpUp.SetActive(true);
            rewardArea.SetActive(false);

            foreach (var item in enableVictorys)
            {
                item.SetActive(false);
            }

            foreach (var item in enableDefeats)
            {
                item.SetActive(true);
            }

            var stageText = StageInformation.GetStageIdString(
                SharedModel.StageType,
                SharedModel.StageID,
                true);

            worldStageId.text = $"{SharedModel.WorldName} {stageText}";

            bottomText.enabled = false;

            _coUpdateBottomText = StartCoroutine(CoUpdateBottom(Timer));
        }

        private IEnumerator CoUpdateBottom(int limitSeconds)
        {
            var secondsFormat = L10nManager.Localize("UI_AFTER_N_SECONDS");
            string fullFormat = string.Empty;
            closeButton.interactable = true;

            var canExit = (SharedModel.StageID >= Battle.RequiredStageForExitButton ||
                           SharedModel.LastClearedStageId >= Battle.RequiredStageForExitButton);
            if (!SharedModel.IsClear)
            {
                closeButton.gameObject.SetActive(true);
                stagePreparationButton.gameObject.SetActive(canExit);
                stagePreparationButton.interactable = canExit;
            }

            var isActionPointEnough = !SharedModel.ActionPointNotEnough ||
                                      SharedModel.StageType == StageType.EventDungeon;
            if (isActionPointEnough)
            {
                if (SharedModel.IsClear)
                {
                    stagePreparationButton.gameObject.SetActive(canExit && !SharedModel.IsEndStage);
                    stagePreparationButton.interactable = canExit && !SharedModel.IsEndStage;
                }
                else
                {
                    repeatButton.gameObject.SetActive(canExit);
                    repeatButton.interactable = canExit;
                }
            }
            else
            {
                stagePreparationButton.gameObject.SetActive(canExit && !SharedModel.IsClear);
                stagePreparationButton.interactable = canExit && !SharedModel.IsClear;
            }

            if (!SharedModel.IsEndStage && isActionPointEnough && SharedModel.IsClear)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }

            // for event EventDungeon
            if (SharedModel.StageType == StageType.EventDungeon && SharedModel.IsEndStage && SharedModel.IsClear)
            {
                repeatButton.gameObject.SetActive(true);
                repeatButton.interactable = true;
            }

            switch (SharedModel.NextState)
            {
                case NextState.GoToMain:
                    SubmitWidget = closeButton.onClick.Invoke;
                    fullFormat = L10nManager.Localize("UI_BATTLE_EXIT_FORMAT");
                    break;
                case NextState.RepeatStage:
                    SubmitWidget = repeatButton.onClick.Invoke;
                    fullFormat = L10nManager.Localize("UI_BATTLE_RESULT_REPEAT_STAGE_FORMAT");
                    break;
                case NextState.NextStage:
                    SubmitWidget = nextButton.onClick.Invoke;
                    fullFormat = L10nManager.Localize("UI_BATTLE_RESULT_NEXT_STAGE_FORMAT");
                    break;
                default:
                    bottomText.text = SharedModel.ActionPointNotEnough
                        ? L10nManager.Localize("UI_BATTLE_RESULT_NOT_ENOUGH_ACTION_POINT")
                        : string.Empty;
                    yield break;
            }

            // for tutorial
            if (SharedModel.StageID == SharedModel.LastClearedStageId &&
                SharedModel.State == BattleLog.Result.Win)
            {
                if (SharedModel.StageID is Battle.RequiredStageForExitButton ||
                    TutorialController.TutorialStageArray.Any(stageId => stageId == SharedModel.StageID))
                {
                    closeButton.gameObject.SetActive(true);
                    stagePreparationButton.gameObject.SetActive(false);
                    nextButton.gameObject.SetActive(false);
                    repeatButton.gameObject.SetActive(false);
                    bottomText.text = string.Empty;
                    yield break;
                }
            }

            bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));

            yield return new WaitUntil(() => CanClose);
            var dialog = Find<DialogPopup>();

            var floatTime = (float)limitSeconds;
            var floatTimeMinusOne = limitSeconds - 1f;
            while (limitSeconds > 0)
            {
                yield return null;

                floatTime -= Time.deltaTime;
                if (floatTimeMinusOne < floatTime)
                {
                    continue;
                }

                if (dialog.gameObject.activeSelf)
                {
                    yield break;
                }

                limitSeconds--;
                bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));
                floatTimeMinusOne = limitSeconds - 1f;
            }

            switch (SharedModel.NextState)
            {
                case NextState.GoToMain:
                    StartCoroutine(OnClickClose());
                    break;
                case NextState.RepeatStage:
                    StartCoroutine(OnClickRepeat());
                    break;
                case NextState.NextStage:
                    StartCoroutine(OnClickNext());
                    break;
            }
        }

        private IEnumerator CoProceedNextStage(bool buyTicketIfNeeded = false)
        {
            if (!nextButton.interactable)
            {
                yield break;
            }

            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            closeButton.interactable = false;
            stagePreparationButton.interactable = false;
            repeatButton.interactable = false;
            nextButton.interactable = false;
            actionPoint.SetEventTriggerEnabled(false);

            StopCoUpdateBottomText();
            StartCoroutine(CoFadeOut());
            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            var stageLoadingScreen = Find<StageLoadingEffect>();
            stageLoadingScreen.Show(
                SharedModel.StageType,
                stage.zone,
                SharedModel.WorldName,
                SharedModel.StageID + 1,
                true, SharedModel.StageID);
            Find<Status>().Close();
            var player = stage.RunPlayerForNextStage();
            player.DisableHUD();
            ActionRenderHandler.Instance.Pending = true;

            yield return StartCoroutine(SendBattleActionAsync(1, buyTicketIfNeeded));
        }

        private IEnumerator CoRepeatStage(bool buyTicketIfNeeded = false)
        {
            if (!repeatButton.interactable)
            {
                yield break;
            }

            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            closeButton.interactable = false;
            stagePreparationButton.interactable = false;
            repeatButton.interactable = false;
            nextButton.interactable = false;
            actionPoint.SetEventTriggerEnabled(false);

            StopCoUpdateBottomText();
            StartCoroutine(CoFadeOut());
            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            var stageLoadingScreen = Find<StageLoadingEffect>();
            stageLoadingScreen.Show(
                SharedModel.StageType,
                stage.zone,
                SharedModel.WorldName,
                SharedModel.StageID,
                false,
                SharedModel.StageID);
            Find<Status>().Close();

            var player = stage.RunPlayerForNextStage();
            player.DisableHUD();
            ActionRenderHandler.Instance.Pending = true;

            var props = new Dictionary<string, Value>()
            {
                ["StageId"] = SharedModel.StageID,
            };
            var eventKey = SharedModel.ClearedWaveNumber == 3
                ? "Repeat"
                : "Retry";
            var eventName = $"Unity/Stage Exit {eventKey}";
            Analyzer.Instance.Track(eventName, props);

            var category = $"Stage_Exit_{eventKey}";
            var evt = new AirbridgeEvent(category);
            evt.SetValue(SharedModel.StageID);
            AirbridgeUnity.TrackEvent(evt);

            yield return StartCoroutine(SendBattleActionAsync(0, buyTicketIfNeeded));
        }

        private IEnumerator SendBattleActionAsync(int stageIdOffset, bool buyTicketIfNeeded = false)
        {
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];
            var costumes = itemSlotState.Costumes;
            var equipments = itemSlotState.Equipments;
            var runeSlotInfos = States.Instance.CurrentRuneSlotStates[BattleType.Adventure].GetEquippedRuneSlotInfos();
            yield return SharedModel.StageType switch
            {
                StageType.HackAndSlash => Game.Game.instance.ActionManager
                    .HackAndSlash(
                        costumes,
                        equipments,
                        new List<Consumable>(),
                        runeSlotInfos,
                        SharedModel.WorldID,
                        SharedModel.StageID + stageIdOffset)
                    .StartAsCoroutine(),
                StageType.EventDungeon => Game.Game.instance.ActionManager
                    .EventDungeonBattle(
                        RxProps.EventScheduleRowForDungeon.Value.Id,
                        SharedModel.WorldID,
                        SharedModel.StageID + stageIdOffset,
                        equipments,
                        costumes,
                        new List<Consumable>(),
                        runeSlotInfos,
                        buyTicketIfNeeded)
                    .StartAsCoroutine(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void NextPrepareStage(BattleLog log)
        {
            if (!IsActive() || !Find<StageLoadingEffect>().IsActive())
                return;

            StartCoroutine(CoGoToNextStageClose(log));
        }

        private IEnumerator CoGoToNextStageClose(BattleLog log)
        {
            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            yield return StartCoroutine(CoFadeOut());

            var stageLoadingEffect = Find<StageLoadingEffect>();
            if (!stageLoadingEffect.LoadingEnd)
            {
                yield return new WaitUntil(() => stageLoadingEffect.LoadingEnd);
            }

            yield return StartCoroutine(stageLoadingEffect.CoClose());

            // TODO: WhenAll
            yield return BattleRenderer.Instance.LoadStageResources(log);
            Close();
        }

        public void NextMimisbrunnrStage(BattleLog log)
        {
            StartCoroutine(CoGoToNextMimisbrunnrStageClose(log));
        }

        private IEnumerator CoGoToNextMimisbrunnrStageClose(BattleLog log)
        {
            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            yield return StartCoroutine(Find<StageLoadingEffect>().CoClose());
            yield return StartCoroutine(CoFadeOut());
            Close();
        }

        private void GoToMain(bool worldClear)
        {
            var props = new Dictionary<string, Value>()
            {
                ["StageId"] = Game.Game.instance.Stage.stageId,
            };
            var eventKey = Game.Game.instance.Stage.IsExitReserved ? "Quit" : "Main";
            var eventName = $"Unity/Stage Exit {eventKey}";
            Analyzer.Instance.Track(eventName, props);

            var category = $"Stage_Exit_{eventKey}";
            var evt = new AirbridgeEvent(category);
            evt.SetValue(Game.Game.instance.Stage.stageId);
            AirbridgeUnity.TrackEvent(evt);

            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            if (worldClear)
            {
                var worldMapLoading = Find<LoadingScreen>();
                worldMapLoading.Show(LoadingScreen.LoadingType.Adventure);
                Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
                {
                    Find<HeaderMenuStatic>().Show();
                    Find<Menu>().Close();
                    Find<WorldMap>().Show(States.Instance.CurrentAvatarState.worldInformation);
                    worldMapLoading.Close(true);
                });
            }
        }

        private void GoToPreparation()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            var worldMapLoading = Find<LoadingScreen>();
            worldMapLoading.Show();
            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);

                var stageNumber = 0;
                switch (SharedModel.StageType)
                {
                    case StageType.HackAndSlash:
                        Find<WorldMap>().Show(SharedModel.WorldID, SharedModel.StageID, true);
                        stageNumber = SharedModel.StageID;
                        break;

                    case StageType.Mimisbrunnr:
                        var viewModel = new WorldMap.ViewModel
                        {
                            WorldInformation = States.Instance.CurrentAvatarState.worldInformation,
                        };
                        viewModel.SelectedStageId.SetValueAndForceNotify(SharedModel.WorldID);
                        viewModel.SelectedStageId.SetValueAndForceNotify(SharedModel.StageID);
                        Game.Game.instance.TableSheets.WorldSheet.TryGetValue(SharedModel.WorldID,
                            out var worldRow);

                        Find<StageInformation>().Show(viewModel, worldRow, StageType.Mimisbrunnr);
                        stageNumber = SharedModel.StageID % 10000000;
                        break;

                    case StageType.EventDungeon:
                        if (RxProps.EventDungeonRow is null)
                        {
                            NotificationSystem.Push(
                                MailType.System,
                                L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                                NotificationCell.NotificationType.Information);
                            break;
                        }

                        var worldMap = Find<WorldMap>();
                        worldMap.Show(States.Instance.CurrentAvatarState.worldInformation, true);
                        worldMap.ShowEventDungeonStage(RxProps.EventDungeonRow, false);
                        stageNumber = SharedModel.StageID.ToEventDungeonStageNumber();
                        break;
                }

                Find<BattlePreparation>().Show(
                    SharedModel.StageType,
                    SharedModel.WorldID,
                    SharedModel.StageID,
                    $"{SharedModel.WorldName.ToUpper()} {stageNumber}",
                    true);

                worldMapLoading.Close(true);
            });
        }

        private void GoToMarket()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                Find<ShopSell>().Show();
            });
        }

        private void GoToProduct()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                Find<ShopBuy>().Show();
            });
        }

        private void GoToCraft()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Widget.Find<Menu>().GoToCraftEquipment();
            });
        }

        private void GoToFood()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Widget.Find<Menu>().GoToFood();
            });
        }

        private void StopCoUpdateBottomText()
        {
            if (_coUpdateBottomText != null)
            {
                StopCoroutine(_coUpdateBottomText);
            }
        }

        private IEnumerator CoFadeOut()
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime;

                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void RefreshSeasonPassCourageAmount()
        {
            if (Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }
                int playCount = 0;
                try
                {
                    playCount = SharedModel.ClearedCountForEachWaves.Sum();
                }
                catch
                {
                    NcDebug.LogError("SharedModel.ClearedCountForEachWaves Sum Failed");
                }

                if (SharedModel.StageType == StageType.EventDungeon)
                {
                    seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.EventDungeonCourageAmount * playCount}";
                }
                else
                {
                    seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.AdventureCourageAmount * playCount}";
                }
            }
            else
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(false);
                }
            }
        }
    }
}
