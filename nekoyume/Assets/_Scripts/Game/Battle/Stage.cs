#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif
//#define TEST_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using mixpanel;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Trigger;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using CharacterBase = Nekoyume.Model.CharacterBase;
using Enemy = Nekoyume.Model.Enemy;
using EnemyPlayer = Nekoyume.Model.EnemyPlayer;
using Player = Nekoyume.Game.Character.Player;
using Random = UnityEngine.Random;
using Skill = Nekoyume.Model.BattleStatus.Skill;

namespace Nekoyume.Game.Battle
{
    public class Stage : MonoBehaviour, IStage
    {
        public const float DefaultAnimationTimeScaleWeight = 1f;
        public const float AcceleratedAnimationTimeScaleWeight = 1.6f;
        public const float StageStartPosition = -1.2f;
        private const float SkillDelay = 0.1f;
        public ObjectPool objectPool;
        public DropItemFactory dropItemFactory;

        public MonsterSpawner spawner;

        private GameObject _background;

        // dummy for stage background moving.
        public GameObject dummy;
        public ParticleSystem defaultBGVFX;
        public ParticleSystem bosswaveBGVFX;

        public int worldId;
        public int stageId;
        public int waveCount;
        public bool newlyClearedStage;
        public int waveNumber;
        public int waveTurn;

        public int foodCount;
        public string zone;
        public Animator roomAnimator { get; private set; }

        private Camera _camera;
        private BattleLog _battleLog;
        private List<ItemBase> _rewards;
        private BattleResultPopup.Model _battleResultModel;
        private Coroutine _battleCoroutine;
        private Player _stageRunningPlayer;
        private Vector3 _playerPosition;
        private Coroutine _positionCheckCoroutine;
        private List<int> prevFood;
        private List<BattleTutorialController.BattleTutorialModel> _tutorialModels = new();

        public StageType StageType { get; set; }
        public Player SelectedPlayer { get; set; }
        public List<GameObject> ReleaseWhiteList { get; private set; } = new List<GameObject>();
        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public TutorialController TutorialController { get; private set; }
        public Enemy Boss { get; private set; }
        public AvatarState AvatarState { get; set; }
        public bool IsShowHud { get; set; }
        public bool IsExitReserved { get; set; }
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }
        public int PlayCount { get; set; }
        public float AnimationTimeScaleWeight { get; set; } = DefaultAnimationTimeScaleWeight;

        public Vector3 SelectPositionBegin(int index) =>
            new Vector3(-2.15f + index * 2.22f, -1.79f, 0.0f);

        public Vector3 SelectPositionEnd(int index) =>
            new Vector3(-2.15f + index * 2.22f, -0.25f, 0.0f);

        public bool showLoadingScreen;

        #region Events

        private readonly ISubject<Stage> _onEnterToStageEnd = new Subject<Stage>();
        public IObservable<Stage> OnEnterToStageEnd => _onEnterToStageEnd;

        public readonly ISubject<Stage> OnRoomEnterEnd = new Subject<Stage>();

        #endregion

        protected void Awake()
        {
            _camera = ActionCamera.instance.Cam;
            if (_camera is null)
            {
                throw new NullReferenceException("`Camera.main` can't be null.");
            }

            if (dummy is null)
            {
                throw new NullReferenceException("`Dummy` can't be null.");
            }

            Event.OnNestEnter.AddListener(OnNestEnter);
            Event.OnLoginDetail.AddListener(OnLoginDetail);
            Event.OnRoomEnter.AddListener(OnRoomEnter);

            BattleRenderer.Instance.OnStageStart += OnStartStage;
        }

        private void OnDestroy()
        {
            Event.OnNestEnter.RemoveListener(OnNestEnter);
            Event.OnLoginDetail.RemoveListener(OnLoginDetail);
            Event.OnRoomEnter.RemoveListener(OnRoomEnter);

            BattleRenderer.Instance.OnStageStart -= OnStartStage;
        }

        public void Initialize()
        {
            objectPool.Initialize();
            dropItemFactory.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
            TutorialController = new TutorialController(MainCanvas.instance.Widgets);
        }

        public void UpdateTimeScale()
        {
            foreach (var character in GetComponentsInChildren<Actor>())
            {
                var isEnemy = character is Character.StageMonster;
                character.Animator.TimeScale = isEnemy
                    ? Actor.AnimatorTimeScale * AnimationTimeScaleWeight
                    : AnimationTimeScaleWeight;
                if (character.RunSpeed != 0f)
                {
                    character.RunSpeed = isEnemy
                        ? -1 * AnimationTimeScaleWeight
                        : character.CharacterModel.RunSpeed * AnimationTimeScaleWeight;
                }
            }
        }

        private void OnStartStage(BattleLog log)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(OnStageStart)}() enter");
#endif
            if (_battleLog is null)
            {
                if (_battleCoroutine is not null)
                {
                    StopCoroutine(_battleCoroutine);
                    _battleCoroutine = null;
                    objectPool.ReleaseAll();
                }

                _battleLog = log;
                PlayStage(_battleLog);
            }
            else
            {
                NcDebug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private void OnNestEnter()
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(OnNestEnter)}() enter");
#endif
            gameObject.AddComponent<NestEntering>();
        }

        private void OnLoginDetail(int index)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(OnLoginDetail)}({index}) enter");
