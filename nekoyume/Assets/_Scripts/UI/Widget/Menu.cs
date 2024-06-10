using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DG.Tweening;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.Model.BattleStatus;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Game.Battle;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Lobby;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Scroller;
    using UniRx;

    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";

        private const string FirstOpenMimisbrunnrKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenMimisbrunnrKeyKey_{0}";

        [SerializeField] private MainMenu btnQuest;

        [SerializeField] private MainMenu btnCombination;

        [SerializeField] private MainMenu btnShop;

        [SerializeField] private MainMenu btnRanking;

        [SerializeField] private MainMenu btnStaking;

        [SerializeField] private MainMenu btnWorldBoss;

        [SerializeField] private MainMenu btnDcc;

        [SerializeField]
        private MainMenu btnPatrolReward;

        [SerializeField]
        private MainMenu btnSeasonPass;

        [SerializeField]
        private MainMenu btnCollection;

        [SerializeField]
        private SpeechBubble[] speechBubbles;

        [SerializeField] private GameObject shopExclamationMark;

        [SerializeField] private GameObject combinationExclamationMark;

        [SerializeField] private GameObject questExclamationMark;

        [SerializeField] private GameObject mimisbrunnrExclamationMark;

        [SerializeField] private GameObject eventDungeonExclamationMark;

        [SerializeField] private TextMeshProUGUI eventDungeonTicketsText;

        [SerializeField] private Image stakingLevelIcon;

        [SerializeField] private GuidedQuest guidedQuest;

        [SerializeField] private Button playerButton;

        [SerializeField] private Button petButton;

        [SerializeField] private StakeIconDataScriptableObject stakeIconData;

        [SerializeField] private RectTransform player;

        [SerializeField] private RectTransform playerPosition;

        [SerializeField] private Transform titleSocket;

        private Coroutine _coLazyClose;

        private readonly List<IDisposable> _disposablesAtShow = new();
        private GameObject _cachedCharacterTitle;

        public PatrolRewardMenu PatrolRewardMenu => (PatrolRewardMenu)btnPatrolReward;

        public bool IsShown => AnimationState.Value == AnimationStateType.Shown;

        protected override void Awake()
        {
            base.Awake();

            speechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            playerButton.onClick.AddListener(() => Game.Game.instance.Lobby.Character.Touch());
            petButton.onClick.AddListener(() => Game.Game.instance.Lobby.Character.TouchPet());
            guidedQuest.OnClickWorldQuestCell
                .Subscribe(tuple => HackAndSlash(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.OnClickCombinationEquipmentQuestCell
                .Subscribe(tuple => GoToCombinationEquipmentRecipe(tuple.quest.RecipeId))
                .AddTo(gameObject);
            guidedQuest.OnClickEventDungeonQuestCell
                .Subscribe(tuple => EventDungeonBattle(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.CnClickCraftEventItemQuestCell
                .Subscribe(tuple => GoToCraftWithToggleType(2))
                .AddTo(gameObject);
            AnimationState.Subscribe(stateType =>
            {
                var buttonList = new List<Button>
                {
                    btnCombination.GetComponent<Button>(),
                    btnQuest.GetComponent<Button>(),
                    btnRanking.GetComponent<Button>(),
                    btnShop.GetComponent<Button>(),
                    btnStaking.GetComponent<Button>(),
                    btnWorldBoss.GetComponent<Button>(),
                    btnDcc.GetComponent<Button>(),
                    btnPatrolReward.GetComponent<Button>(),
                    btnSeasonPass.GetComponent<Button>(),
                    btnCollection.GetComponent<Button>(),
                };
                buttonList.ForEach(button =>
                    button.interactable = stateType == AnimationStateType.Shown);
            }).AddTo(gameObject);

            StakingSubject.Level
                .Subscribe(level =>
                    stakingLevelIcon.sprite = stakeIconData.GetIcon(level, IconType.Bubble))
                .AddTo(gameObject);
            BattleRenderer.Instance.OnPrepareStage += GoToPrepareStage;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BattleRenderer.Instance.OnPrepareStage -= GoToPrepareStage;
        }

        private static void HackAndSlashForTutorial(int stageId)
        {
            var sheets = Game.Game.instance.TableSheets;
            var stageRow = sheets.StageSheet.OrderedList.FirstOrDefault(row => row.Id == stageId);
            if (stageRow is null)
            {
                return;
            }

            var requiredCost = stageRow.CostAP;
            if (ReactiveAvatarState.ActionPoint < requiredCost)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("ERROR_ACTION_POINT"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (!sheets.WorldSheet.TryGetByStageId(stageId, out var worldRow))
            {
                return;
            }

            var worldId = worldRow.Id;

            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Adventure);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);

            var stage = Game.Game.instance.Stage;
            stage.IsExitReserved = false;
            var player = stage.GetPlayer();
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
            ActionRenderHandler.Instance.Pending = true;
            var emptyGuids = new List<Guid>();
            Game.Game.instance.ActionManager
                .HackAndSlash(emptyGuids, emptyGuids, new List<Consumable>(),
                    new List<RuneSlotInfo>(), worldId, stageId)
                .Subscribe();
        }

        private void HackAndSlash(int stageId)
        {
            if (TableSheets.Instance.WorldSheet.TryGetByStageId(stageId, out var worldRow) &&
                ShortcutHelper.CheckConditionOfShortcut(ShortcutHelper.PlaceType.Stage,
                    stageId))
            {
                CloseWithOtherWidgets();
                ShortcutHelper.ShortcutActionForStage(worldRow.Id, stageId, true);
            }
            else if (ShortcutHelper.CheckUIStateForUsingShortcut(ShortcutHelper.PlaceType.Stage))
            {
                Find<Menu>().QuestClick();
            }
        }

        private void EventDungeonBattle(int eventDungeonStageId)
        {
            if (RxProps.EventScheduleRowForDungeon.Value is null)
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            if (ShortcutHelper.CheckConditionOfShortcut(ShortcutHelper.PlaceType.EventDungeonStage,
                    eventDungeonStageId))
            {
                CloseWithOtherWidgets();
                ShortcutHelper.ShortcutActionForEventStage(eventDungeonStageId, true);
            }
            else if (ShortcutHelper.CheckUIStateForUsingShortcut(ShortcutHelper.PlaceType
                         .EventDungeonStage))
            {
                Find<Menu>().QuestClick();
            }
        }

        public void UpdateTitle(Costume title)
        {
            Destroy(_cachedCharacterTitle);
            if (title == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(title.Grade,
                title.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, titleSocket);
        }

        public void EnterRoom()
        {
            player.localPosition = playerPosition.localPosition + (Vector3.left * 300);
            player.DOLocalMoveX(playerPosition.localPosition.x, 1.0f);
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

        public void GoToCraftEquipment()
        {
            GoToCraftWithToggleType(0);
        }

        public void GoToFood()
        {
            GoToCraftWithToggleType(1);
        }

        private void GoToCraftWithToggleType(int toggleIndex)
        {
            AudioController.PlayClick();

            Analyzer.Instance.Track("Unity/Click Guided Quest Combination Equipment");

            var evt = new AirbridgeEvent("Click_Guided_Quest_Combination_Equipment");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            CombinationClickInternal(() =>
                Find<Craft>().ShowWithToggleIndex(toggleIndex));
        }

        private void GoToCombinationEquipmentRecipe(int recipeId)
        {
            AudioController.PlayClick();

            Analyzer.Instance.Track("Unity/Click Guided Quest Combination Equipment",
                new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                });

            var evt = new AirbridgeEvent("Click_Guided_Quest_Combination_Equipment");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            CombinationClickInternal(() =>
                Find<Craft>().ShowWithEquipmentRecipeId(recipeId));
        }

        private void GoToMarket(TradeType tradeType)
        {
            Close();
            Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
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

        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();
            btnStaking.Update();
            btnWorldBoss.Update();
            btnDcc.Update();

            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var firstOpenCombinationKey
                = string.Format(FirstOpenCombinationKeyFormat, addressHex);
            var firstOpenQuestKey
                = string.Format(FirstOpenQuestKeyFormat, addressHex);
            var firstOpenMimisbrunnrKey
                = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);

            combinationExclamationMark.SetActive(
                (btnCombination.IsUnlocked &&
                 (PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0 ||
                  Craft.SharedModel.HasNotification)) || Summon.HasNotification);
            shopExclamationMark.SetActive(
                btnShop.IsUnlocked
                && ShopNoti(addressHex));

            var worldMap = Find<WorldMap>();
            worldMap.UpdateNotificationInfo();
            var hasNotificationInWorldMap = worldMap.HasNotification;
            questExclamationMark.SetActive(
                (btnQuest.IsUnlocked
                 && PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0)
                || hasNotificationInWorldMap);
        }

        private bool ShopNoti(string addressHex)
        {
            var firstOpenShopKey
                = string.Format(FirstOpenShopKeyFormat, addressHex);
#if UNITY_ANDROID || UNITY_IOS
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            return PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0 || iapStoreManager.ExistAvailableFreeProduct();
#else
            return PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0;
#endif
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
            btnStaking.gameObject.SetActive(false);
        }

        public void ShowWorld()
        {
            Show();
            HideButtons();
        }

        public void QuestClick()
        {
            if (!btnQuest.IsUnlocked)
            {
                return;
            }

            if (questExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenQuestKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            var avatarState = States.Instance.CurrentAvatarState;
            Find<WorldMap>().Show(avatarState.worldInformation);
            AudioController.PlayClick();
        }

        public void ShopClick()
        {
            if (!btnShop.IsUnlocked)
            {
                return;
            }

            Analyzer.Instance.Track("Unity/Lobby/ShopButton/Click");

            var evt = new AirbridgeEvent("Lobby_ShopButton_Click");
            AirbridgeUnity.TrackEvent(evt);

            if (shopExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenShopKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<ShopBuy>().Show();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            AudioController.PlayClick();
        }

        public void CombinationClick() =>
            CombinationClickInternal(() => Find<CombinationMain>().Show());

        private void CombinationClickInternal(System.Action showAction)
        {
            if (showAction is null)
            {
                return;
            }

            if (!btnCombination.IsUnlocked)
            {
                return;
            }

            if (combinationExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Workshop, null, true);
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            showAction();
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                return;
            }

            Close(true);
            Find<ArenaJoin>().ShowAsync().Forget();

            Analyzer.Instance.Track("Unity/Enter arena page", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });

            var evt = new AirbridgeEvent("Enter_Arena_Page");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            AudioController.PlayClick();
        }

        //public void MimisbrunnrClick()
        //{
        //    if (!btnMimisbrunnr.IsUnlocked)
        //    {
        //        return;
        //    }

        //    const int worldId = GameConfig.MimisbrunnrWorldId;
        //    var worldSheet = Game.Game.instance.TableSheets.WorldSheet;
        //    var worldRow =
        //        worldSheet.OrderedList.FirstOrDefault(
        //            row => row.Id == worldId);
        //    if (worldRow is null)
        //    {
        //        NotificationSystem.Push(MailType.System,
        //            L10nManager.Localize("ERROR_WORLD_DOES_NOT_EXIST"),
        //            NotificationCell.NotificationType.Information);
        //        return;
        //    }

        //    var wi = States.Instance.CurrentAvatarState.worldInformation;
        //    if (!wi.TryGetWorld(worldId, out var world))
        //    {
        //        LocalLayerModifier.AddWorld(
        //            States.Instance.CurrentAvatarState.address,
        //            worldId);

        //        if (!wi.TryGetWorld(worldId, out world))
        //        {
        //            // Do nothing.
        //            return;
        //        }
        //    }

        //    if (!world.IsUnlocked)
        //    {
        //        // Do nothing.
        //        return;
        //    }

        //    var SharedViewModel = new WorldMap.ViewModel
        //    {
        //        WorldInformation = wi,
        //    };

        //    if (mimisbrunnrExclamationMark.gameObject.activeSelf)
        //    {
        //        var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
        //        var key = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);
        //        PlayerPrefs.SetInt(key, 1);
        //    }

        //    Close();
        //    AudioController.PlayClick();

        //    SharedViewModel.SelectedWorldId.SetValueAndForceNotify(world.Id);
        //    SharedViewModel.SelectedStageId.SetValueAndForceNotify(world.GetNextStageId());
        //    var stageInfo = Find<StageInformation>();
        //    stageInfo.Show(SharedViewModel, worldRow, StageType.Mimisbrunnr);
        //    var status = Find<Status>();
        //    status.Close(true);
        //    Find<EventBanner>().Close(true);
        //    Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
        //    HelpTooltip.HelpMe(100019, true);
        //}

        public void StakingClick()
        {
            if (!btnStaking.IsUnlocked)
            {
                return;
            }

            Find<StakingPopup>().Show();
        }

        public void WorldBossClick()
        {
            if (!btnWorldBoss.IsUnlocked)
            {
                return;
            }

            AudioController.PlayClick();

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            if (curStatus == WorldBossStatus.OffSeason)
            {
                if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out _))
                {
                    OneLineSystem.Push(
                        MailType.System,
                        "There is no world boss schedule.",
                        NotificationCell.NotificationType.Alert);
                    return;
                }
            }

            Close(true);
            Find<WorldBoss>().ShowAsync().Forget();
            Analyzer.Instance.Track("Unity/Enter world boss page");

            var evt = new AirbridgeEvent("Enter_World_Boss_Page");
            AirbridgeUnity.TrackEvent(evt);
        }

        public void DccClick()
        {
            if (!btnDcc.IsUnlocked)
            {
                return;
            }

            AudioController.PlayClick();

            Close(true);
            Find<DccMain>().Show();
        }

        public void PatrolRewardClick()
        {
            if(!btnPatrolReward.IsUnlocked)
            {
                return;
            }

            Find<PatrolRewardPopup>().Show();
        }

        public void SeasonPassClick()
        {
            if (!btnSeasonPass.IsUnlocked || Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData == null)
            {
                return;
            }
            if(Game.Game.instance.SeasonPassServiceManager.AvatarInfo.Value == null)
            {
                OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_CONNECT_FAIL"), NotificationCell.NotificationType.Notification);
                return;
            }
            Find<SeasonPass>().Show();
        }

        public void CollectionClick()
        {
            if (!btnCollection.IsUnlocked)
            {
                return;
            }

            AudioController.PlayClick();

            Close(true);
            Find<Collection>().Show();
        }

        public void UpdateGuideQuest(AvatarState avatarState)
        {
            guidedQuest.UpdateList(avatarState);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Analyzer.Instance.Track("Unity/Lobby/Show");

            var evt = new AirbridgeEvent("Lobby_Show");
            AirbridgeUnity.TrackEvent(evt);

            SubscribeAtShow();
            Time.timeScale = Game.Game.DefaultTimeScale;

            if (!(_coLazyClose is null))
            {
                StopCoroutine(_coLazyClose);
                _coLazyClose = null;
            }

            guidedQuest.Hide(true);
            base.Show(ignoreShowAnimation);

            StartCoroutine(CoStartSpeeches());
            UpdateButtons();
            stakingLevelIcon.sprite =
                stakeIconData.GetIcon(States.Instance.StakingLevel, IconType.Bubble);
        }

        private void SubscribeAtShow()
        {
            _disposablesAtShow.DisposeAllAndClear();
            RxProps.EventScheduleRowForDungeon.Subscribe(value =>
            {
                eventDungeonTicketsText.text =
                    RxProps.EventDungeonTicketProgress.Value
                        .currentTickets.ToString(CultureInfo.InvariantCulture);
                eventDungeonExclamationMark.gameObject
                    .SetActive(value is not null);
            }).AddTo(_disposablesAtShow);
            RxProps.EventDungeonTicketProgress.Subscribe(value =>
            {
                eventDungeonTicketsText.text =
                    value.currentTickets.ToString(CultureInfo.InvariantCulture);
            }).AddTo(_disposablesAtShow);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            Find<DialogPopup>().Show(1, PlayTutorial);
            StartCoroutine(CoHelpPopup());
        }

        private void PlayTutorial()
        {
            var worldInfo = Game.Game.instance.States.CurrentAvatarState.worldInformation;
            if (worldInfo is null)
            {
                NcDebug.LogError("[Menu.PlayTutorial] : worldInformation is null");
                return;
            }

            var clearedStageId = worldInfo.TryGetLastClearedStageId(out var id) ? id : 0;
            Game.Game.instance.Stage.TutorialController.Run(clearedStageId);
        }

        private IEnumerator CoHelpPopup()
        {
            var dialog = Find<DialogPopup>();
            while (dialog.IsActive())
            {
                yield return null;
            }

            guidedQuest.Show(States.Instance.CurrentAvatarState);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Destroy(_cachedCharacterTitle);
            Find<EventReleaseNotePopup>().Close(true);
            StopSpeeches();
            guidedQuest.Hide(true);
            Find<Status>().Close(true);
            Find<EventBanner>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoStartSpeeches()
        {
            yield return new WaitForSeconds(2.0f);

            while (AnimationState.Value == AnimationStateType.Shown)
            {
                var n = speechBubbles.Length;
                while (n > 1)
                {
                    n--;
                    var k = Mathf.FloorToInt(Random.value * (n + 1));
                    (speechBubbles[k], speechBubbles[n]) = (speechBubbles[n], speechBubbles[k]);
                }

                foreach (var bubble in speechBubbles)
                {
                    yield return StartCoroutine(bubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }

        private void StopSpeeches()
        {
            StopCoroutine(CoStartSpeeches());
            foreach (var bubble in speechBubbles)
            {
                bubble.Hide();
            }
        }

        public void TutorialActionHackAndSlash()
        {
            HackAndSlashForTutorial(GuidedQuest.WorldQuest?.Goal ?? 1);
        }

        // Invoke from TutorialController.PlayAction()
        public void TutorialActionGoToFirstRecipeCellView()
        {
            var firstRecipeRow = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.OrderedList
                .FirstOrDefault();
            if (firstRecipeRow is null)
            {
                NcDebug.LogError("TutorialActionGoToFirstRecipeCellView() firstRecipeRow is null");
                return;
            }

            Craft.SharedModel.DummyLockedRecipes.Add(firstRecipeRow.Id);

            if (combinationExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            Find<Craft>().ShowWithEquipmentRecipeId(firstRecipeRow.Id);
        }

        // Invoke from TutorialController.PlayAction()
        public void TutorialActionClickGuidedQuestWorldStage2()
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            player.DisableHudContainer();
            HackAndSlash(
                States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var targetStage)
                    ? targetStage + 1
                    : 1);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionGoToWorkShop()
        {
            CombinationClick();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickPatrolRewardMenu()
        {
            PatrolRewardClick();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickArenaMenu()
        {
            RankingClick();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickWorldBossButton()
        {
            WorldBossClick();
        }

#if UNITY_EDITOR
        protected override void Update()
        {
            base.Update();

            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.C) &&
                    !Find<CombinationResultPopup>().gameObject.activeSelf)
                {
                    Find<CombinationResultPopup>().ShowWithEditorProperty();
                }
                else if (Input.GetKeyDown(KeyCode.E) &&
                         !Find<EnhancementResultPopup>().gameObject.activeSelf)
                {
                    Find<EnhancementResultPopup>().ShowWithEditorProperty();
                }
                else if (Input.GetKeyDown(KeyCode.U) &&
                         !Find<OneButtonSystem>().gameObject.activeSelf)
                {
                    var game = Game.Game.instance;
                    var states = game.States;
                    var sheet = game.TableSheets.MaterialItemSheet;
                    var mail = new UnloadFromMyGaragesRecipientMail(
                        game.Agent.BlockIndex,
                        Guid.NewGuid(),
                        game.Agent.BlockIndex,
                        fungibleAssetValue: new[]
                        {
                            (
                                states.AgentState.address,
                                new FungibleAssetValue(
                                    states.GoldBalanceState.Gold.Currency,
                                    9,
                                    99)
                            ),
                            (states.CurrentAvatarState.address, 99 * Currencies.Crystal),
                        },
                        fungibleIdAndCount: sheet.OrderedList!.Take(3)
                            .Select((row, index) => (
                                row.ItemId,
                                index + 1)),
                        "memo")
                    {
                        New = true,
                    };
                    var mailBox = states.CurrentAvatarState.mailBox;
                    mailBox.Add(mail);
                    mailBox.CleanUp();
                    states.CurrentAvatarState.mailBox = mailBox;
                    LocalLayerModifier.AddNewMail(
                        game.States.CurrentAvatarState.address,
                        mail.id);
                }
            }
        }
#endif
    }
}
