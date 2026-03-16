using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Model;
using Nekoyume.Model.Quest;
using Nekoyume.UI.Module;
using UnityEngine;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Scroller;
using TMPro;
using Unity.Mathematics;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using mixpanel;
    using UniRx;

    public class WorldMap : Widget
    {
        private enum WorldMapMode
        {
            Normal,
            Hard,
        }

        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new(false);
            public readonly ReactiveProperty<int> SelectedWorldId = new(1);
            public readonly ReactiveProperty<int> SelectedStageId = new(1);

            public WorldInformation WorldInformation;
            public List<int> UnlockedWorldIds;
        }

        [Serializable]
        public class EventDungeonObject
        {
            public int eventId;
            public WorldButton button;
            public GameObject remainingTimeObject;
            public TextMeshProUGUI remainingTimeText;
        }

        [SerializeField]
        private GameObject worldMapRoot;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private WorldButton[] _worldButtons;

        [Header("Mode Toggle")]
        [SerializeField]
        private Button normalModeButton;

        [SerializeField]
        private Button hardModeButton;

        private GameObject _normalModeSelectedImage;
        private GameObject _normalModeNormalText;
        private GameObject _normalModeSelectedText;
        private GameObject _hardModeSelectedImage;
        private GameObject _hardModeNormalText;
        private GameObject _hardModeSelectedText;

        [SerializeField]
        [Tooltip("Hard-mode world id base for buttons when WorldSheet rows are missing. e.g., 101 => 101..109")]
        private int hardWorldIdBase = 101;

        [SerializeField]
        [Tooltip("Background image to swap on mode change.")]
        private Image backgroundImage;

        [SerializeField]
        private Sprite normalBackgroundSprite;

        [SerializeField]
        private Sprite hardBackgroundSprite;

        [SerializeField]
        [Tooltip("Normal mode effect object (Bg_Effect_Normal). Active in Normal mode.")]
        private GameObject normalEffectObject;

        [SerializeField]
        [Tooltip("Hard mode effect object (Bg_Effect_Hard). Active in Hard mode.")]
        private GameObject hardEffectObject;

        [SerializeField]
        [Tooltip("Normal mode texture object (Bg_Tex_Normal). Active in Normal mode.")]
        private GameObject normalTexObject;

        [SerializeField]
        [Tooltip("Hard mode texture object (Bg_Tex_Hard). Active in Hard mode.")]
        private GameObject hardTexObject;

        [Header("Debug (local test)")]
        [SerializeField]
        [Tooltip("If enabled (Editor/Development only), stages up to the given id are treated as cleared in this WorldMap UI.")]
        private bool debugAssumeClearedStages;

        [SerializeField]
        [Tooltip("Max stage id to assume as cleared when debugAssumeClearedStages is enabled. e.g., 450 means cleared up to World 9 end.")]
        private int debugAssumeClearedToStageId = 450;

        [SerializeField]
        [Tooltip("If enabled (Editor/Development only), worlds up to the given id are treated as unlocked/opened in this WorldMap UI.")]
        private bool debugAssumeWorldsUnlocked;

        [SerializeField]
        [Tooltip("Max world id to assume as unlocked when debugAssumeWorldsUnlocked is enabled. e.g., 9 means World 1~9 opened.")]
        private int debugAssumeUnlockedToWorldId = 9;

        [SerializeField]
        private EventDungeonObject[] eventDungeonObjects;

        [SerializeField]
        private Button eventDungeonLockButton;

        [SerializeField]
        private WorldMapAdventureBoss worldMapAdventureBossButton;

        [SerializeField]
        private WorldMapInfiniteTower worldMapInfiniteTowerButton;

        private readonly List<IDisposable> _disposablesAtShow = new();

        public ViewModel SharedViewModel { get; private set; }

        public bool HasNotification { get; private set; }

        public int StageIdToNotify { get; private set; }

        private WorldMapMode _currentMode = WorldMapMode.Normal;
        private int _lastSelectedWorldIdNormal = 1;
        private int _lastSelectedWorldIdHard = -1;

