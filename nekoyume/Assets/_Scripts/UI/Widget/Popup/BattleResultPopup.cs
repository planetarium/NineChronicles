using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
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
    using UniRx;

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
            public int ActionPoint;
            public int LastClearedStageId;
            public bool ActionPointNotEnough;
            public bool IsClear;
            public bool IsEndStage;

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
        public struct RewardsArea
        {
            public GameObject root;
            public BattleReward[] rewards;
            public BattleReward rewardForMulti;
        }

        [Serializable]
        public struct DefeatTextArea
        {
            public GameObject root;
            public TextMeshProUGUI defeatText;
            public TextMeshProUGUI expText;
        }

        private const int Timer = 10;
        private static readonly Vector3 VfxBattleWinOffset = new(-0.05f, 1.2f, 10f);

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameObject victoryImageContainer;

        [SerializeField]
        private GameObject defeatImageContainer;

        [SerializeField]
        private TextMeshProUGUI worldStageId;

        [SerializeField]
        private GameObject topArea;

        [SerializeField]
        private DefeatTextArea defeatTextArea;

        [SerializeField]
        private RewardsArea rewardsArea;

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
        private StageProgressBar stageProgressBar;

        [SerializeField]
        private GameObject[] victoryResultTexts;

        [SerializeField]
        private ActionPoint actionPoint;

        private BattleWin01VFX _battleWin01VFX;

        private BattleWin02VFX _battleWin02VFX;

        private BattleWin03VFX _battleWin03VFX;

        private BattleWin04VFX _battleWin04VFX;

        private Coroutine _coUpdateBottomText;

        private readonly WaitForSeconds _battleWinVFXYield = new(0.2f);
        private static readonly int ClearedWave = Animator.StringToHash("ClearedWave");

        private Animator _victoryImageAnimator;

        private bool _IsAlreadyOut;

        private Model SharedModel { get; set; }

        public Model ModelForMultiHackAndSlash { get; set; }

        public StageProgressBar StageProgressBar => stageProgressBar;

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                var wi = States.Instance.CurrentAvatarState.worldInformation;
                if (!wi.TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                {
                    return;
                }

                // NOTE: This `BattleResultPopup` cannot be closed when
                //       the player is not cleared `Battle.RequiredStageForExitButton` stage yet.
                var canExit = world.StageClearedId >= Battle.RequiredStageForExitButton;
                if (canExit)
                {
                    StartCoroutine(OnClickClose());
                }
            }).AddTo(gameObject);

            stagePreparationButton.OnClickAsObservable().Subscribe(_ => OnClickStage()).AddTo(gameObject);

            nextButton.OnClickAsObservable()
                .Subscribe(_ => StartCoroutine(OnClickNext()))
                .AddTo(gameObject);

            repeatButton.OnClickAsObservable()
                .Subscribe(_ => StartCoroutine(OnClickRepeat()))
                .AddTo(gameObject);

            CloseWidget = closeButton.onClick.Invoke;
            SubmitWidget = nextButton.onClick.Invoke;
            defeatTextArea.root.SetActive(false);

            _victoryImageAnimator = victoryImageContainer.GetComponent<Animator>();
        }

        private IEnumerator OnClickClose()
        {
            _IsAlreadyOut = true;
            AudioController.PlayClick();
            if (SharedModel.IsClear)
            {
                yield return CoDialog(SharedModel.StageID);
            }

            GoToMain();
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
            if (SharedModel.StageType == StageType.EventDungeon)
            {
                yield break;
            }

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

        public void Show(Model model, bool isBoosted)
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
            actionPoint.SetActionPoint(model.ActionPoint);
            actionPoint.SetEventTriggerEnabled(true);

            foreach (var reward in rewardsArea.rewards)
            {
                reward.gameObject.SetActive(false);
            }
            rewardsArea.rewardForMulti.gameObject.SetActive(false);

            base.Show();
            closeButton.gameObject.SetActive(
                model.StageID >= Battle.RequiredStageForExitButton ||
                model.LastClearedStageId >= Battle.RequiredStageForExitButton);
            stagePreparationButton.gameObject.SetActive(false);
            repeatButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);

            UpdateView(isBoosted);
            HelpTooltip.HelpMe(100006, true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopVFX();

            foreach (var obj in victoryResultTexts)
            {
                obj.SetActive(false);
            }

            stageProgressBar.Close();
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateView(bool isBoosted)
        {
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
            topArea.SetActive(true);
            defeatTextArea.root.SetActive(false);
            stageProgressBar.Show();
            stageProgressBar.SetStarProgress(SharedModel.ClearedWaveNumber);

            _coUpdateBottomText = StartCoroutine(CoUpdateBottom(Timer));
            yield return StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator EmitBattleWinVFX()
        {
            yield return _battleWinVFXYield;
            AudioController.instance.PlaySfx(AudioController.SfxCode.Win);

            switch (SharedModel.ClearedWaveNumber)
            {
                case 1:
                    _battleWin01VFX =
                        VFXController.instance.CreateAndChase<BattleWin01VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
                case 2:
                    _battleWin02VFX =
                        VFXController.instance.CreateAndChase<BattleWin02VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
                case 3:
                    _battleWin03VFX =
                        VFXController.instance.CreateAndChase<BattleWin03VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
            }
        }

        private void UpdateViewAsDefeat(BattleLog.Result result)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);

            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);
            topArea.SetActive(false);
            defeatTextArea.root.SetActive(true);
            var key = "UI_BATTLE_RESULT_DEFEAT_MESSAGE";
            if (result == BattleLog.Result.TimeOver)
            {
                key = "UI_BATTLE_RESULT_TIMEOUT_MESSAGE";
            }

            defeatTextArea.defeatText.text = L10nManager.Localize(key);
            defeatTextArea.expText.text = $"EXP + {SharedModel.Exp}";
            bottomText.enabled = false;

            _coUpdateBottomText = StartCoroutine(CoUpdateBottom(Timer));
            StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator CoUpdateRewards()
        {
            rewardsArea.root.SetActive(true);
            var isNotClearedInMulti = SharedModel.ClearedCountForEachWaves[3] <= 0 &&
                                      SharedModel.ClearedCountForEachWaves.Sum() > 1;
            for (var i = 0; i < rewardsArea.rewards.Length; i++)
            {
                var view =
                    i == 2 &&
                    isNotClearedInMulti
                        ? rewardsArea.rewardForMulti
                        : rewardsArea.rewards[i];

                view.StartShowAnimation();

                var sum = 0;
                for (var j = i; j < 3; j++)
                {
                    sum += SharedModel.ClearedCountForEachWaves[j + 1];
                }

                var cleared = sum > 0;
                switch (i)
                {
                    case 0:
                        view.Set(SharedModel.Exp, cleared);
                        break;
                    case 1:
                        view.Set(SharedModel.Rewards, Game.Game.instance.Stage.stageId, cleared);
                        break;
                    case 2:
                        if (isNotClearedInMulti)
                        {
                            Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet.TryGetValue(
                                SharedModel.StageID, out var row);
                            var starCount = States.Instance.CrystalRandomSkillState?.StarCount ?? 0;
                            var maxStarCount = row?.MaxStar ?? 0;

                            view.Set(SharedModel.ClearedCountForEachWaves, starCount, maxStarCount);
                        }
                        else
                        {
                            view.Set(cleared);
                        }

                        break;
                }

                yield return new WaitForSeconds(0.5f);

                view.gameObject.SetActive(true);
                if (i < 2 || !isNotClearedInMulti)
                {
                    view.EnableStar(cleared);
                }

                yield return null;
                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            yield return new WaitForSeconds(0.5f);

            foreach (var reward in rewardsArea.rewards)
            {
                reward.StopShowAnimation();
                reward.StartScaleTween();
            }

            rewardsArea.rewardForMulti.StopShowAnimation();
            rewardsArea.rewardForMulti.StartScaleTween();
        }

        private IEnumerator CoUpdateBottom(int limitSeconds)
        {
            var secondsFormat = L10nManager.Localize("UI_AFTER_N_SECONDS");
            string fullFormat = string.Empty;
            closeButton.interactable = true;

            if (!SharedModel.IsClear)
            {
                stagePreparationButton.gameObject.SetActive(true);
                stagePreparationButton.interactable = true;
            }

            var isActionPointEnough = !SharedModel.ActionPointNotEnough ||
                                      SharedModel.StageType == StageType.EventDungeon;
            if (isActionPointEnough)
            {
                var value =
                    SharedModel.StageID >= Battle.RequiredStageForExitButton ||
                    SharedModel.LastClearedStageId >= Battle.RequiredStageForExitButton;
                repeatButton.gameObject.SetActive(value);
                repeatButton.interactable = value;
            }

            if (!SharedModel.IsEndStage && isActionPointEnough && SharedModel.IsClear)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }

            switch (SharedModel.NextState)
            {
                case NextState.GoToMain:
                    SubmitWidget = closeButton.onClick.Invoke;
                    fullFormat = SharedModel.ActionPointNotEnough
                        ? L10nManager.Localize("UI_BATTLE_RESULT_NOT_ENOUGH_ACTION_POINT_FORMAT")
                        : L10nManager.Localize("UI_BATTLE_EXIT_FORMAT");
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
                    bottomText.text = string.Empty;
                    yield break;
            }

            // for tutorial
            if (SharedModel.StageID == Battle.RequiredStageForExitButton &&
                SharedModel.LastClearedStageId == Battle.RequiredStageForExitButton &&
                SharedModel.State == BattleLog.Result.Win)
            {
                stagePreparationButton.gameObject.SetActive(false);
                nextButton.gameObject.SetActive(false);
                repeatButton.gameObject.SetActive(false);
                bottomText.text = string.Empty;
                yield break;
            }

            bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));

            yield return new WaitUntil(() => CanClose);

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

                limitSeconds--;
                bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));
                floatTimeMinusOne = limitSeconds - 1f;
            }

            StopVFX();
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

            StopVFX();
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

            StopVFX();
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
                StageType.Mimisbrunnr => Game.Game.instance.ActionManager
                    .MimisbrunnrBattle(
                        costumes,
                        equipments,
                        new List<Consumable>(),
                        runeSlotInfos,
                        SharedModel.WorldID,
                        SharedModel.StageID + stageIdOffset,
                        1)
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

        public void NextStage(BattleLog log)
        {
            StartCoroutine(CoGoToNextStageClose(log));
        }

        private IEnumerator CoGoToNextStageClose(BattleLog log)
        {
            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            yield return StartCoroutine(Find<StageLoadingEffect>().CoClose());
            yield return StartCoroutine(CoFadeOut());
            Game.Event.OnStageStart.Invoke(log);
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
            Game.Event.OnStageStart.Invoke(log);
            Close();
        }

        private void GoToMain()
        {
            var props = new Dictionary<string, Value>()
            {
                ["StageId"] = Game.Game.instance.Stage.stageId,
            };
            var eventKey = Game.Game.instance.Stage.IsExitReserved ? "Quit" : "Main";
            var eventName = $"Unity/Stage Exit {eventKey}";
            Analyzer.Instance.Track(eventName, props);

            Find<Battle>().Close(true);
            Game.Game.instance.Stage.DestroyBackground();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var lastClearedStageId))
            {
                if (SharedModel.IsClear
                    && SharedModel.IsEndStage
                    && lastClearedStageId == SharedModel.StageID
                    && !Find<WorldMap>().SharedViewModel.UnlockedWorldIds.Contains(SharedModel.WorldID + 1))
                {
                    var worldMapLoading = Find<WorldMapLoadingScreen>();
                    worldMapLoading.Show();
                    Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
                    {
                        Find<HeaderMenuStatic>().Show();
                        Find<Menu>().Close();
                        Find<WorldMap>().Show(States.Instance.CurrentAvatarState.worldInformation);
                        worldMapLoading.Close(true);
                    });
                }
            }
        }

        private void GoToPreparation()
        {
            Find<Battle>().Close(true);
            Game.Game.instance.Stage.DestroyBackground();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            var worldMapLoading = Find<WorldMapLoadingScreen>();
            worldMapLoading.Show();
            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);

                var stageNumber = 0;
                switch (SharedModel.StageType)
                {
                    case StageType.HackAndSlash:
                        Find<WorldMap>().Show(SharedModel.WorldID, SharedModel.StageID, false);
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
            Game.Game.instance.Stage.DestroyBackground();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                Find<ShopSell>().Show();
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

        private void StopVFX()
        {
            if (_battleWin01VFX)
            {
                _battleWin01VFX.Stop();
                _battleWin01VFX = null;
            }

            if (_battleWin02VFX)
            {
                _battleWin02VFX.Stop();
                _battleWin02VFX = null;
            }

            if (_battleWin03VFX)
            {
                _battleWin03VFX.Stop();
                _battleWin03VFX = null;
            }

            if (_battleWin04VFX)
            {
                _battleWin04VFX.Stop();
                _battleWin04VFX = null;
            }

            foreach (var reward in rewardsArea.rewards)
            {
                reward.StopVFX();
            }
        }
    }
}