#endif
            DOTween.KillAll();
            var players = Widget.Find<Login>().players;
            for (var i = 0; i < players.Count; ++i)
            {
                var player = players[i];
                var playerObject = player.gameObject;
                var anim = player.Animator;
                if (index == i)
                {
                    var moveTo = new Vector3(-0.5f, -0.5f);
                    playerObject.transform.DOScale(1.1f, 2.0f).SetDelay(0.2f);
                    playerObject.transform.DOMove(moveTo, 1.3f).SetDelay(0.2f);
                    var seqPos = new Vector3(
                        moveTo.x,
                        moveTo.y - Random.Range(0.05f, 0.1f),
                        0.0f);
                    var seq = DOTween.Sequence();
                    seq.Append(playerObject.transform.DOMove(
                        seqPos,
                        Random.Range(4.0f, 5.0f)));
                    seq.Append(playerObject.transform.DOMove(
                        moveTo,
                        Random.Range(4.0f, 5.0f)));
                    seq.Play().SetDelay(2.6f).SetLoops(-1);
                    if (!ReferenceEquals(anim, null) && !anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        player.SpineController.Appear();
                    }

                    SelectedPlayer = players[i];
                }
                else
                {
                    playerObject.transform.DOScale(0.9f, 1.0f);
                    playerObject.transform.DOMoveY(-5.4f, 3.0f);

                    if (!ReferenceEquals(anim, null) && anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        player.SpineController.Disappear();
                    }
                }
            }
        }

        private void OnRoomEnter(bool showScreen)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(OnRoomEnter)}() enter");
#endif
            showLoadingScreen = showScreen;
            gameObject.AddComponent<RoomEntering>();
            BattleRenderer.Instance.IsOnBattle = false;
        }

        public void LoadBackground(string prefabName, float fadeTime = 0.0f)
        {
            var hasPrevBackground = _background != null;
            if (hasPrevBackground)
            {
                if (_background.name.Equals(prefabName))
                    return;

                if (fadeTime > 0.0f)
                {
                    if (_background.TryGetComponent<BackgroundGroup>(out var prevBackgroundGroup))
                    {
                        prevBackgroundGroup.FadeOut(fadeTime);
                    }
                    // TODO: 임시코드, 캐싱 전략 정해지면 수정 필요
                    // fade와 동시에 destroy 되는 것을 방지하기 위해 padding을 줌
                    DestroyBackground(fadeTime + 0.1f);
                }
                else
                {
                    DestroyBackground();
                }
            }

            var path = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(path);
            if (!prefab)
                throw new FailedToLoadResourceException<GameObject>(path);

            _background = Instantiate(prefab, transform);
            _background.name = prefabName;

            foreach (Transform child in _background.transform)
            {
                var childName = child.name;
                if (!childName.StartsWith("bgvfx"))
                    continue;

                var num = childName.Substring(childName.Length - 2);
                switch (num)
                {
                    case "01":
                        defaultBGVFX = child.GetComponent<ParticleSystem>();
                        break;
                    case "02":
                        bosswaveBGVFX = child.GetComponent<ParticleSystem>();
                        break;
                }
            }

            if (_background.TryGetComponent<BackgroundGroup>(out var backgroundGroup))
            {
                if (hasPrevBackground)
                {
                    backgroundGroup.SetBackgroundAlpha(0);
                    backgroundGroup.FadeIn(fadeTime);
                }
                else
                {
                    backgroundGroup.SetBackgroundAlpha(1);
                }
            }
        }

        public void ReleaseBattleAssets()
        {
            BattleRenderer.Instance.ReleaseMonsterResources();
            DestroyBackground();
        }

        private void DestroyBackground(float fadeTime = 0f)
        {
            Destroy(_background, fadeTime);

            _background = null;
#if UNITY_ANDROID || UNITY_IOS
            objectPool.RemoveAllExceptFirst();
#endif
        }

        public void PlayStage(BattleLog log)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(PlayStage)}() enter");
#endif
            if (log?.Count <= 0)
            {
                return;
            }

            _battleCoroutine = StartCoroutine(CoPlayStage(log));
        }

        private IEnumerator CoPlayStage(BattleLog log)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoPlayStage)}() enter");
#endif
            var avatarState = States.Instance.CurrentAvatarState;
            prevFood = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Consumable>()
                .Where(s => s.ItemSubType == ItemSubType.Food)
                .Select(r => r.Id)
                .ToList();

            BattleRenderer.Instance.IsOnBattle = true;

            yield return StartCoroutine(CoStageEnter(log));
            foreach (var e in log)
            {
                e.LogEvent();
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoStageEnd(log));
            ClearBattle();
        }

        public void ClearBattle()
        {
            _battleLog = null;
            if (_battleCoroutine is null)
            {
                return;
            }

            StopCoroutine(_battleCoroutine);
            _battleCoroutine = null;
        }

        private static IEnumerator CoGuidedQuest(int stageIdToClear)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoGuidedQuest)}() enter. stageIdToClear: {stageIdToClear}");