#region Mono

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
                Lobby.Enter(true);
            });

            CloseWidget = () =>
            {
                Close();
                Lobby.Enter(true);
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = TableSheets.Instance.StageWaveSheet.First?.StageId ?? 1;
            SharedViewModel = new ViewModel
            {
                SelectedStageId =
                {
                    Value = firstStageId
                }
            };

            foreach (var worldButton in _worldButtons)
            {
                worldButton.OnClickSubject
                    .Subscribe(button =>
                    {
                        if (button.IsUnlockable)
                        {
                            if (!ShowManyWorldUnlockPopup(SharedViewModel.WorldInformation))
                            {
                                ShowWorldUnlockPopup(button.Id);
                            }
                        }
                        else
                        {
                            ShowWorld(button.Id);
                        }
                    }).AddTo(gameObject);
            }

            InitializeModeToggles();
            ApplyMode(_currentMode, force: true);

            foreach (var eventDungeonButton in eventDungeonObjects.Select(i => i.button))
            {
                eventDungeonButton.Lock();
                eventDungeonButton.Hide();
                eventDungeonButton.OnClickSubject.Subscribe(_ =>
                {
                    if (RxProps.EventScheduleRowForDungeon.Value is null)
                    {
                        return;
                    }

                    ShowEventDungeonStage(RxProps.EventDungeonRow, false);
                }).AddTo(gameObject);
            }

            eventDungeonLockButton.onClick.AddListener(() =>
            {
                if (RxProps.EventScheduleRowForDungeon.Value is null)
                {
                    NotificationSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS"),
                        NotificationCell.NotificationType.Information);
                }
            });
        }

