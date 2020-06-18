// #define TEST_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
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

        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public bool IsInStage { get; private set; }
        public Enemy Boss { get; private set; }
        public AvatarState AvatarState { get; set; }
        public Vector3 SelectPositionBegin(int index) => new Vector3(-2.15f + index * 2.22f, -1.79f, 0.0f);
        public Vector3 SelectPositionEnd(int index) => new Vector3(-2.15f + index * 2.22f, -0.25f, 0.0f);

        public bool showLoadingScreen;

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
        }

        private void OnStageStart(BattleLog log)
        {
            _rankingBattle = false;
            if (_battleLog?.id != log.id)
            {
                _battleLog = log;
                PlayStage(_battleLog);
            }
            else
            {
                Debug.Log("Skip duplicated battle");
            }
        }

        private void OnRankingBattleStart(BattleLog log)
        {
            _rankingBattle = true;
            if (_battleLog?.id != log.id)
            {
                _battleLog = log;
                PlayRankingBattle(_battleLog);
            }
            else
            {
                Debug.Log("Skip duplicated battle");
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
                    var seqPos = new Vector3(moveTo.x, moveTo.y - UnityEngine.Random.Range(0.05f, 0.1f), 0.0f);
                    var seq = DOTween.Sequence();
                    seq.Append(playerObject.transform.DOMove(seqPos, UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Append(playerObject.transform.DOMove(moveTo, UnityEngine.Random.Range(4.0f, 5.0f)));
                    seq.Play().SetDelay(2.6f).SetLoops(-1);
                    if (!ReferenceEquals(anim, null) && !anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        var skeleton = anim.Target.GetComponentInChildren<SkeletonAnimation>().skeleton;
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
                StartCoroutine(CoPlayStage(log));
            }
        }

        private void PlayRankingBattle(BattleLog log)
        {
            if (log?.Count > 0)
            {
                StartCoroutine(CoPlayRankingBattle(log));
            }
        }

        private IEnumerator CoPlayStage(BattleLog log)
        {
            //[TentuPlay] PlayStage 시작 기록
            new TPStashEvent().PlayerStage(
                player_uuid: Game.instance.Agent.Address.ToHex(),
                stage_category_slug: "HackAndSlash",
                stage_slug: "HackAndSlash" + "_" + log.worldId.ToString() + "_" + log.stageId.ToString(),
                stage_status: stageStatus.Start,
                stage_level: log.worldId.ToString() + "_" + log.stageId.ToString(),
                is_autocombat_committed: isAutocombat.AutocombatOn
                );

            IsInStage = true;
            yield return StartCoroutine(CoStageEnter(log));
            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoStageEnd(log));
            IsInStage = false;
        }

        private IEnumerator CoPlayRankingBattle(BattleLog log)
        {
            //[TentuPlay] RankingBattle 시작 기록
            new TPStashEvent().PlayerStage(
                player_uuid: Game.instance.Agent.Address.ToHex(),
                stage_category_slug: "RankingBattle",
                stage_slug: "RankingBattle",
                stage_status: stageStatus.Start,
                stage_level: null,
                is_autocombat_committed: isAutocombat.AutocombatOn
                );

            IsInStage = true;
            yield return StartCoroutine(CoRankingBattleEnter(log));
            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoRankingBattleEnd(log));
            IsInStage = false;
        }

        private static IEnumerator CoDialog(int worldStage)
        {
            var stageDialogs = Game.instance.TableSheets.StageDialogSheet.Values
                .Where(i => i.StageId == worldStage)
                .OrderBy(i => i.DialogId)
                .ToArray();
            if (stageDialogs.Any())
            {
                var dialog = Widget.Find<Dialog>();

                foreach (var stageDialog in stageDialogs)
                {
                    dialog.Show(stageDialog.DialogId);
                    yield return new WaitWhile(() => dialog.gameObject.activeSelf);
                }
            }

            yield return null;
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

            var battle = Widget.Find<UI.Battle>();
            Game.instance.TableSheets.StageSheet.TryGetValue(stageId, out var stageData);
            battle.stageProgressBar.Initialize(true);
            Widget.Find<BattleResult>().stageProgressBar.Initialize(false);
            var title = Widget.Find<StageTitle>();
            title.Show(stageId);

            yield return new WaitForSeconds(2.0f);

            yield return StartCoroutine(title.CoClose());

            AudioController.instance.PlayMusic(data.BGM);
        }

        private IEnumerator CoRankingBattleEnter(BattleLog log)
        {
            waveCount = log.waveCount;
            waveTurn = 1;
#if TEST_LOG
            Debug.LogWarning($"{nameof(waveTurn)}: {waveTurn} / {nameof(CoRankingBattleEnter)}");
#endif
            if (!Game.instance.TableSheets.StageSheet.TryGetValue(1, out var data))
                yield break;

            _battleResultModel = new BattleResult.Model();

            zone = data.Background;
            LoadBackground(zone, 3.0f);
            PlayBGVFX(false);
            RunPlayer();

            yield return new WaitForSeconds(2.0f);

            AudioController.instance.PlayMusic(data.BGM);
        }

        private IEnumerator CoStageEnd(BattleLog log)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            yield return new WaitForSeconds(1f);
            Boss = null;
            Widget.Find<UI.Battle>().bossStatus.Close();
            Widget.Find<UI.Battle>().Close();
            yield return StartCoroutine(CoUnlockAlert());
            _battleResultModel.ClearedWaveNumber = log.clearedWaveNumber;
            var passed = _battleResultModel.ClearedWaveNumber == log.waveCount;
            yield return new WaitForSeconds(0.75f);
            if (log.result == BattleLog.Result.Win)
            {
                var playerCharacter = GetPlayer();
                playerCharacter.DisableHUD();
                if (passed)
                {
                    yield return StartCoroutine(CoDialog(log.stageId));
                }

                playerCharacter.Animator.Win(log.clearedWaveNumber);
                playerCharacter.ShowSpeech("PLAYER_WIN");
                yield return new WaitForSeconds(2.2f);
                objectPool.ReleaseExceptForPlayer();
                if (passed)
                {
                    StartCoroutine(CoSlideBg());
                }
            }
            else
            {
                objectPool.ReleaseAll();
            }

            var avatarState =
                new AvatarState(
                    (Bencodex.Types.Dictionary) Game.instance.Agent.GetState(States.Instance.CurrentAvatarState
                        .address));
            _battleResultModel.State = log.result;
            var stage = Game.instance.TableSheets.StageSheet.Values.First(i => i.Id == stageId);
            var apNotEnough = avatarState.actionPoint < stage.CostAP;
            _battleResultModel.ActionPointNotEnough = apNotEnough;
            _battleResultModel.ShouldExit = apNotEnough || isExitReserved;
            _battleResultModel.ShouldRepeat = !apNotEnough && (repeatStage || !passed);

            if (!_battleResultModel.ShouldRepeat)
            {
                Game.instance.TableSheets.WorldSheet.TryGetValue(worldId, out var worldRow, true);
                if (stageId == worldRow.StageEnd)
                {
                    _battleResultModel.ShouldExit = true;
                }
            }

            ActionRenderHandler.Instance.Pending = false;
            Widget.Find<BattleResult>().Show(_battleResultModel);
            yield return null;

            //[TentuPlay] PlayStage 끝 기록
            stageStatus stage_status = stageStatus.Unknown;
            switch (log.result)
            {
                case BattleLog.Result.Win:
                    stage_status = stageStatus.Win;
                    break;
                case BattleLog.Result.Lose:
                    stage_status = stageStatus.Lose;
                    break;
                case BattleLog.Result.TimeOver:
                    stage_status = stageStatus.Timeout;
                    break;
            }
            new TPStashEvent().PlayerStage(
                player_uuid: Game.instance.Agent.Address.ToHex(),
                stage_category_slug: "HackAndSlash",
                stage_slug: "HackAndSlash" + "_" + log.worldId.ToString() + "_" + log.stageId.ToString(),
                stage_status: stage_status,
                stage_level: log.worldId.ToString() + "_" + log.stageId.ToString(),
                stage_score: log.clearedWaveNumber,
                stage_playtime: null,
                is_autocombat_committed: isAutocombat.AutocombatOn
                );
        }

        private IEnumerator CoSlideBg()
        {
            RunPlayer();
            while (Widget.Find<BattleResult>().IsActive())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator CoRankingBattleEnd(BattleLog log)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));

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
            Widget.Find<RankingBattleResult>().Show(log.result, log.score, log.diffScore);
            yield return null;

            //[TentuPlay] RankingBattle 끝 기록
            stageStatus stage_status = stageStatus.Unknown;
            switch (log.result)
            {
                case BattleLog.Result.Win:
                    stage_status = stageStatus.Win;
                    break;
                case BattleLog.Result.Lose:
                    stage_status = stageStatus.Lose;
                    break;
                case BattleLog.Result.TimeOver:
                    stage_status = stageStatus.Timeout;
                    break;
            }
            new TPStashEvent().PlayerStage(
                player_uuid: Game.instance.Agent.Address.ToHex(),
                stage_category_slug: "RankingBattle",
                stage_slug: "RankingBattle",
                stage_status: stage_status,
                stage_level: null,
                stage_score: log.diffScore,
                stage_playtime: null,
                is_autocombat_committed: isAutocombat.AutocombatOn
                );
        }

        public IEnumerator CoSpawnPlayer(Player character)
        {
            var playerCharacter = RunPlayer();
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
                battle.Show();
                battle.comboText.Close();
                battle.stageProgressBar.Close();
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

            battle.repeatButton.gameObject.SetActive(!_rankingBattle);

            if (!(AvatarState is null) && !ActionRenderHandler.Instance.Pending)
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(AvatarState);
            }
            yield return null;
        }

        public IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character)
        {
            var battle = Widget.Find<UI.Battle>();
            battle.bossStatus.Close();
            battle.enemyPlayerStatus.Show();
            battle.enemyPlayerStatus.SetHp(character.CurrentHP, character.HP);

            var sprite = SpriteHelper.GetItemIcon(character.armor?.Id ?? GameConfig.DefaultAvatarArmorId);
            battle.enemyPlayerStatus.SetProfile(character.Level, character.NameWithHash, sprite);
            yield return StartCoroutine(spawner.CoSetData(character));
        }

        #region Skill

        public IEnumerator CoNormalAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoNormalAttack));
                yield return null;
            }
        }

        public IEnumerator CoBlowAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoBlowAttack));
                yield return null;
            }
        }

        public IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoDoubleAttack));
                yield return null;
            }
        }

        public IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoAreaAttack));
                yield return null;
            }
        }

        public IEnumerator CoHeal(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoHeal));
                yield return null;
            }
        }

        public IEnumerator CoBuff(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var character = GetCharacter(caster);
            if (character)
            {
                character.actions.Add(CoSkill(character, skillInfos, buffInfos, character.CoBuff));
                yield return null;
            }
        }

        private IEnumerator CoSkill(Character.CharacterBase character, IEnumerable<Skill.SkillInfo> skillInfos,
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
            yield return new WaitUntil(() => waveTurn == infosFirstWaveTurn);
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
            yield return new WaitUntil(() => Time.time - time > 2f || character.TargetInAttackRange(enemy));
        }

        private IEnumerator CoAfterSkill(Character.CharacterBase character,
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

        public IEnumerator CoRemoveBuffs(CharacterBase caster)
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

        public IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss)
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
            yield return new WaitForSeconds(.3f);
            Widget.Find<UI.Battle>().bossStatus.Close();
            Widget.Find<UI.Battle>().enemyPlayerStatus.Close();
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
                battle.bossStatus.Show();
                battle.bossStatus.SetHp(boss.HP, boss.HP);
                battle.bossStatus.SetProfile(boss.Level, LocalizationManager.LocalizeCharacterName(boss.RowData.Id),
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
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            this.waveTurn = waveTurn;
#if TEST_LOG
            Debug.LogWarning($"{nameof(this.waveTurn)}: {this.waveTurn} / {nameof(CoWaveTurnEnd)} Exit");
#endif
            Event.OnPlayerTurnEnd.Invoke(turnNumber);
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

        public IEnumerator CoDead(CharacterBase model)
        {
            var characters = GetComponentsInChildren<Character.CharacterBase>();
            yield return new WaitWhile(() => characters.Any(i => i.actions.Any()));
            var character = GetCharacter(model);

            var isPlayerCleared = model is Player && _battleLog.clearedWaveNumber > 0;
            if (isPlayerCleared)
                yield break;

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
                objectPool.Remove<Player>(selectedPlayer.gameObject);
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

        private Character.Player RunPlayer()
        {
            var player = GetPlayer();
            var playerTransform = player.transform;
            Vector2 position = playerTransform.position;
            position.y = StageStartPosition;
            playerTransform.position = position;
            RunAndChasePlayer(player);
            return player;
        }

        public Character.Player RunPlayer(Vector2 position)
        {
            var player = GetPlayer(position);
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
        public Character.CharacterBase GetCharacter(CharacterBase caster)
        {
            if (caster is null)
                throw new ArgumentNullException(nameof(caster));

            var character = GetComponentsInChildren<Character.CharacterBase>().FirstOrDefault(c => c.Id == caster.Id);
            if (!(character is null))
                character.Set(caster);
            return character;
        }

        private void PlayBGVFX(bool isBoss)
        {
            if (isBoss)
            {
                if (defaultBGVFX)
                    defaultBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (bosswaveBGVFX)
                    bosswaveBGVFX.Play(true);
            }
            else
            {
                if (bosswaveBGVFX)
                    bosswaveBGVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (defaultBGVFX)
                    defaultBGVFX.Play(true);
            }
        }

        private IEnumerator CoUnlockAlert()
        {
            if (waveNumber != waveCount || !newlyClearedStage)
                yield break;

            var key = string.Empty;
            if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuCombination)
            {
                key = "UI_UNLOCK_COMBINATION";
            }
            else if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuShop)
            {
                key = "UI_UNLOCK_SHOP";
            }
            else if (stageId == GameConfig.RequireClearedStageLevel.UIMainMenuRankingBoard)
            {
                key = "UI_UNLOCK_RANKING";
            }

            if (string.IsNullOrEmpty(key))
                yield break;

            var w = Widget.Find<Alert>();
            w.Show("UI_UNLOCK_TITLE", key);
            yield return new WaitWhile(() => w.isActiveAndEnabled);
        }

        private static void RunAndChasePlayer(Character.Player player)
        {
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
        }
    }
}