#endif
            var done = false;
            var battle = Widget.Find<UI.Battle>();
            battle.ClearStage(stageIdToClear, cleared => done = true);
            yield return new WaitUntil(() => done);
        }

        private static IEnumerator CoUnlockRecipe(int stageIdToFirstClear)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoUnlockRecipe)}() enter. stageIdToFirstClear: {stageIdToFirstClear}");
#endif
            var questResult = Widget.Find<CelebratesPopup>();
            var rows = TableSheets.Instance.EquipmentItemRecipeSheet.OrderedList
                .Where(row => row.UnlockStage == stageIdToFirstClear)
                .Distinct()
                .ToList();
            foreach (var row in rows)
            {
                questResult.Show(row);
                yield return new WaitWhile(() => questResult.IsActive());
            }
        }

        private IEnumerator CoStageEnter(BattleLog log)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoStageEnter)}() enter");
#endif
            worldId = log.worldId;
            stageId = log.stageId;
            waveCount = log.waveCount;
            newlyClearedStage = log.newlyCleared;
            _tutorialModels.Clear();

            string bgmName = null;
            switch (StageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    if (!TableSheets.Instance.StageSheet.TryGetValue(stageId, out var stageRow))
                    {
                        yield break;
                    }

                    zone = stageRow.Background;
                    bgmName = stageRow.BGM;
                    break;
                }
                case StageType.EventDungeon:
                {
                    if (TableSheets.Instance.EventDungeonStageSheet is null ||
                        !TableSheets.Instance.EventDungeonStageSheet.TryGetValue(stageId, out var eventDungeonStageRow))
                    {
                        yield break;
                    }

                    zone = eventDungeonStageRow.Background;
                    bgmName = eventDungeonStageRow.BGM;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _battleResultModel = new BattleResultPopup.Model
            {
                StageType = StageType,
            };

            AnimationTimeScaleWeight = DefaultAnimationTimeScaleWeight;
            LoadBackground(zone, 3.0f);
            PlayBGVFX(false);
            RunPlayer();
            ReleaseWhiteList.Clear();
            ReleaseWhiteList.Add(_stageRunningPlayer.gameObject);

            Widget.Find<UI.Battle>().StageProgressBar.Initialize(true);
            var title = Widget.Find<StageTitle>();
            title.Show(StageType, stageId);
            IsShowHud = false;
            yield return new WaitForSeconds(StageConfig.instance.stageEnterDelay);

            yield return StartCoroutine(title.CoClose());

            _stageRunningPlayer.Pet.Animator.Play(PetAnimation.Type.BattleStart);
            AudioController.instance.PlayMusic(bgmName);
            IsShowHud = true;

            SelectedPlayer.Model.worldInformation.TryGetLastClearedStageId(out var lastClearedStageIdBeforeResponse);
            _battleResultModel.LastClearedStageIdBeforeResponse = lastClearedStageIdBeforeResponse;

            if (!SelectedPlayer.Model.worldInformation.IsStageCleared(stageId))
            {
                _tutorialModels = Widget.Find<Tutorial>().TutorialController.GetModelListByStage(stageId);
            }
        }

        private IEnumerator CoStageEnd(BattleLog log)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoStageEnd)}() enter");