#endregion

        private bool IsStageClearedForUi(WorldInformation worldInformation, int stageId)
        {
            if (worldInformation is null)
            {
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug-only override: treat stages up to the configured id as cleared.
            // (Used for reproducing/validating world unlock UI flows.)
            if (debugAssumeClearedStages && stageId > 0 && stageId <= debugAssumeClearedToStageId)
            {
                return true;
            }
#endif

            return worldInformation.IsStageCleared(stageId);
        }

        private bool IsWorldOpenedInLegacyForUi(int worldId)
        {
            // Legacy opened-world list (separate from WorldInformation) is used to determine whether to show
            // "crystal lock" (Unlockable) animation/state.
            var opened = SharedViewModel?.UnlockedWorldIds != null &&
                SharedViewModel.UnlockedWorldIds.Contains(worldId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!opened &&
                debugAssumeWorldsUnlocked &&
                _currentMode == WorldMapMode.Normal &&
                worldId > 0 &&
                worldId <= debugAssumeUnlockedToWorldId)
            {
                opened = true;
            }
#endif

            return opened;
        }

        private bool IsWorldUnlockedForUi(WorldInformation worldInformation, int worldId, bool canTryThisWorld)
        {
            if (worldInformation is null)
            {
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugAssumeWorldsUnlocked &&
                _currentMode == WorldMapMode.Normal &&
                worldId > 0 &&
                worldId <= debugAssumeUnlockedToWorldId)
            {
                return true;
            }
#endif

            return (worldInformation.TryGetWorld(worldId, out var worldModel) && worldModel.IsUnlocked) ||
                canTryThisWorld;
        }

        private void InitializeModeToggles()
        {
            if (normalModeButton == null && hardModeButton == null)
            {
                _currentMode = WorldMapMode.Normal;
                return;
            }

            if (normalModeButton != null)
            {
                var t = normalModeButton.transform;
                _normalModeSelectedImage = t.Find("SelectedImage")?.gameObject;
                _normalModeNormalText    = t.Find("NormalText")?.gameObject;
                _normalModeSelectedText  = t.Find("SelectedText")?.gameObject;
            }

            if (hardModeButton != null)
            {
                var t = hardModeButton.transform;
                _hardModeSelectedImage = t.Find("SelectedImage")?.gameObject;
                _hardModeNormalText    = t.Find("NormalText")?.gameObject;
                _hardModeSelectedText  = t.Find("SelectedText")?.gameObject;
            }

            normalModeButton?.onClick.AddListener(() => ApplyMode(WorldMapMode.Normal));
            hardModeButton?.onClick.AddListener(() => ApplyMode(WorldMapMode.Hard));

            // 초기 상태: Normal 선택
            _currentMode = WorldMapMode.Normal;
            SetModeButtonVisual(isSelected: true,  _normalModeSelectedImage, _normalModeNormalText, _normalModeSelectedText);
            SetModeButtonVisual(isSelected: false, _hardModeSelectedImage,   _hardModeNormalText,   _hardModeSelectedText);
        }

        private void ApplyMode(WorldMapMode mode, bool force = false)
        {
            if (!force && _currentMode == mode)
            {
                return;
            }

            var prevMode = _currentMode;
            SaveLastSelectedWorldIdForMode(_currentMode);
            _currentMode = mode;

            var normalSelected = mode == WorldMapMode.Normal;
            SetModeButtonVisual(normalSelected,  _normalModeSelectedImage, _normalModeNormalText, _normalModeSelectedText);
            SetModeButtonVisual(!normalSelected, _hardModeSelectedImage,   _hardModeNormalText,   _hardModeSelectedText);

            BindWorldButtonsForMode(mode);

            if (backgroundImage != null)
            {
                var sprite = mode == WorldMapMode.Hard ? hardBackgroundSprite : normalBackgroundSprite;
                if (sprite != null)
                    backgroundImage.sprite = sprite;
            }

            var isHard = mode == WorldMapMode.Hard;

            if (normalEffectObject != null) normalEffectObject.SetActive(!isHard);
            if (hardEffectObject != null)   hardEffectObject.SetActive(isHard);

            if (normalTexObject != null) normalTexObject.SetActive(!isHard);
            if (hardTexObject != null)   hardTexObject.SetActive(isHard);

            SetWorldInformation(SharedViewModel.WorldInformation);

            RestoreLastSelectedWorldForMode(mode);
        }

        private static void SetModeButtonVisual(bool isSelected, GameObject selectedImage, GameObject normalText, GameObject selectedText)
        {
            if (selectedImage != null) selectedImage.SetActive(isSelected);
            if (normalText   != null) normalText.SetActive(!isSelected);
            if (selectedText != null) selectedText.SetActive(isSelected);
        }

        private void SaveLastSelectedWorldIdForMode(WorldMapMode mode)
        {
            var worldId = SharedViewModel?.SelectedWorldId.Value ?? 1;
            switch (mode)
            {
                case WorldMapMode.Normal:
                    _lastSelectedWorldIdNormal = worldId;
                    break;
                case WorldMapMode.Hard:
                    _lastSelectedWorldIdHard = worldId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private void RestoreLastSelectedWorldForMode(WorldMapMode mode)
        {
            var worldIds = GetWorldIdsForMode(mode);
            if (worldIds.Count <= 0)
            {
                return;
            }

            var desiredWorldId = mode == WorldMapMode.Hard && _lastSelectedWorldIdHard > 0
                ? _lastSelectedWorldIdHard
                : _lastSelectedWorldIdNormal;

            if (!worldIds.Contains(desiredWorldId))
            {
                desiredWorldId = worldIds[0];
            }

            if (SharedViewModel != null)
            {
                SharedViewModel.SelectedWorldId.Value = desiredWorldId;
            }
        }

        private void BindWorldButtonsForMode(WorldMapMode mode)
        {
            for (var i = 0; i < _worldButtons.Length; i++)
            {
                var worldButton = _worldButtons[i];
                var worldId = GetWorldIdForSlot(mode, i);
                if (worldId <= 0)
                {
                    worldButton.Hide();
                    continue;
                }

                var worldSheet = TableSheets.Instance.WorldSheet;
                if (worldSheet.TryGetValue(worldId, out var rowData, false))
                {
                    worldButton.Set(rowData);
                }
                else
                {
                    // Show as locked placeholder even if the sheet row is missing.
                    worldButton.Set(worldId);
                }

                worldButton.Show();
            }
        }

        private List<int> GetWorldIdsForMode(WorldMapMode mode)
        {
            if (mode == WorldMapMode.Hard)
            {
                // Prefer data-driven hard-world rows from WorldSheet.
                // Fallback to synthetic id range only when the sheet doesn't contain hard rows.
                var hardRows = GetWorldRowsForMode(WorldMapMode.Hard);
                if (hardRows.Count > 0)
                {
                    return hardRows.Select(r => r.Id).ToList();
                }

                return Enumerable.Range(hardWorldIdBase, _worldButtons.Length).ToList();
            }

            // Normal mode: use existing WorldSheet rows.
            return GetWorldRowsForMode(WorldMapMode.Normal).Select(r => r.Id).ToList();
        }

        private int GetWorldIdForSlot(WorldMapMode mode, int index)
        {
            if (index < 0 || index >= _worldButtons.Length)
            {
                return -1;
            }

            if (mode == WorldMapMode.Hard)
            {
                var hardRows = GetWorldRowsForMode(WorldMapMode.Hard);
                if (hardRows.Count > 0)
                {
                    return hardRows.Count > index ? hardRows[index].Id : -1;
                }

                return hardWorldIdBase + index;
            }

            var normalRows = GetWorldRowsForMode(WorldMapMode.Normal);
            return normalRows.Count > index ? normalRows[index].Id : -1;
        }

        private List<WorldSheet.Row> GetWorldRowsForMode(WorldMapMode mode)
        {
            var worldSheet = TableSheets.Instance.WorldSheet;
            return worldSheet.OrderedList
                .Where(row => row.Id != GameConfig.MimisbrunnrWorldId)
                .Where(row => mode == WorldMapMode.Hard ? IsHardWorld(row) : !IsHardWorld(row))
                .OrderBy(row => row.Id)
                .ToList();
        }

        private static bool IsHardWorld(WorldSheet.Row row)
        {
            if (row is null)
            {
                return false;
            }

            return row.Id >= 10 && row.Id < GameConfig.MimisbrunnrWorldId;
        }

        public void Show(WorldInformation worldInformation, bool blockWorldUnlockPopup = false)
        {
            UpdateAssets();

            HasNotification = false;
            SetWorldInformation(worldInformation);

            if (hardModeButton != null)
            {
                var hardWorldIds = GetWorldIdsForMode(WorldMapMode.Hard);
                var isHardModeUnlocked = hardWorldIds.Any(id =>
                    worldInformation?.IsWorldUnlocked(id) ?? false);
                var targetMode = isHardModeUnlocked ? WorldMapMode.Hard : WorldMapMode.Normal;
                ApplyMode(targetMode);
            }

            var status = Find<Status>();
            status.Close(true);
            Show(true);

            if (!blockWorldUnlockPopup)
            {
                ShowManyWorldUnlockPopup(worldInformation);
            }

            Find<AdventureBossRewardPopup>().Show();
        }

        public void Show(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            ShowWorld(worldId, stageId, showWorld, callByShow);
            Show(true);
            Find<AdventureBossRewardPopup>().Show();
        }

        public void UpdateAssets(bool isForceSetBattle = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            RxProps.EventScheduleRowForDungeon.Subscribe(value =>
            {
                foreach (var eventDungeonObject in eventDungeonObjects)
                {
                    eventDungeonObject.button.Hide();
                    eventDungeonObject.remainingTimeObject.SetActive(false);
                }

                if (isForceSetBattle || value is null)
                {
                    Find<HeaderMenuStatic>()
                        .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                    eventDungeonLockButton.gameObject.SetActive(true);
                }
                else
                {
                    Find<HeaderMenuStatic>()
                        .UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);

                    eventDungeonLockButton.gameObject.SetActive(false);
                    var eventDungeonObject = eventDungeonObjects.LastOrDefault(o => o.eventId == value.Id) ??
                        eventDungeonObjects.First();
                    eventDungeonObject.button.Show();
                    eventDungeonObject.button.HasNotification.Value = true;
                    eventDungeonObject.button.Unlock();
                    eventDungeonObject.remainingTimeObject.SetActive(true);

                    if (eventDungeonObject.remainingTimeText == null)
                    {
                        return;
                    }

                    RxProps.EventDungeonRemainingTimeText
                        .SubscribeTo(eventDungeonObject.remainingTimeText)
                        .AddTo(_disposablesAtShow);
                }
            }).AddTo(_disposablesAtShow);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            base.Close(true);
        }

        public void SetWorldInformation(WorldInformation worldInformation)
        {
            SharedViewModel.WorldInformation = worldInformation;
            if (worldInformation is null)
            {
                return;
            }

            foreach (var worldButton in _worldButtons)
            {
                if (!worldButton.IsShown)
                {
                    continue;
                }

                var buttonWorldId = worldButton.Id;
                var unlockRow = TableSheets.Instance.WorldUnlockSheet
                    .OrderedList
                    .FirstOrDefault(row => row.WorldIdToUnlock == buttonWorldId);
                var unlockRowConsistent = unlockRow is null ||
                    !TableSheets.Instance.WorldSheet.TryGetByStageId(unlockRow.StageId, out var stageWorld) ||
                    stageWorld.Id == unlockRow.WorldId;
                var canTryThisWorld =
                    unlockRowConsistent &&
                    IsStageClearedForUi(worldInformation, unlockRow?.StageId ?? int.MaxValue);
                var worldIsUnlocked = IsWorldUnlockedForUi(worldInformation, buttonWorldId, canTryThisWorld);
                var openedLegacy = IsWorldOpenedInLegacyForUi(worldButton.Id);

                UpdateNotificationInfo();

                var isIncludedInQuest = StageIdToNotify >= worldButton.StageBegin &&
                    StageIdToNotify <= worldButton.StageEnd;

                if (worldIsUnlocked)
                {
                    worldButton.HasNotification.Value = isIncludedInQuest;
                    var crystalLock = SharedViewModel.UnlockedWorldIds != null && !openedLegacy;
                    worldButton.Unlock(crystalLock);
                }
                else
                {
                    worldButton.Lock();
                }

                SetWorldOpenCostTextColor(States.Instance.CrystalBalance);
            }

            if (!worldInformation.TryGetFirstWorld(out _))
            {
                throw new Exception("worldInformation.TryGetFirstWorld() failed!");
            }
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
            {
                var unlockConditionRow =
                    TableSheets.Instance.WorldUnlockSheet.OrderedList
                        .FirstOrDefault(row =>
                            row.WorldIdToUnlock == worldId);
                if (unlockConditionRow is null ||
                    !IsStageClearedForUi(SharedViewModel.WorldInformation, unlockConditionRow.StageId))
                {
                    throw new ArgumentException(nameof(worldId));
                }

                var worldSheet = TableSheets.Instance.WorldSheet;
                SharedViewModel.WorldInformation.UnlockWorld(worldId, 0, worldSheet);
                SharedViewModel.WorldInformation.TryGetWorld(worldId, out world);
            }

            if (worldId == 1)
            {
                Analyzer.Instance.Track("Unity/Click Yggdrasil", new Dictionary<string, Value>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                });
            }

            Push();
            ShowWorld(world.Id, world.GetNextStageId(), false);
        }

        private void ShowWorld(
            int worldId,
            int stageId,
            bool showWorld,
            bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }


            TableSheets.Instance.WorldSheet.TryGetValue(
                worldId,
                out var worldRow,
                true);
            SharedViewModel.SelectedWorldId.Value = worldId;
            SharedViewModel.SelectedStageId.Value = stageId;
            var stageInfo = Find<StageInformation>();
            stageInfo.Show(SharedViewModel, worldRow, StageType.HackAndSlash);
            UpdateNotificationInfo();
            UpdateAssets(true);
            Find<HeaderMenuStatic>().Show();
        }

        public void ShowEventDungeonStage(
            EventDungeonSheet.Row eventDungeonRow,
            bool showWorld,
            bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }

            Show(true);
            var openedStageId =
                RxProps.EventDungeonInfo.Value is null ||
                RxProps.EventDungeonInfo.Value.ClearedStageId == 0
                    ? RxProps.EventDungeonRow.StageBegin
                    : math.min(
                        RxProps.EventDungeonInfo.Value.ClearedStageId + 1,
                        RxProps.EventDungeonRow.StageEnd);
            SharedViewModel.SelectedWorldId.Value = eventDungeonRow.Id;
            SharedViewModel.SelectedStageId.Value = openedStageId;
            var stageInfo = Find<StageInformation>();
            stageInfo.Show(
                SharedViewModel,
                eventDungeonRow,
                openedStageId,
                openedStageId);
            StageIdToNotify = openedStageId;
            UpdateAssets();
            Find<HeaderMenuStatic>().Show();
        }

        public void UpdateNotificationInfo()
        {
            var questStageId = Game.Game.instance.States.CurrentAvatarState.questList?
                .OfType<WorldQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.Goal)
                .FirstOrDefault()?
                .Goal ?? -1;
            StageIdToNotify = questStageId;

            HasNotification = questStageId > 0;
        }

        private void CallByShowUpdateWorld()
        {
            var status = Find<Status>();
            status.Close(true);
            worldMapRoot.SetActive(true);
        }

        private void ShowWorldUnlockPopup(int worldId)
        {
            var cost = CrystalCalculator.CalculateWorldUnlockCost(
                    new[] { worldId },
                    TableSheets.Instance.WorldUnlockSheet)
                .MajorUnit;
            var balance = States.Instance.CrystalBalance;
            var usageMessage = L10nManager.Localize(
                "UI_UNLOCK_WORLD_FORMAT",
                L10nManager.LocalizeWorldName(worldId));
            Find<PaymentPopup>().ShowCheckPaymentCrystal(
                balance.MajorUnit,
                cost,
                balance.GetPaymentFormatText(usageMessage, cost),
                () =>
                {
                    Find<LoadingScreen>().Show(LoadingScreen.LoadingType.WorldUnlock);
                    ActionManager.Instance.UnlockWorld(new List<int> { worldId }, (int)cost)
                        .Subscribe();
                });
        }

        private bool ShowManyWorldUnlockPopup(WorldInformation worldInformation)
        {
            if (!worldInformation.TryGetLastClearedStageId(out _))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!debugAssumeClearedStages)
                {
                    return false;
                }
