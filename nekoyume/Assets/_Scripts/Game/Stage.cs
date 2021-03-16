// #define TEST_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.BlockChain;
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
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Spine.Unity;
using UnityEngine;
using TentuPlay.Api;
using UniRx;
using mixpanel;
using Nekoyume.Game.Character;
using Nekoyume.L10n;
using UnityEngine.Rendering;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour, IStage
    {
        public const float StageStartPosition = -1.2f;
        private const float SkillDelay = 0.1f;
        public ObjectPool objectPool;
        public NPCFactory npcFactory;
        public DropItemFactory dropItemFactory;

        public MonsterSpawner spawner;

        public GameObject background;

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
        public Character.Player selectedPlayer;
        public readonly Vector2 questPreparationPosition = new Vector2(2.45f, -0.35f);
        public readonly Vector2 roomPosition = new Vector2(-2.808f, -1.519f);
        public bool repeatStage;
        public bool isExitReserved;
        public string zone;
        public Animator roomAnimator { get; private set; }

        private Camera _camera;
        private BattleLog _battleLog;
        private BattleResult.Model _battleResultModel;
        private bool _rankingBattle;
        private Coroutine _battleCoroutine;

        public List<GameObject> ReleaseWhiteList { get; private set; } = new List<GameObject>();
        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public TutorialController TutorialController { get; private set; }
        public bool IsInStage { get; set; }
        public Model.Enemy Boss { get; private set; }
        public AvatarState AvatarState { get; set; }

        public Vector3 SelectPositionBegin(int index) =>
            new Vector3(-2.15f + index * 2.22f, -1.79f, 0.0f);

        public Vector3 SelectPositionEnd(int index) =>
            new Vector3(-2.15f + index * 2.22f, -0.25f, 0.0f);

        public bool showLoadingScreen;

        private Character.Player _stageRunningPlayer = null;
        private Vector3 _playerPosition;

        private List<int> prevFood;

        private Coroutine _positionCheckCoroutine = null;

        #region Events

        private readonly ISubject<Stage> _onEnterToStageEnd = new Subject<Stage>();
        public IObservable<Stage> onEnterToStageEnd => _onEnterToStageEnd;

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
            Event.OnStageStart.AddListener(OnStageStart);
            Event.OnRankingBattleStart.AddListener(OnRankingBattleStart);
        }

        public void Initialize()
        {
            objectPool.Initialize();
            dropItemFactory.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
            TutorialController = new TutorialController(MainCanvas.instance.Widgets);
        }

        private void OnStageStart(BattleLog log)
        {
            _rankingBattle = false;
            if (_battleLog is null)
            {
                if (!(_battleCoroutine is null))
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
                Debug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private void OnRankingBattleStart(BattleLog log)
        {
            _rankingBattle = true;
            if (_battleLog is null)
            {
                if (!(_battleCoroutine is null))
                {
                    StopCoroutine(_battleCoroutine);
                    _battleCoroutine = null;
                    objectPool.ReleaseAll();
                }
                _battleLog = log;
                PlayRankingBattle(_battleLog);
            }
            else
            {
                Debug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private void OnNestEnter()
        {
            gameObject.AddComponent<NestEntering>();
        }

        private void OnLoginDetail(int index)
        {
            DOTween.KillAll();
            var players = Widget.Find<Login>().players;
            for (var i = 0; i < players.Count; ++i)
            {
                var player = players[i];
                var playerObject = player.gameObject;
                var anim = player.Animator;
                if (index == i)
                {
                    var moveTo = new Vector3(-0.05f, -0.5f);
                    playerObject.transform.DOScale(1.1f, 2.0f).SetDelay(0.2f);
                    playerObject.transform.DOMove(moveTo, 1.3f).SetDelay(0.2f);
                    var seqPos = new Vector3(
                        moveTo.x,
                        moveTo.y - UnityEngine.Random.Range(0.05f, 0.1f),
                        0.0f);
                    var seq = DOTween.Sequence();
                    seq.Append(playerObject.transform.DOMove(
                        seqPos,
                        UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Append(playerObject.transform.DOMove(
                        moveTo,
                        UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Play().SetDelay(2.6f).SetLoops(-1);
                    if (!ReferenceEquals(anim, null) && !anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        var skeleton =
                            anim.Target.GetComponentInChildren<SkeletonAnimation>().skeleton;
                        skeleton.A = 0.0f;
                        DOTween.To(() => skeleton.A, x => skeleton.A = x, 1.0f, 1.0f);
                        player.SpineController.Appear();
                    }

                    selectedPlayer = players[i];
                }
                else
                {
                    playerObject.transform.DOScale(0.9f, 1.0f);
                    playerObject.transform.DOMoveY(-3.6f, 2.0f);

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
            showLoadingScreen = showScreen;
            gameObject.AddComponent<RoomEntering>();
            IsInStage = false;
        }

        // todo: 배경 캐싱.
        public void LoadBackground(string prefabName, float fadeTime = 0.0f)
        {
            if (background)
            {
                if (background.name.Equals(prefabName))
                    return;

                if (fadeTime > 0.0f)
                {
                    var sprites = background.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in sprites)
                    {
                        sprite.sortingOrder += 1;
                        sprite.DOFade(0.0f, fadeTime);
                    }

                    var particles = background.GetComponentsInChildren<ParticleSystem>();
                    foreach (var particle in particles)
                    {
                        particle.Stop();
                    }
                }

                Destroy(background, fadeTime);
                background = null;
            }

            var path = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(path);
            if (!prefab)
                throw new FailedToLoadResourceException<GameObject>(path);

            background = Instantiate(prefab, transform);
            background.name = prefabName;
            if (prefabName == "room")
                roomAnimator = background.GetComponent<Animator>();

            foreach (Transform child in background.transform)
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
        }

        public void PlayStage(BattleLog log)
        {
            if (log?.Count > 0)
            {
                _battleCoroutine = StartCoroutine(CoPlayStage(log));
            }
        }

        private void PlayRankingBattle(BattleLog log)
        {
            if (log?.Count > 0)
            {
                _battleCoroutine = StartCoroutine(CoPlayRankingBattle(log));
            }
        }

        private IEnumerator CoPlayStage(BattleLog log)
        {
            AvatarState avatarState = States.Instance.CurrentAvatarState;
            string stage_slug = "HackAndSlash" + "_" + log.worldId + "_" + log.stageId;

            //[TentuPlay] PlayStage 시작 기록
            OnCharacterStageStart("HackAndSlash", stage_slug, log.worldId + "_" + log.stageId);

            //[TentuPlay] 전투 입장 시 사용하는 Action Point
            new TPStashEvent().CharacterCurrencyUse(
                player_uuid: Game.instance.Agent.Address.ToHex(),
                character_uuid: avatarState.address.ToHex().Substring(0, 4),
                currency_slug: "action_point",
                currency_quantity: 5,
                currency_total_quantity: (float)avatarState.actionPoint,
                reference_entity: entity.Stages,
                reference_category_slug: "HackAndSlash",
                reference_slug: "HackAndSlash" + "_" + log.worldId + "_" + log.stageId
                );

            //[TentuPlay] PlayStage 장비 착용 기록
            OnCharacterEquipmentPlay("HackAndSlash", stage_slug);

            //[TentuPlay] PlayStage 코스튬 착용 기록
            OnCharacterCostumePlay("HackAndSlash", stage_slug);

            prevFood = avatarState.inventory.Items.Select(i => i.item).OfType<Consumable>()
           .Where(s => s.ItemSubType == ItemSubType.Food)
           .Select(r => r.Id).ToList();

            IsInStage = true;
            yield return StartCoroutine(CoStageEnter(log));
            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoStageEnd(log));
            ClearBattle();
        }

        private IEnumerator CoPlayRankingBattle(BattleLog log)
        {
            //[TentuPlay] RankingBattle 시작 기록
            OnCharacterStageStart("RankingBattle", "RankingBattle", null);

            //[TentuPlay] RankingBattle 장비 착용 기록
            OnCharacterEquipmentPlay("RankingBattle", "RankingBattle");

            //[TentuPlay] RankingBattle 코스튬 착용 기록
            OnCharacterCostumePlay("RankingBattle", "RankingBattle");

            IsInStage = true;
            yield return StartCoroutine(CoRankingBattleEnter(log));
            Widget.Find<ArenaBattleLoadingScreen>().Close();
            _positionCheckCoroutine = StartCoroutine(CheckPosition(log));
            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }
            StopCoroutine(_positionCheckCoroutine);
            _positionCheckCoroutine = null;
            yield return StartCoroutine(CoRankingBattleEnd(log));
            ClearBattle();
        }

        private IEnumerator CheckPosition(BattleLog log)
        {
            var player = GetPlayer();
            while (player.isActiveAndEnabled)
            {
                if (player.transform.localPosition.x >= 16f)
                {
                    _positionCheckCoroutine = null;

                    if (log.FirstOrDefault(e => e is GetReward) is GetReward getReward)
                    {
                        var rewards = getReward.Rewards;
                        foreach (var item in rewards)
                        {
                            var countableItem = new CountableItem(item, 1);
                            _battleResultModel.AddReward(countableItem);
                        }
                    }

                    yield return StartCoroutine(CoRankingBattleEnd(log, true));
                    ClearBattle();
                    StopAllCoroutines();
                }

                yield return new WaitForSeconds(1f);
            }
        }

        public void ClearBattle()
        {
            _battleLog = null;
            if (!(_battleCoroutine is null))
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }
        }

        private static IEnumerator CoGuidedQuest(int stageIdToClear)
        {
            var done = false;
            var battle = Widget.Find<UI.Battle>();
            battle.ClearStage(stageIdToClear, cleared => done = true);
            yield return new WaitUntil(() => done);
        }

        private static IEnumerator CoUnlockRecipe(int stageIdToFirstClear)
        {
            var questResult = Widget.Find<CelebratesPopup>();
            var rows = Game.instance.TableSheets.EquipmentItemRecipeSheet.OrderedList
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
            worldId = log.worldId;
            stageId = log.stageId;
            waveCount = log.waveCount;
            newlyClearedStage = log.newlyCleared;
            if (!Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var data))
                yield break;

            _battleResultModel = new BattleResult.Model();

            zone = data.Background;
            LoadBackground(zone, 3.0f);
            PlayBGVFX(false);
            RunPlayer();
            ReleaseWhiteList.Clear();
            ReleaseWhiteList.Add(_stageRunningPlayer.gameObject);

            var battle = Widget.Find<UI.Battle>();
            Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageData);
            battle.StageProgressBar.Initialize(true);
            Widget.Find<BattleResult>().StageProgressBar.Initialize(false);
            var title = Widget.Find<StageTitle>();
            title.Show(stageId);

            yield return new WaitForSeconds(StageConfig.instance.stageEnterDelay);

            yield return StartCoroutine(title.CoClose());

            AudioController.instance.PlayMusic(data.BGM);
        }

        private IEnumerator CoRankingBattleEnter(BattleLog log)
        {
            waveCount = log.waveCount;
            waveTurn = 1;
            stageId = log.stageId;
#if TEST_LOG
            Debug.LogWarning($"{nameof(waveTurn)}: {waveTurn} / {nameof(CoRankingBattleEnter)}");
#endif
            if (!Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var data))
                yield break;

            _battleResultModel = new BattleResult.Model();

            zone = data.Background;
            LoadBackground(zone, 3.0f);
            PlayBGVFX(false);
            RunPlayer(new Vector2(-15f, -1.2f), false);

            yield return new WaitForSeconds(2.0f);

            AudioController.instance.PlayMusic(AudioController.MusicCode.PVPBattle);
        }

        private IEnumerator CoStageEnd(BattleLog log)
        {
            _onEnterToStageEnd.OnNext(this);
            _battleResultModel.ClearedWaveNumber = log.clearedWaveNumber;
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            yield return new WaitForSeconds(1f);
            Boss = null;
            Widget.Find<UI.Battle>().BossStatus.Close();
            var isClear = log.IsClear;
            if (isClear)
            {
                yield return StartCoroutine(CoGuidedQuest(log.stageId));
                yield return new WaitForSeconds(1f);
            }
            else
            {
                var enemies = GetComponentsInChildren<Character.Enemy>();
                if (enemies.Any())
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy.isActiveAndEnabled)
                        {
                            enemy.Animator.Win();
                        }
                    }
                    yield return new WaitForSeconds(1f);
                }
            }

            Widget.Find<UI.Battle>().Close();

            if (newlyClearedStage)
            {
                yield return StartCoroutine(CoUnlockMenu());
                yield return new WaitForSeconds(0.75f);
                yield return StartCoroutine(CoUnlockRecipe(stageId));
                yield return new WaitForSeconds(1f);
            }

            if (log.result == BattleLog.Result.Win)
            {
                _stageRunningPlayer.DisableHUD();
                _stageRunningPlayer.Animator.Win(log.clearedWaveNumber);
                _stageRunningPlayer.ShowSpeech("PLAYER_WIN");
                yield return new WaitForSeconds(2.2f);
                objectPool.ReleaseExcept(ReleaseWhiteList);
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
                objectPool.ReleaseExcept(ReleaseWhiteList);
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatarState = new AvatarState(
                (Bencodex.Types.Dictionary) Game.instance.Agent.GetState(avatarAddress));
            _battleResultModel.ActionPoint = avatarState.actionPoint;
            _battleResultModel.State = log.result;
            Game.instance.TableSheets.WorldSheet.TryGetValue(log.worldId, out var world);
            _battleResultModel.WorldName = world?.GetLocalizedName();
            _battleResultModel.StageID = log.stageId;
            avatarState.worldInformation.TryGetLastClearedStageId(out var lasStageId);
            _battleResultModel.LastClearedStageId = lasStageId;
            _battleResultModel.IsClear = log.IsClear;

            if (isExitReserved)
            {
                _battleResultModel.ActionPointNotEnough = false;
                _battleResultModel.ShouldExit = true;
                _battleResultModel.ShouldRepeat = false;
            }
            else if (repeatStage || !isClear)
            {
                var apNotEnough = true;
                if (Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
                {
                    apNotEnough = avatarState.actionPoint < stageRow.CostAP;
                }

                _battleResultModel.ActionPointNotEnough = apNotEnough;
                _battleResultModel.ShouldExit = apNotEnough;
                _battleResultModel.ShouldRepeat = !apNotEnough;
            }
            else
            {
                var apNotEnough = true;
                if (Game.instance.TableSheets.StageSheet.TryGetValue(stageId + 1, out var stageRow))
                {
                    apNotEnough = avatarState.actionPoint < stageRow.CostAP;
                }

                _battleResultModel.ActionPointNotEnough = apNotEnough;
                _battleResultModel.ShouldExit = apNotEnough;
                _battleResultModel.ShouldRepeat = false;
            }

            if (!_battleResultModel.ShouldExit &&
                !_battleResultModel.ShouldRepeat)
            {
                if (Game.instance.TableSheets.WorldSheet.TryGetValue(worldId, out var worldRow))
                {
                    if (stageId == worldRow.StageEnd)
                    {
                        _battleResultModel.ShouldExit = true;
                    }
                }
                else
                {
                    _battleResultModel.ShouldExit = true;
                }
            }

            ActionRenderHandler.Instance.Pending = false;
            Widget.Find<BattleResult>().Show(_battleResultModel);
            yield return null;

            //[TentuPlay] 전투 입장 시 사용한 아이템 - 소모품
            string stage_slug = "HackAndSlash" + "_" + log.worldId + "_" + log.stageId;
            OnCharacterConsumablePlay("HackAndSlash", stage_slug);

            //[TentuPlay] PlayStage 끝 기록
            OnCharacterStageEnd(log, "HackAndSlash", stage_slug, log.clearedWaveNumber);

            var props = new Value
            {
                ["StageId"] = log.stageId
            };
            Mixpanel.Track("Unity/Stage End", props);
        }

        private IEnumerator CoSlideBg()
        {
            RunPlayer();
            while (Widget.Find<BattleResult>().IsActive())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator CoRankingBattleEnd(BattleLog log, bool forceQuit = false)
        {
            _onEnterToStageEnd.OnNext(this);
            var characters = GetComponentsInChildren<Character.CharacterBase>();

            if (!forceQuit)
            {
                yield return new WaitWhile(() =>
                    characters.Any(i => i.actions.Any()));
            }

            Boss = null;
            var playerCharacter = log.result == BattleLog.Result.Win
                ? GetPlayer()
                : GetComponentInChildren<Character.EnemyPlayer>();

            yield return new WaitForSeconds(0.75f);
            playerCharacter.Animator.Win();
            playerCharacter.ShowSpeech("PLAYER_WIN");
            Widget.Find<UI.Battle>().Close();
            Widget.Find<Status>().Close();

            ActionRenderHandler.Instance.Pending = false;
            Widget.Find<RankingBattleResult>().Show(log, _battleResultModel.Rewards);
            yield return null;

            //[TentuPlay] RankingBattle 끝 기록
            OnCharacterStageEnd(log, "RankingBattle", "RankingBattle", log.diffScore);
        }

        public IEnumerator CoSpawnPlayer(Model.Player character)
        {
            var playerCharacter = RunPlayer(false);
            playerCharacter.Set(character, true);
            playerCharacter.ShowSpeech("PLAYER_INIT");
            var player = playerCharacter.gameObject;
            player.SetActive(true);

            var status = Widget.Find<Status>();
            status.UpdatePlayer(playerCharacter);
            status.Show();
            status.ShowBattleStatus();

            var battle = Widget.Find<UI.Battle>();
            if (_rankingBattle)
            {
                battle.ShowInArena();
            }
            else
            {
                battle.Show(stageId, repeatStage, isExitReserved);
                var stageSheet = Game.instance.TableSheets.StageSheet;
                if (stageSheet.TryGetValue(stageId, out var row))
                {
                    status.ShowBattleTimer(row.TurnLimit);
                }
            }

            if (!(AvatarState is null) && !ActionRenderHandler.Instance.Pending)
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(AvatarState);
            }

            yield return null;
        }

        public IEnumerator CoSpawnEnemyPlayer(Model.EnemyPlayer character)
        {
            var battle = Widget.Find<UI.Battle>();
            battle.BossStatus.Close();
            battle.EnemyPlayerStatus.Show();
            battle.EnemyPlayerStatus.SetHp(character.CurrentHP, character.HP);

            var sprite =
                SpriteHelper.GetItemIcon(character.armor?.Id ?? GameConfig.DefaultAvatarArmorId);
            battle.EnemyPlayerStatus.SetProfile(character.Level, character.NameWithHash, sprite);
            yield return StartCoroutine(spawner.CoSetData(character, new Vector3(8f, -1.2f)));
        }

        #region Skill

        public IEnumerator CoNormalAttack(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoNormalAttack);
                character.actions.Add(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoBlowAttack(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoBlowAttack);
                character.actions.Add(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoDoubleAttack(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoDoubleAttack);
                character.actions.Add(actionParams);

                yield return null;
            }
        }

        public IEnumerator CoAreaAttack(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoAreaAttack);
                character.actions.Add(actionParams);

                yield return null;
            }
        }

        public IEnumerator CoHeal(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoHeal);
                character.actions.Add(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoBuff(
            Model.CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                var actionParams = new ActionParams(character, skillInfos, buffInfos, character.CoBuff);
                character.actions.Add(actionParams);
                yield return null;
            }
        }

        public IEnumerator CoSkill(ActionParams param)
        {
            yield return StartCoroutine(CoSkill(param.character, param.skillInfos, param.buffInfos, param.func));
        }

        private IEnumerator CoSkill(
            Character.CharacterBase character,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos,
            Func<IReadOnlyList<Skill.SkillInfo>, IEnumerator> func)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            var infos = skillInfos.ToList();
            var infosFirstWaveTurn = infos.First().WaveTurn;