#endif
            IsAvatarStateUpdatedAfterBattle = false;
            // NOTE ActionRenderHandler.Instance.Pending should be false before _onEnterToStageEnd.OnNext() invoked.
            ActionRenderHandler.Instance.Pending = false;
            _onEnterToStageEnd.OnNext(this);
            if (_tutorialModels.FirstOrDefault(model => model.ClearedWave == 3) is {} tutorialModel)
            {
                Widget.Find<Tutorial>().PlaySmallGuide(tutorialModel.Id);
            }

            yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);
            var avatarState = States.Instance.CurrentAvatarState;

            _battleResultModel.ClearedWaveNumber = log.clearedWaveNumber;
            var characters = GetComponentsInChildren<Actor>();
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            yield return new WaitForSeconds(1f);
            Boss = null;
            Widget.Find<UI.Battle>().BossStatus.Close();
            var isClear = log.IsClear;
            if (isClear)
            {
                yield return StartCoroutine(CoGuidedQuest(log.stageId));
            }
            else
            {
                var enemies = GetComponentsInChildren<StageMonster>();
                if (enemies.Any())
                {
                    // TODO: 하드코딩된 수치 이용하지 말고 데이터 관리
                    const float winDuration = 1.0f;
                    foreach (var enemy in enemies)
                    {
                        enemy.WinAsync(winDuration).Forget();
                    }

                    yield return new WaitForSeconds(winDuration);
                }
            }

            Widget.Find<UI.Battle>().Close();
            Widget.Find<Tutorial>().Close(true);

            List<TableData.EquipmentItemRecipeSheet.Row> newRecipes = null;

            if (newlyClearedStage)
            {
                yield return StartCoroutine(CoUnlockMenu());
                yield return new WaitForSeconds(0.75f);
                newRecipes = TableSheets.Instance.EquipmentItemRecipeSheet.OrderedList
                    .Where(row => row.UnlockStage == stageId)
                    .Distinct()
                    .ToList();
            }

            IsShowHud = false;
            if (log.result == BattleLog.Result.Win)
            {
                _stageRunningPlayer.DisableHUD();
                _stageRunningPlayer.Animator.Win(log.clearedWaveNumber);
                _stageRunningPlayer.ShowSpeech("PLAYER_WIN");
                _stageRunningPlayer.Pet.Animator.Play(PetAnimation.Type.BattleEnd);
                yield return new WaitForSeconds(2.2f);
                if (isClear)
                {
                    StartCoroutine(CoSlideBg());
                }
            }
            else
            {
                if (log.result == BattleLog.Result.TimeOver)
                {
                    _stageRunningPlayer.Animator.TurnOver();
                    yield return new WaitForSeconds(2f);
                }

                ReleaseWhiteList.Remove(_stageRunningPlayer.gameObject);
            }
            objectPool.ReleaseExcept(ReleaseWhiteList);
            _stageRunningPlayer.ClearVfx();

            _battleResultModel.ActionPoint = ReactiveAvatarState.ActionPoint;
            _battleResultModel.State = log.result;
            switch (StageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    if (TableSheets.Instance.WorldSheet
                        .TryGetValue(log.worldId, out var worldRow))
                    {
                        _battleResultModel.WorldName = worldRow.GetLocalizedName();
                        _battleResultModel.IsEndStage = stageId == worldRow.StageEnd;
                    }
                    else
                    {
                        _battleResultModel.WorldName = string.Empty;
                        _battleResultModel.IsEndStage = false;
                    }

                    if (TableSheets.Instance.StageSheet
                        .TryGetValue(stageId, out var stageRow))
                    {
                        _battleResultModel.ActionPointNotEnough =
                            ReactiveAvatarState.ActionPoint < stageRow.CostAP;
                    }

                    break;
                }
                case StageType.EventDungeon:
                {
                    if (TableSheets.Instance.EventDungeonSheet is not null &&
                        TableSheets.Instance.EventDungeonSheet
                            .TryGetValue(log.worldId, out var eventDungeonRow))
                    {
                        _battleResultModel.WorldName = eventDungeonRow.GetLocalizedName();
                        _battleResultModel.IsEndStage = stageId == eventDungeonRow.StageEnd;
                    }
                    else
                    {
                        _battleResultModel.WorldName = string.Empty;
                        _battleResultModel.IsEndStage = false;
                    }

                    // NOTE: Event dungeon doesn't have action point cost.
                    // if (TableSheets.Instance.EventDungeonStageSheet
                    //     .TryGetValue(stageId, out var eventDungeonStageRow))
                    // {
                    //     _battleResultModel.ActionPointNotEnough =
                    //         avatarState.actionPoint < eventDungeonStageRow.CostAP;
                    // }
                    _battleResultModel.ActionPointNotEnough =
                        RxProps.EventDungeonTicketProgress.Value.currentTickets < PlayCount;

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _battleResultModel.WorldID = log.worldId;
            _battleResultModel.StageID = log.stageId;
            avatarState.worldInformation.TryGetLastClearedStageId(out var lasStageId);
            _battleResultModel.LastClearedStageId = lasStageId;
            _battleResultModel.IsClear = isClear;
            var isMulti = PlayCount > 1;

            if (IsExitReserved)
            {
                _battleResultModel.NextState = BattleResultPopup.NextState.GoToMain;
                _battleResultModel.ActionPointNotEnough = false;
            }
            else if (isMulti)
            {
                _battleResultModel.NextState = BattleResultPopup.NextState.None;
            }
            else if (_battleResultModel.ActionPointNotEnough)
            {
                _battleResultModel.NextState = BattleResultPopup.NextState.None;
            }
            else if (isClear)
            {
                if (_battleResultModel.IsEndStage)
                {
                    _battleResultModel.NextState = StageType == StageType.EventDungeon
                        ? BattleResultPopup.NextState.None
                        : BattleResultPopup.NextState.GoToMain;
                }
                else
                {
                    _battleResultModel.NextState = BattleResultPopup.NextState.NextStage;
                }
            }
            else  // Failed
            {
                _battleResultModel.NextState = BattleResultPopup.NextState.None;
            }

            _battleResultModel.ClearedCountForEachWaves[log.clearedWaveNumber] = 1;
            Widget.Find<BattleResultPopup>().Show(_battleResultModel, isMulti, newRecipes);
            yield return null;

            var characterSheet = TableSheets.Instance.CharacterSheet;
            var costumeStatSheet = TableSheets.Instance.CostumeStatSheet;
            var cp = CPHelper.GetCPV2(States.Instance.CurrentAvatarState, characterSheet, costumeStatSheet);
            var props = new Dictionary<string, Value>()
            {
                ["StageId"] = log.stageId,
                ["ClearedWave"] = log.clearedWaveNumber,
                ["CP"] = cp,
                ["FoodCount"] = foodCount,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            };
            Analyzer.Instance.Track("Unity/Stage End", props);

            var evt = new AirbridgeEvent("Stage_End");
            evt.SetValue(log.stageId);
            evt.AddCustomAttribute("cleared-wave", log.clearedWaveNumber);
            evt.AddCustomAttribute("cp", cp);
            evt.AddCustomAttribute("food-count", foodCount);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);
        }

        private IEnumerator CoSlideBg()
        {
            RunPlayer();
            while (Widget.Find<BattleResultPopup>().IsActive())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        public IEnumerator CoSpawnPlayer(Model.Player character)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoSpawnPlayer)}() enter");