#else
                return false;
#endif
            }

            var tableSheets = TableSheets.Instance;
            var worldUnlockSheet = tableSheets.WorldUnlockSheet;
            var worldSheet = tableSheets.WorldSheet;

            bool IsWorldIdInCurrentMode(int worldId)
            {
                if (!worldSheet.TryGetValue(worldId, out var rowData, false))
                {
                    return false;
                }

                // Exclude Mímisbrunnr here as well (id-based hard classification could otherwise include it).
                if (rowData.Id == GameConfig.MimisbrunnrWorldId)
                {
                    return false;
                }

                var isHardWorld = IsHardWorld(rowData);
                return _currentMode == WorldMapMode.Hard ? isHardWorld : !isHardWorld;
            }

            bool IsUnlockConditionMet(WorldUnlockSheet.Row row)
            {
                if (row is null)
                {
                    return false;
                }

                if (!IsStageClearedForUi(worldInformation, row.StageId))
                {
                    return false;
                }

                // If the sheet is consistent, the world that contains stageId should match row.WorldId.
                // When it doesn't, avoid suggesting unlocks based on ambiguous data.
                if (worldSheet.TryGetByStageId(row.StageId, out var containingWorldRow) &&
                    containingWorldRow.Id != row.WorldId)
                {
                    return false;
                }

                return true;
            }

            var worldIdListForUnlock = worldUnlockSheet.OrderedList
                .Where(IsUnlockConditionMet)
                .Select(r => r.WorldIdToUnlock)
                .Distinct()
                .Where(IsWorldIdInCurrentMode)
                .Where(worldId => !worldInformation.IsWorldUnlocked(worldId))
                .Where(worldId => !IsWorldOpenedInLegacyForUi(worldId))
                .OrderBy(i => i)
                .ToList();

            if (worldIdListForUnlock.Count <= 1)
            {
                return false;
            }

            var paymentPopup = Find<PaymentPopup>();
            var cost = CrystalCalculator.CalculateWorldUnlockCost(worldIdListForUnlock,
                tableSheets.WorldUnlockSheet).MajorUnit;
            paymentPopup.ShowCheckPaymentCrystal(
                States.Instance.CrystalBalance.MajorUnit,
                cost,
                L10nManager.Localize("CRYSTAL_MIGRATION_WORLD_ALL_OPEN_FORMAT", cost),
                () =>
                {
                    Find<LoadingScreen>().Show(LoadingScreen.LoadingType.WorldUnlock);
                    ActionManager.Instance.UnlockWorld(worldIdListForUnlock, (int)cost)
                        .Subscribe();
                });

            return true;
        }

        private int GetCountOfCanUnlockWorld(int stageId)
        {
            var countOfCanUnlockWorld = 0;
            var copyOfStageId = stageId;

            while(copyOfStageId > 0)
            {
                var stageGap = GetStageGap(copyOfStageId);
                copyOfStageId -= stageGap;
                countOfCanUnlockWorld++;
            }

            return countOfCanUnlockWorld;
        }

        private int GetStageGap(int stageId)
        {
            var stageGap = 50;
            var worldSheet = TableSheets.Instance.WorldSheet;
            var currentWorld = worldSheet?
                .OrderedList?
                .FirstOrDefault(row => row.StageBegin <= stageId && row.StageEnd >= stageId);

            if (currentWorld is null)
            {
                return stageGap;
            }

            stageGap = currentWorld.StageEnd - currentWorld.StageBegin + 1;

            if (stageGap > 0)
            {
                return stageGap;
            }

            NcDebug.LogWarning($"Invalid stage gap computed: {stageGap}. Using default value of 50.");
            stageGap = 50;

            return stageGap;
        }

        private void SetWorldOpenCostTextColor(FungibleAssetValue crystal)
        {
            foreach (var worldButton in _worldButtons)
            {
                worldButton.SetOpenCostTextColor(crystal.MajorUnit);
            }
        }

        public void SetAdventureBossButtonLoading(bool isLoading)
        {
            worldMapAdventureBossButton.SetLoadingIndicator(isLoading);
        }
    }
}
