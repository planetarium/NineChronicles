using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Game
{
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class RaidStage : MonoBehaviour, IStage
    {
        [SerializeField]
        private ObjectPool objectPool;

        [SerializeField]
        private float skillDelay = 0.3f;

        [SerializeField]
        private int alertTurn = 10;

        private Character.RaidPlayer _player;
        private Character.RaidBoss _boss;

        public readonly ISubject<Stage> OnRoomEnterEnd = new Subject<Stage>();
        public IObservable<RaidStage> OnBattleEnded => _onBattleEnded;
        private readonly ISubject<RaidStage> _onBattleEnded = new Subject<RaidStage>();
        private readonly Queue<Character.RaidActionParams> _actionQueue = new();

        private RaidTimelineContainer container;
        private Coroutine _battleCoroutine;
        private IEnumerator _nextWaveCoroutine;
        private int _waveTurn;
        private int _turnLimit;
        private int _wave;
        private bool _isPlaying;
        private int _currentScore;
        private int _currentBossId;

        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }
        public RaidCamera Camera => container.Camera;
        public int TurnNumber => _waveTurn;

        public void Initialize()
        {
            objectPool.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
        }

        public void Play(
            int bossId,
            BattleLog log,
            ArenaPlayerDigest player,
            int damageDealt,
            bool isNewRecord,
            bool isPractice,
            List<FungibleAssetValue> battleRewards,
            List<FungibleAssetValue> killRewards)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;

                if (_battleCoroutine is not null)
                {
                    StopCoroutine(_battleCoroutine);
                    _battleCoroutine = null;
                    objectPool.ReleaseAll();
                }

                if (log?.Count > 0)
                {
                    _battleCoroutine = StartCoroutine(
                        CoPlay(bossId, log, player, damageDealt, isNewRecord, isPractice, battleRewards, killRewards));
                }
            }
            else
            {
                Debug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private IEnumerator CoPlay(
            int bossId,
            BattleLog log,
            ArenaPlayerDigest player,
            int damageDealt,
            bool isNewRecord,
            bool isPractice,
            List<FungibleAssetValue> rewards,
            List<FungibleAssetValue> killRewards)
        {
            yield return StartCoroutine(CoEnter(bossId, player));

            var actionDelay = new WaitForSeconds(StageConfig.instance.actionDelay);
            var skillDelay = new WaitForSeconds(this.skillDelay);

            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));

                while (_actionQueue.TryDequeue(out var param))
                {
                    var caster = param.RaidCharacter;

                    // Wait for caster idle
                    if (caster.IsActing)
                    {
                        yield return new WaitWhile(() => caster.IsActing);
                        yield return actionDelay;
                    }

                    if (caster is Character.RaidBoss boss)
                    {
                        if (container.SkillCutsceneExists(param.SkillId))
                        {
                            if (!Game.instance.TableSheets.SkillSheet
                                .TryGetValue(param.SkillId, out var skillRow))
                            {
                                continue;
                            }

                            var playAll = skillRow.SkillType != Model.Skill.SkillType.Attack;
                            container.OnAttackPoint = () =>
                            {
                                boss.ProceedSkill(playAll);
                            };

                            var infos = param.SkillInfos;
                            if (param.BuffInfos is not null &&
                                param.BuffInfos.Any())
                            {
                                infos = infos.Concat(param.BuffInfos);
                            }

                            boss.SetSkillInfos(skillRow, infos);
                            yield return StartCoroutine(container.CoPlaySkillCutscene(param.SkillId));
                            if (!playAll)
                            {
                                // Show remaining skill infos
                                boss.ProceedSkill(true);
                            }

                            _player.UpdateStatusUI();
                            _boss.UpdateStatusUI();
                        }
                        else
                        {
                            caster.CurrentAction = StartCoroutine(CoAct(param));
                        }
                    }
                    else
                    {
                        caster.CurrentAction = StartCoroutine(CoAct(param));
                    }
                }

                yield return skillDelay;
            }

            yield return StartCoroutine(CoFinish(damageDealt, isNewRecord, isPractice, rewards, killRewards));
        }

        private IEnumerator CoEnter(int bossId, ArenaPlayerDigest playerDigest)
        {
            _currentBossId = bossId;
            _nextWaveCoroutine = null;
            Widget.Find<HeaderMenuStatic>().Close(true);
            ActionCamera.instance.Cam.gameObject.SetActive(false);
            _actionQueue.Clear();

            CreateContainer(bossId);
            container.Show();
            MainCanvas.instance.Canvas.worldCamera = container.Camera.Cam;

            _player = container.Player;
            _boss = container.Boss;

            _player.Init(playerDigest, _boss);
            _boss.Init(_player);

            if (WorldBossFrontHelper.TryGetBossData(bossId, out var data))
            {
                AudioController.instance.PlayMusic(data.battleMusicName);
            }

            Widget.Find<LoadingScreen>().Close();
            Game.instance.IsInWorld = true;
            _waveTurn = 1;
            _wave = 0;
            _currentScore = 0;

            yield return StartCoroutine(container.CoPlayAppearCutscene());
            _boss.Animator.Idle();
        }

        public IEnumerator CoAct(Character.RaidActionParams param)
        {
            var infos = param.SkillInfos.ToList();
            yield return StartCoroutine(param.ActionCoroutine(infos));
            param.RaidCharacter.CurrentAction = null;

            _player.UpdateStatusUI();
            _boss.UpdateStatusUI();
        }

        private IEnumerator CoFinish(
            int damageDealt,
            bool isNewRecord,
            bool isPractice,
            List<FungibleAssetValue> rewards,
            List<FungibleAssetValue> killRewards)
        {
            if (TurnNumber >= _turnLimit && !_boss.IsDead)
            {
                yield return container.CoPlayPlayerDefeatCutscene();
            }

            IsAvatarStateUpdatedAfterBattle = false;
            _onBattleEnded.OnNext(this);

            if (!isPractice)
            {
                yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);
            }

            if (_battleCoroutine is not null)
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }
            _isPlaying = false;
            ActionRenderHandler.Instance.Pending = false;
            Game.instance.IsInWorld = false;
            Widget.Find<WorldBossBattle>().Close();

            container.Close();

            var actionCam = ActionCamera.instance;
            actionCam.Cam.gameObject.SetActive(true);
            actionCam.RerunFSM();
            MainCanvas.instance.Canvas.worldCamera = ActionCamera.instance.Cam;

            if (isPractice)
            {
                Widget.Find<WorldBossResultPopup>().ShowAsPractice(_currentBossId, damageDealt);
            }
            else
            {
                Widget.Find<WorldBossResultPopup>().Show(
                    _currentBossId, damageDealt, isNewRecord, rewards, killRewards);
            }

            if (container)
            {
                Destroy(container.gameObject);
            }
        }

        public IEnumerator CoSpawnPlayer(Player character)
        {
            _player.Spawn(character);
            Widget.Find<WorldBossBattle>().SetData(_currentBossId);
            yield break;
        }

        public IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character)
        {
            yield break;
        }

        public IEnumerator CoNormalAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoNormalAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoBlowAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoBlowAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoBuffRemovalAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoBlowAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoDoubleAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoDoubleAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoAreaAttack(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoAreaAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoHeal(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoHeal);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoTickDamage(CharacterBase affectedCharacter,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos)
        {
            Character.RaidCharacter target = affectedCharacter.Id == _player.Id ? _player : _boss;
            target.Set(affectedCharacter);

            yield return new WaitWhile(() => target.IsActing);
            foreach (var info in skillInfos)
            {
                target.ProcessDamage(info, true);
            }
        }

        public IEnumerator CoBuff(
            CharacterBase caster,
            int skillId,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            var actionParams = new Character.RaidActionParams(target, skillId, skillInfos, buffInfos, target.CoBuff);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoRemoveBuffs(CharacterBase caster)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.Set(caster);
            target.UpdateStatusUI();
            if (target)
            {
                if (target.HPBar.HpVFX != null)
                {
                    target.HPBar.HpVFX.Stop();
                }
            }

            yield break;
        }

        public IEnumerator CoDropBox(List<ItemBase> items)
        {
            yield break;
        }

        public IEnumerator CoGetReward(List<ItemBase> rewards)
        {
            yield break;
        }

        public IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss)
        {
            _boss.Spawn(enemies.First());
            if (_nextWaveCoroutine != null)
            {
                yield return StartCoroutine(_nextWaveCoroutine);
            }
            Widget.Find<WorldBossBattle>().Show();

            _turnLimit = 150;
            var sheet = Game.instance.TableSheets.WorldBossCharacterSheet;
            if (sheet.TryGetValue(_currentBossId, out var boss))
            {
                _turnLimit = boss.WaveStats.FirstOrDefault(x => x.Wave == waveNumber).TurnLimit;
            }

            Widget.Find<WorldBossBattle>().SetBossProfile(_boss.Model as Enemy, _turnLimit);
        }

        public IEnumerator CoGetExp(long exp)
        {
            yield break;
        }

        public IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn)
        {
            _waveTurn = waveTurn;
            Event.OnPlayerTurnEnd.Invoke(turnNumber);
            if (_turnLimit - _waveTurn == alertTurn)
            {
                var message = L10nManager.Localize("UI_MESSAGE_TURNS_LEFT_FORMAT", alertTurn);
                OneLineSystem.Push(Model.Mail.MailType.System, message, NotificationCell.NotificationType.Alert);
            }
            yield break;
        }

        public IEnumerator CoDead(CharacterBase character)
        {
            Character.RaidCharacter raidCharacter =
                character.Id == _player.Id ? _player : _boss;
            raidCharacter.Set(character);

            Widget.Find<WorldBossBattle>().Close();
            if (raidCharacter is Character.RaidPlayer player)
            {
                yield return StartCoroutine(container.CoPlayPlayerDefeatCutscene());
            }
            else if (raidCharacter is Character.RaidBoss boss)
            {
                Widget.Find<WorldBossBattle>().OnWaveCompleted();

                if (_wave < 4)
                {
                    _nextWaveCoroutine = container.CoPlayRunAwayCutscene(_wave);
                }
                else
                {
                    yield return StartCoroutine(container.CoPlayFallDownCutscene());
                }

                ++_wave;
            }
        }

        public void AddScore(int score)
        {
            _currentScore += score;
            Widget.Find<WorldBossBattle>().UpdateScore(_currentScore);
        }

        private void CreateContainer(int id)
        {
            if (container)
            {
                Destroy(container);
            }

            var prefab = Resources.Load<RaidTimelineContainer>($"Timeline/WorldBoss/ContainerPrefabs/{id}");
            container = Instantiate(prefab, transform);
        }
    }
}