#endif
            var avatarState = States.Instance.CurrentAvatarState;
            var playerCharacter = RunPlayer(false);
            playerCharacter.Set(avatarState.address, character, true);
            playerCharacter.Run();
            playerCharacter.ShowSpeech("PLAYER_INIT");
            var player = playerCharacter.gameObject;
            player.SetActive(true);

            var status = Widget.Find<Status>();
            status.UpdatePlayer(playerCharacter);
            status.Show();
            status.ShowBattleStatus();

            var battle = Widget.Find<UI.Battle>();
            var isTutorial = false;
            if (avatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(out var worldInfo))
            {
                if (worldInfo.StageClearedId < UI.Battle.RequiredStageForHeaderMenu)
                {
                    Widget.Find<HeaderMenuStatic>().Close(true);
                    isTutorial = true;
                }
                else
                {
                    Widget.Find<HeaderMenuStatic>().Show();
                }
            }
            else
            {
                Widget.Find<HeaderMenuStatic>().Close(true);
                isTutorial = true;
            }

            int apCost;
            int turnLimit;
            switch (StageType)
            {
                case StageType.HackAndSlash:
                case StageType.Mimisbrunnr:
                {
                    var sheet = TableSheets.Instance.StageSheet;
                    apCost = sheet.OrderedList
                        .FirstOrDefault(row => row.Id == stageId)?
                        .CostAP ?? 0;
                    turnLimit = sheet.TryGetValue(stageId, out var stageRow)
                        ? stageRow.TurnLimit
                        : 0;

                    break;
                }
                case StageType.EventDungeon:
                {
                    var sheet = TableSheets.Instance.EventDungeonStageSheet;
                    if (sheet is null)
                    {
                        turnLimit = 0;

                        break;
                    }

                    turnLimit = sheet.TryGetValue(stageId, out var eventDungeonStageRow)
                        ? eventDungeonStageRow.TurnLimit
                        : 0;

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            battle.Show(
                StageType,
                stageId,
                IsExitReserved,
                isTutorial);
            status.ShowBattleTimer(turnLimit);

            if (AvatarState is not null && !ActionRenderHandler.Instance.Pending)
            {
                ActionRenderHandler.Instance
                    .UpdateCurrentAvatarStateAsync(AvatarState)
                    .Forget();
            }

            yield return null;
        }

        public IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoSpawnEnemyPlayer)}() enter");
#endif
            var battle = Widget.Find<UI.Battle>();
            battle.BossStatus.Close();
            battle.EnemyPlayerStatus.Show();
            battle.EnemyPlayerStatus.SetHp(character.CurrentHP, character.HP);

            var sprite =
                SpriteHelper.GetItemIcon(character.armor?.Id ?? GameConfig.DefaultAvatarArmorId);
            battle.EnemyPlayerStatus.SetProfile(character.Level, character.NameWithHash, sprite);
            yield return StartCoroutine(spawner.CoSpawnEnemyPlayer(character, new Vector3(8f, -1.2f)));
        }

        #region Skill

        public IEnumerator CoNormalAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoNormalAttack)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoNormalAttack);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoBlowAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoBlowAttack)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoBlowAttack);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoBuffRemovalAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoBuffRemovalAttack)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoBlowAttack);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoDoubleAttackWithCombo(CharacterBase caster, int skillId, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoDoubleAttackWithCombo)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoDoubleAttackWithCombo);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoDoubleAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoDoubleAttack)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoDoubleAttack);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoAreaAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoAreaAttack)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoAreaAttack);
                character.AddAction(actionParams);

                yield return null;
            }
        }

        public IEnumerator CoHeal(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoHeal)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoHeal);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoTickDamage(CharacterBase affectedCharacter,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoTickDamage)}() enter. affectedCharacter: {affectedCharacter.Id}, skillId: {skillId}");
#endif
            var character = GetActor(affectedCharacter);
            foreach (var info in skillInfos)
            {
                var characters = GetComponentsInChildren<Actor>();
                yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
                yield return StartCoroutine(character.CoProcessDamage(info, true, true));
                yield return new WaitForSeconds(SkillDelay);
            }
        }

        public IEnumerator CoBuff(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoBuff)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoBuff);
                character.AddAction(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoSkill(ActionParams param)
        {
            yield return StartCoroutine(CoSkill(param.character, param.skillInfos, param.buffInfos, param.func));
        }

        private IEnumerator CoSkill(
            Actor character,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos,
            Func<IReadOnlyList<Skill.SkillInfo>, IEnumerator> func)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoSkill)}() enter. character: {character.Id}");