#if TEST_LOG
            Debug.LogWarning(
                $"{nameof(waveTurn)}: {waveTurn} / {nameof(infosFirstWaveTurn)}: {infosFirstWaveTurn} / {nameof(CoSkill)}");
#endif
            var time = Time.time;
            yield return new WaitUntil(() => Time.time - time > 5f ||  waveTurn == infosFirstWaveTurn);
            yield return StartCoroutine(CoBeforeSkill(character));

            yield return StartCoroutine(func(infos));

            yield return StartCoroutine(CoAfterSkill(character, buffInfos));
        }

        #endregion

        public IEnumerator CoDropBox(List<ItemBase> items)
        {
            var prevEnemies = GetComponentsInChildren<Character.Enemy>();
            yield return new WaitWhile(() => prevEnemies.Any(enemy => enemy.isActiveAndEnabled));
            if (items.Count > 0)
            {
                var player = GetPlayer();
                var position = player.transform.position;
                position.x += 1.0f;
                yield return StartCoroutine(dropItemFactory.CoCreate(items, position));
            }

            yield return null;
        }

        private IEnumerator CoBeforeSkill(Character.CharacterBase character)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (!enemy || character.TargetInAttackRange(enemy))
                yield break;

            character.StartRun();
            var time = Time.time;
            yield return new WaitUntil(() =>
                Time.time - time > 2f || character.TargetInAttackRange(enemy));
        }

        private IEnumerator CoAfterSkill(
            Character.CharacterBase character,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            if (!character)
                throw new ArgumentNullException(nameof(character));

            character.UpdateHpBar();

            if (!(buffInfos is null))
            {
                foreach (var buffInfo in buffInfos)
                {
                    var buffCharacter = GetCharacter(buffInfo.Target);
                    if (!buffCharacter)
                        throw new ArgumentNullException(nameof(buffCharacter));
                    buffCharacter.UpdateHpBar();
                }
            }

            yield return new WaitForSeconds(SkillDelay);
            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.TargetTag) && c.IsAlive)
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy && !character.TargetInAttackRange(enemy))
                character.StartRun();
        }

        public IEnumerator CoRemoveBuffs(Model.CharacterBase caster)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.UpdateHpBar();
                if (character.HPBar.HpVFX != null)
                {
                    character.HPBar.HpVFX.Stop();
                }
            }

            yield break;
        }

        public IEnumerator CoGetReward(List<ItemBase> rewards)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
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
            List<Model.Enemy> enemies,
            bool hasBoss)
        {
            this.waveNumber = waveNumber;
            this.waveTurn = waveTurn;
#if TEST_LOG
            Debug.LogWarning($"{nameof(waveTurn)}: {waveTurn} / {nameof(CoSpawnWave)}");
#endif
            var prevEnemies = GetComponentsInChildren<Character.Enemy>();
            yield return new WaitWhile(() => prevEnemies.Any(enemy => enemy.isActiveAndEnabled));
            foreach (var prev in prevEnemies)
            {
                objectPool.Remove<Character.Enemy>(prev.gameObject);
            }

            Event.OnWaveStart.Invoke(enemies.Sum(enemy => enemy.HP));

            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
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
                var sprite = SpriteHelper.GetCharacterIcon(boss.RowData.Id);
                var battle = Widget.Find<UI.Battle>();
                battle.BossStatus.Show();
                battle.BossStatus.SetHp(boss.HP, boss.HP);
                battle.BossStatus.SetProfile(
                    boss.Level,
                    L10nManager.LocalizeCharacterName(boss.RowData.Id),
                    sprite);
                playerCharacter.ShowSpeech("PLAYER_BOSS_ENCOUNTER");
            }

            yield return new WaitForEndOfFrame();

            yield return StartCoroutine(spawner.CoSetData(enemies));
        }

        public IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn)
        {
#if TEST_LOG
            Debug.LogWarning($"{nameof(this.waveTurn)}: {this.waveTurn} / {nameof(CoWaveTurnEnd)} Enter");
#endif
            yield return new WaitWhile(() => selectedPlayer.actions.Any());
            Event.OnPlayerTurnEnd.Invoke(turnNumber);
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            this.waveTurn = waveTurn;
#if TEST_LOG
            Debug.LogWarning($"{nameof(this.waveTurn)}: {this.waveTurn} / {nameof(CoWaveTurnEnd)} Exit");
#endif
        }

        #endregion

        public IEnumerator CoGetExp(long exp)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            _battleResultModel.Exp += exp;
            var player = GetPlayer();
            yield return StartCoroutine(player.CoGetExp(exp));
        }

        public IEnumerator CoDead(Model.CharacterBase model)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            var character = GetCharacter(model);
            _playerPosition = selectedPlayer.transform.position;
            character.Dead();
        }

        public Character.Player GetPlayer(bool forceCreate = false)
        {
            if (!forceCreate &&
                selectedPlayer &&
                selectedPlayer.gameObject.activeSelf)
            {
                return selectedPlayer;
            }

            if (selectedPlayer)
            {
                objectPool.Remove<Model.Player>(selectedPlayer.gameObject);
            }

            var go = PlayerFactory.Create(States.Instance.CurrentAvatarState);
            selectedPlayer = go.GetComponent<Character.Player>();

            if (selectedPlayer is null)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            return selectedPlayer;
        }

        public Character.Player GetPlayer(Vector2 position, bool forceCreate = false)
        {
            var player = GetPlayer(forceCreate);
            player.transform.position = position;
            return player;
        }

        private Character.Player RunPlayer(bool chasePlayer = true)
        {
            _stageRunningPlayer = GetPlayer();
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

        public Character.Player RunPlayer(Vector2 position, bool chasePlayer = true)
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
            if (selectedPlayer != null)
            {
                _playerPosition = selectedPlayer.transform.position;
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
        public Character.CharacterBase GetCharacter(Model.CharacterBase caster)
        {
            if (caster is null)
                throw new ArgumentNullException(nameof(caster));

            var characters = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.Id == caster.Id);
            var character = characters?.FirstOrDefault();

            if (!(characters is null))
            {
                var ch = characters.First();

                if (ch is null)
                {
                    Debug.Log("player is null");
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
            if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuCombination)
            {
                menuNames.Add(nameof(Combination));
            }

            if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuShop)
            {
                menuNames.Add(nameof(UI.Shop));
            }

            if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuRankingBoard)
            {
                menuNames.Add(nameof(RankingBoard));
            }

            if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuMimisbrunnr)
            {
                menuNames.Add(nameof(MimisbrunnrPreparation));
            }

            var celebratesPopup = Widget.Find<CelebratesPopup>();
            foreach (var menuName in menuNames)
            {
                celebratesPopup.Show(menuName);
                yield return new WaitWhile(() => celebratesPopup.IsActive());
            }
        }

        private static void RunAndChasePlayer(Character.Player player)
        {
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
        }

        private void OnCharacterStageStart(string stageCategorySlug, string stageSlug, string stageLevel)
        {
            try
            {
                TPStashEvent myStashEvent = new TPStashEvent();
                AvatarState avatarState = States.Instance.CurrentAvatarState;

                myStashEvent.CharacterStage(
                    player_uuid: Game.instance.Agent.Address.ToHex(),
                    character_uuid: avatarState.address.ToHex().Substring(0, 4),
                    stage_category_slug: stageCategorySlug,
                    stage_slug: stageSlug,
                    stage_status: stageStatus.Start,
                    stage_level: stageLevel,
                    is_autocombat_committed: isAutocombat.AutocombatOn
                );
            }
            catch
            {
            }
        }

        private void OnCharacterEquipmentPlay(string stageCategorySlug, string stageSlug)
        {
            try
            {
                TPStashEvent myStashEvent = new TPStashEvent();
                AvatarState avatarState = States.Instance.CurrentAvatarState;

                List<int> allEquippedEquipmentsId = new List<int>();
                List<int> weapon = avatarState.inventory.Items.Select(i => i.item).OfType<Weapon>().Where(e => e.equipped).Select(r => r.Id).ToList();
                List<int> armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().Where(e => e.equipped).Select(r => r.Id).ToList();
                List<int> belt = avatarState.inventory.Items.Select(i => i.item).OfType<Belt>().Where(e => e.equipped).Select(r => r.Id).ToList();
                List<int> necklace = avatarState.inventory.Items.Select(i => i.item).OfType<Necklace>().Where(e => e.equipped).Select(r => r.Id).ToList();
                List<int> ring = avatarState.inventory.Items.Select(i => i.item).OfType<Ring>().Where(e => e.equipped).Select(r => r.Id).ToList();
                allEquippedEquipmentsId.AddRange(weapon);
                allEquippedEquipmentsId.AddRange(armor);
                allEquippedEquipmentsId.AddRange(belt);
                allEquippedEquipmentsId.AddRange(necklace);
                allEquippedEquipmentsId.AddRange(ring);
                foreach (int id in allEquippedEquipmentsId)
                {
                    myStashEvent.CharacterItemPlay(
                        player_uuid: Game.instance.Agent.Address.ToHex(),
                        character_uuid: avatarState.address.ToHex().Substring(0, 4),
                        item_category: itemCategory.Equipment,
                        item_slug: id.ToString(),
                        reference_entity: entity.Stages,
                        reference_category_slug: stageCategorySlug,
                        reference_slug: stageSlug
                        );
                }
            }
            catch
            {
            }
        }

        private void OnCharacterCostumePlay(string stageCategorySlug, string stageSlug)
        {
            try
            {
                TPStashEvent myStashEvent = new TPStashEvent();
                AvatarState avatarState = States.Instance.CurrentAvatarState;

                List<int> allEquippedCostumeIds = avatarState.inventory.Items.Select(i => i.item).OfType<Costume>().Where(e => e.equipped)
                .Where(s => s.ItemSubType == ItemSubType.FullCostume || s.ItemSubType == ItemSubType.Title)
                .Select(r => r.Id).ToList();
                foreach (int id in allEquippedCostumeIds)
                {
                    myStashEvent.CharacterItemPlay(
                        player_uuid: Game.instance.Agent.Address.ToHex(),
                        character_uuid: avatarState.address.ToHex().Substring(0, 4),
                        item_category: itemCategory.Cosmetics,
                        item_slug: id.ToString(),
                        reference_entity: entity.Stages,
                        reference_category_slug: stageCategorySlug,
                        reference_slug: stageSlug
                        );
                }
            }
            catch
            {
            }
        }

        private void OnCharacterConsumablePlay(string stageCategorySlug, string stageSlug)
        {
            try
            {
                TPStashEvent myStashEvent = new TPStashEvent();
                AvatarState avatarState = States.Instance.CurrentAvatarState;

                List<int> CurrentFood = avatarState.inventory.Items.Select(i => i.item).OfType<Consumable>()
                    .Where(s => s.ItemSubType == ItemSubType.Food)
                    .Select(r => r.Id).ToList();
                foreach (int foodId in CurrentFood)
                {
                    prevFood.Remove(foodId);
                }
                foreach (int foodId in prevFood)
                {
                    myStashEvent.CharacterItemPlay(
                        player_uuid: Game.instance.Agent.Address.ToHex(),
                        character_uuid: avatarState.address.ToHex().Substring(0, 4),
                        item_category: itemCategory.Consumable,
                        item_slug: foodId.ToString(),
                        reference_entity: entity.Stages,
                        reference_category_slug: stageCategorySlug,
                        reference_slug: stageSlug
                        );
                }
            }
            catch
            {
            }
        }

        private void OnCharacterStageEnd(BattleLog log, string stageCategorySlug, string stageSlug, int stageScore)
        {
            try
            {
                stageStatus stageStatus = stageStatus.Unknown;
                switch (log.result)
                {
                    case BattleLog.Result.Win:
                        stageStatus = stageStatus.Win;
                        break;
                    case BattleLog.Result.Lose:
                        stageStatus = stageStatus.Lose;
                        break;
                    case BattleLog.Result.TimeOver:
                        stageStatus = stageStatus.Timeout;
                        break;
                }

                new TPStashEvent().CharacterStage(
                    player_uuid: Game.instance.Agent.Address.ToHex(),
                    character_uuid: States.Instance.CurrentAvatarState.address.ToHex().Substring(0, 4),
                    stage_category_slug: stageCategorySlug,
                    stage_slug: stageSlug,
                    stage_status: stageStatus,
                    stage_level: log.worldId + "_" + log.stageId,
                    stage_score: stageScore,
                    stage_playtime: null,
                    is_autocombat_committed: isAutocombat.AutocombatOn
                );
            }
            catch
            {
            }
        }
    }
}