#endif
            if (!character)
                throw new ArgumentNullException(nameof(character));

            var infos = skillInfos.ToList();
            var infosFirstWaveTurn = infos.First().WaveTurn;
            var time = Time.time;

            // If a skill's wave turn is 0, it is casted regardless of turn.
            if (infosFirstWaveTurn > 0)
            {
                yield return new WaitUntil(() => Time.time - time > 5f || waveTurn == infosFirstWaveTurn);
                if (Time.time - time > 5f)
                {
                    NcDebug.LogWarning($"Time out. waveTurn: {waveTurn}, infosFirstWaveTurn: {infosFirstWaveTurn}");
                }
            }

            yield return StartCoroutine(CoBeforeSkill(character));

            yield return StartCoroutine(func(infos));

            yield return StartCoroutine(CoAfterSkill(character, buffInfos));
        }

        #endregion

        public IEnumerator CoDropBox(List<ItemBase> items)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoDropBox)}() enter.");
#endif
            var prevEnemies = GetComponentsInChildren<Character.StageMonster>();
            yield return new WaitWhile(() => prevEnemies.Any(enemy => enemy.isActiveAndEnabled));

            var isHeaderMenuShown = Widget.Find<HeaderMenuStatic>().IsActive();
            if (isHeaderMenuShown && items.Count > 0)
            {
                var player = GetPlayer();
                var position = player.transform.position;
                position.x += 1.0f;
                yield return StartCoroutine(dropItemFactory.CoCreate(items, position));
            }

            yield return null;
        }

        private IEnumerator CoBeforeSkill(Actor character)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoBeforeSkill)}() enter. character: {character.Id}");
#endif
            if (!character)
                throw new ArgumentNullException(nameof(character));

            var enemy = GetComponentsInChildren<Actor>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (!enemy || character.TargetInAttackRange(enemy))
                yield break;

            character.StartRun();
            var time = Time.time;
            yield return new WaitUntil(() =>
                Time.time - time > 2f || character.TargetInAttackRange(enemy));
            
            if (Time.time - time > 2f)
            {
                NcDebug.LogWarning($"Time out. character: {character.Id}, enemy: {enemy.Id}");
            }
        }

        private IEnumerator CoAfterSkill(Actor character, IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoAfterSkill)}() enter. character: {character.Id}");
#endif
            if (!character)
                throw new ArgumentNullException(nameof(character));

            character.UpdateActorHud();

            if (buffInfos is not null)
            {
                foreach (var buffInfo in buffInfos)
                {
                    var buffCharacter = GetActor(buffInfo.Target);
                    if (!buffCharacter)
                        throw new ArgumentNullException(nameof(buffCharacter));
                    buffCharacter.UpdateActorHud();
                }
            }

            yield return new WaitForSeconds(SkillDelay);
            var enemy = GetComponentsInChildren<Actor>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy && !character.TargetInAttackRange(enemy))
                character.StartRun();
        }

        public IEnumerator CoRemoveBuffs(CharacterBase caster)
        {
#if TEST_LOG
            Debug.Log($"[CoRemoveBuffs][{nameof(Stage)}] {nameof(CoRemoveBuffs)}() enter. caster: {caster.Id}");
#endif
            var character = GetActor(caster);
            if (!character)
            {
                yield break;
            }

            character.UpdateBuffVfx();
            character.UpdateActorHud();
            if (character.ActorHud.HpVFX != null)
            {
                character.ActorHud.HpVFX.Stop();
            }
        }

        public IEnumerator CoGetReward(List<ItemBase> rewards)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoGetReward)}() enter.");
#endif
            var characters = GetComponentsInChildren<Actor>();
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            foreach (var item in rewards)
            {
                var countableItem = new CountableItem(item, 1);
                _battleResultModel.AddReward(countableItem);
            }

            yield return null;
        }

        #region wave

        public IEnumerator CoSpawnWave(
            int waveNumber,
            int waveTurn,
            List<Enemy> enemies,
            bool hasBoss)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoSpawnWave)}() enter. {nameof(waveTurn)}({waveTurn})");
#endif
            this.waveNumber = waveNumber;
            this.waveTurn = waveTurn;
            var prevEnemies = GetComponentsInChildren<Character.StageMonster>();
            yield return new WaitWhile(() => prevEnemies.Any(enemy => enemy.isActiveAndEnabled));
            foreach (var prev in prevEnemies)
            {
                objectPool.Remove<Character.StageMonster>(prev.gameObject);
            }

            Event.OnWaveStart.Invoke(enemies.Sum(enemy => enemy.HP));

            if (_tutorialModels.FirstOrDefault(model => model.ClearedWave == this.waveNumber - 1) is { } model)
            {
                Widget.Find<Tutorial>().PlaySmallGuide(model.Id);
            }

            var characters = GetComponentsInChildren<Actor>();
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            yield return new WaitForSeconds(StageConfig.instance.spawnWaveDelay);
            Widget.Find<UI.Battle>().BossStatus.Close();
            Widget.Find<UI.Battle>().EnemyPlayerStatus.Close();
            var playerCharacter = GetPlayer();
            RunAndChasePlayer(playerCharacter);

            if (hasBoss)
            {
                yield return new WaitForSeconds(1.5f);
                playerCharacter.ShowSpeech("PLAYER_BOSS_STAGE");
                yield return new WaitForSeconds(1.5f);
                PlayBGVFX(true);
                AudioController.instance.PlayMusic(AudioController.MusicCode.Boss1);
                VFXController.instance.Create<BattleBossTitleVFX>(Vector3.zero);
                StartCoroutine(Widget.Find<Blind>().FadeIn(0.4f, "", 0.2f));
                yield return new WaitForSeconds(2.0f);
                StartCoroutine(Widget.Find<Blind>().FadeOut(0.2f));
                yield return new WaitForSeconds(2.0f);
                var boss = enemies.Last();
                Boss = boss;
                var battle = Widget.Find<UI.Battle>();
                battle.BossStatus.Show();
                battle.BossStatus.SetProfile(boss);
                playerCharacter.ShowSpeech("PLAYER_BOSS_ENCOUNTER");
            }

            yield return new WaitForEndOfFrame();

            yield return StartCoroutine(spawner.CoSetData(enemies));
        }

        public IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoWaveTurnEnd)} enter. {nameof(this.waveTurn)}({this.waveTurn}) [para : waveTurn :{waveTurn}");
#endif
            yield return new WaitWhile(() => SelectedPlayer.HasAction());
            Event.OnPlayerTurnEnd.Invoke(turnNumber);
            var characters = GetComponentsInChildren<Actor>();
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoWaveTurnEnd)} ing. {nameof(this.waveTurn)}({this.waveTurn}) [para : waveTurn :{waveTurn}");
#endif
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            this.waveTurn = waveTurn;
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoWaveTurnEnd)} exit. {nameof(this.waveTurn)}({this.waveTurn}) [para : waveTurn :{waveTurn}");
#endif
        }

        #endregion

        public IEnumerator CoGetExp(long exp)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoGetExp)}() enter. exp: {exp}");
#endif
            var characters = GetComponentsInChildren<Actor>();
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            _battleResultModel.Exp += exp;
            var player = GetPlayer();
            yield return StartCoroutine(player.CoGetExp(exp));
        }

        public IEnumerator CoDead(CharacterBase model)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoDead)}() enter. model: {model.Id}");
#endif
            var characters = GetComponentsInChildren<Actor>();
            yield return new WaitWhile(() => characters.Any(i => i.HasAction()));
            var character = GetActor(model);
            _playerPosition = SelectedPlayer.transform.position;
            character.Dead();
        }

        public IEnumerator CoCustomEvent(CharacterBase character, EventBase eventBase)
        {
            if (eventBase is Tick tick)
            {
                var affectedCharacter = GetActor(character);
                if (tick.SkillId == AuraIceShield.FrostBiteId)
                {
                    if (!character.Buffs.TryGetValue(AuraIceShield.FrostBiteId, out var frostBite))
                    {
                        yield break;
                    }

                    var source = tick.SkillInfos.First().Target;
                    var sourceCharacter = GetActor(source);
                    IEnumerator CoFrostBite(IReadOnlyList<Skill.SkillInfo> skillInfos)
                    {
                        sourceCharacter.CustomEvent(AuraIceShield.FrostBiteId);
                        yield return affectedCharacter.CoBuff(skillInfos);
                    }

                    var tickSkillInfo = new Skill.SkillInfo(
                        affectedCharacter.Id,
                        !affectedCharacter.IsAlive,
                        0,
                        0,
                        false,
                        SkillCategory.Debuff,
                        waveTurn,
                        target: character,
                        buff: frostBite
                    );
                    affectedCharacter.AddAction(
                        new ActionParams(affectedCharacter,
                                        ArraySegment<Skill.SkillInfo>.Empty.Append(tickSkillInfo),
                                         tick.BuffInfos,
                                        CoFrostBite
                        ));
                }
                // This Tick from 'Stun'
                else if (tick.SkillId == 0)
                {
                    IEnumerator StunTick(IEnumerable<Skill.SkillInfo> _)
                    {
                        affectedCharacter.Animator.Hit();
                        affectedCharacter.AddHitColor();
                        yield return new WaitForSeconds(SkillDelay);
                    }

                    var tickSkillInfo = new Skill.SkillInfo(affectedCharacter.Id,
                                                            !affectedCharacter.IsAlive,
                                                            0,
                                                            0,
                                                            false,
                                                            SkillCategory.TickDamage,
                                                            waveTurn,
                                                            target: character
                    );
                    affectedCharacter.AddAction(
                        new ActionParams(affectedCharacter,
                                         tick.SkillInfos.Append(tickSkillInfo),
                                         tick.BuffInfos,
                                         StunTick
                        ));

                    yield return null;
                }
                // This Tick from 'Vampiric'
                else if (TableSheets.Instance.ActionBuffSheet.TryGetValue(tick.SkillId,
                             out var row) && row.ActionBuffType == ActionBuffType.Vampiric)
                {
                    if (affectedCharacter)
                    {
                        yield return new WaitWhile(() => affectedCharacter.HasAction());
                        yield return affectedCharacter.CoHealWithoutAnimation(tick.SkillInfos.ToList());
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }
        }

        public Player GetPlayer()
        {
            if (SelectedPlayer &&
                SelectedPlayer.gameObject.activeSelf)
            {
                return SelectedPlayer;
            }

            if (SelectedPlayer)
            {
                objectPool.Remove<Model.Player>(SelectedPlayer.gameObject);
            }

            var go = PlayerFactory.Create(States.Instance.CurrentAvatarState);
            SelectedPlayer = go.GetComponent<Player>();

            if (SelectedPlayer is null)
            {
                throw new NotFoundComponentException<Player>();
            }

            return SelectedPlayer;
        }

        public Player GetPlayer(Vector3 position)
        {
            var player = GetPlayer();
            player.transform.position = position;
            return player;
        }

        private Player RunPlayer(bool chasePlayer = true)
        {
            _stageRunningPlayer = GetPlayer();
            _stageRunningPlayer.Set(States.Instance.CurrentAvatarState);
            var playerTransform = _stageRunningPlayer.transform;
            Vector2 position = playerTransform.position;
            position.y = StageStartPosition;
            playerTransform.position = position;
            if (chasePlayer)
                RunAndChasePlayer(_stageRunningPlayer);
            else
                _stageRunningPlayer.StartRun();
            return _stageRunningPlayer;
        }

        public Player RunPlayer(Vector2 position, bool chasePlayer = true)
        {
            var player = GetPlayer(position);
            if (chasePlayer)
                RunAndChasePlayer(player);
            else
                player.StartRun();
            return player;
        }

        public Player RunPlayerForNextStage()
        {
            if (SelectedPlayer != null)
            {
                _playerPosition = SelectedPlayer.transform.position;
            }

            var player = GetPlayer(_playerPosition);
            RunAndChasePlayer(player);
            return player;
        }

        /// <summary>
        /// 게임 캐릭터를 갖고 올 때 사용함.
        /// 갖고 올 때 매번 모델을 할당해주고 있음.
        /// 모델을 매번 할당하지 않고, 모델이 변경되는 로직 마다 바꿔주게 하는 것이 좋겠음. 물론 연출도 그때에 맞춰서 해주는 식.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Actor GetActor(CharacterBase caster)
        {
            if (caster is null)
                throw new ArgumentNullException(nameof(caster));

            var characters = GetComponentsInChildren<Actor>()
                .Where(c => c.Id == caster.Id);
            var character = characters?.FirstOrDefault();

            if (characters is not null && characters.Any())
            {
                var ch = characters.First();

                if (ch is null)
                {
                    NcDebug.Log("player is null");
                }

                if (ch is Player)
                {
                    character = characters.FirstOrDefault(x =>
                        x.GetComponent<SortingGroup>().sortingLayerName == "Character");
                }
            }

            character?.Set(caster);

            return character;
        }

        private void PlayBGVFX(bool isBoss)
        {
            if (isBoss && bosswaveBGVFX)
            {
                if (defaultBGVFX)
                    defaultBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                bosswaveBGVFX.Play(true);
            }
            else
            {
                if (bosswaveBGVFX)
                    bosswaveBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (defaultBGVFX && !defaultBGVFX.isPlaying)
                    defaultBGVFX.Play(true);
            }
        }

        private IEnumerator CoUnlockMenu()
        {
            var menuNames = new List<string>();
#if !RUN_ON_MOBILE
            if (stageId == LiveAsset.GameConfig.RequiredStage.Shop)
            {
                menuNames.Add("UI_MAIN_MENU_SHOP");
            }
#endif
            if (stageId == LiveAsset.GameConfig.RequiredStage.Arena)
            {
                menuNames.Add("UI_MAIN_MENU_RANKING");
            }

            if (stageId == LiveAsset.GameConfig.RequiredStage.WorldBoss)
            {
                menuNames.Add("UI_WORLD_BOSS");
            }

            var celebratesPopup = Widget.Find<CelebratesPopup>();
            foreach (var menuName in menuNames)
            {
                celebratesPopup.Show(menuName);
                yield return new WaitWhile(() => celebratesPopup.IsActive());
            }
        }

        private static void RunAndChasePlayer(Actor player)
        {
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
        }

        public IEnumerator CoShatterStrike(CharacterBase caster, int skillId, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
#if TEST_LOG
            Debug.Log($"[{nameof(Stage)}] {nameof(CoShatterStrike)}() enter. caster: {caster.Id}, skillId: {skillId}");
#endif
            var character = GetActor(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoShatterStrike);
                character.AddAction(actionParams);
                yield return null;
            }
        }
    }
}
