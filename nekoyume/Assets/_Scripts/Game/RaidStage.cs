using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;

namespace Nekoyume.Game
{
    public class RaidStage : MonoBehaviour, IStage
    {
        [SerializeField]
        private ObjectPool objectPool;

        private Character.RaidPlayer _player;
        private Character.RaidBoss _boss;

        public readonly ISubject<Stage> OnRoomEnterEnd = new Subject<Stage>();
        public IObservable<RaidStage> OnBattleEnded => _onBattleEnded;
        private readonly ISubject<RaidStage> _onBattleEnded = new Subject<RaidStage>();
        private readonly Queue<Character.RaidActionParams> _actionQueue = new();

        private RaidTimelineContainer container;
        private const float SkillDelay = 0.1f;
        private Coroutine _battleCoroutine;
        private int _waveTurn;
        private bool _isPlaying;

        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }
        public int TurnNumber => _waveTurn;

        private void Awake()
        {

        }

        public void Initialize()
        {
            objectPool.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
        }

        public void Play(
            BattleLog log,
            PlayerDigest player)
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
                    _battleCoroutine = StartCoroutine(CoPlay(log, player));
                }
            }
            else
            {
                Debug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private IEnumerator CoPlay(
            BattleLog log,
            PlayerDigest player)
        {
            yield return StartCoroutine(CoEnter(player));

            var actionDelay = new WaitForSeconds(StageConfig.instance.actionDelay);
            var skillDelay = new WaitForSeconds(SkillDelay);
            var waveIndex = 0;

            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));

                while (_actionQueue.TryDequeue(out var param))
                {
                    var caster = param.RaidCharacter;

                    // Wait for caster idle
                    if (caster.CurrentAction != null)
                    {
                        yield return caster.CurrentAction;
                        yield return actionDelay;
                    }

                    if (caster is Character.RaidBoss &&
                        param.SkillInfos.Any(i => i.SkillCategory != Model.Skill.SkillCategory.NormalAttack))
                    {
                        yield return _player.CurrentAction;
                        yield return StartCoroutine(container.CoPlaySkillCutscene());
                    }

                    caster.CurrentAction = StartCoroutine(CoAct(param));
                    var targets = param.SkillInfos.Select(i => i.Target);
                    foreach (var target in targets)
                    {
                        Character.RaidCharacter character
                            = target.Id == _player.Id ? _player : _boss;
                        if (_player.IsDead)
                        {
                            yield return caster.CurrentAction;
                            yield return StartCoroutine(character.CoDie());
                        }
                        else if (_boss.IsDead)
                        {
                            yield return caster.CurrentAction;
                            yield return StartCoroutine(
                                waveIndex < 4 ? 
                                container.CoPlayRunAwayCutscene(waveIndex) :
                                container.CoPlayFallDownCutscene());
                            yield return StartCoroutine(container.CoPlayAppearCutscene());
                        }
                    }

                    yield return skillDelay;
                }
            }

            yield return StartCoroutine(CoFinish());
        }

        private IEnumerator CoEnter(PlayerDigest playerDigest)
        {
            ActionCamera.instance.gameObject.SetActive(false);
            _actionQueue.Clear();

            CreateContainer(205007);
            MainCanvas.instance.Canvas.worldCamera = container.Camera;

            _player = container.Player;
            _boss = container.Boss;

            _player.Init(playerDigest, _boss);
            _boss.Init(_player);
            container.Show();

            AudioController.instance.PlayMusic(AudioController.MusicCode.StageBlue);
            Widget.Find<LoadingScreen>().Close();
            Game.instance.IsInWorld = true;
            _waveTurn = 1;

            yield return StartCoroutine(container.CoPlayAppearCutscene());
        }

        public IEnumerator CoAct(Character.RaidActionParams param)
        {
            var infos = param.SkillInfos.ToList();
            yield return StartCoroutine(param.ActionCoroutine(infos));
            param.RaidCharacter.CurrentAction = null;

            _player.UpdateStatusUI();
            _boss.UpdateStatusUI();
        }

        private IEnumerator CoFinish()
        {
            IsAvatarStateUpdatedAfterBattle = false;
            _onBattleEnded.OnNext(this);
            yield return _player.CurrentAction;
            yield return _boss.CurrentAction;
            yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);

            if (_battleCoroutine is not null)
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }
            _isPlaying = false;
            ActionRenderHandler.Instance.Pending = false;
            Widget.Find<RaidBattle>().Close();

            container.Close();
            var model = new BattleResultPopup.Model()
            {
                State = BattleLog.Result.Lose,
                LastClearedStageId = 100,
            };
            Widget.Find<BattleResultPopup>().Show(model, false);

            if (container)
            {
                Destroy(container);
            }
            ActionCamera.instance.gameObject.SetActive(true);
            MainCanvas.instance.Canvas.worldCamera = ActionCamera.instance.Cam;
        }

        public IEnumerator CoSpawnPlayer(Player character)
        {
            _player.Spawn(character);
            Widget.Find<RaidBattle>().Show(_player.Model);
            yield break;
        }

        public IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character)
        {
            yield break;
        }

        public IEnumerator CoNormalAttack(
            CharacterBase caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoNormalAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoBlowAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoBlowAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoDoubleAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoAreaAttack);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoHeal(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoHeal);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoBuff(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            var actionParams = new Character.RaidActionParams(target, skillInfos, buffInfos, target.CoBuff);
            _actionQueue.Enqueue(actionParams);
            yield break;
        }

        public IEnumerator CoRemoveBuffs(CharacterBase caster)
        {
            Character.RaidCharacter target = caster.Id == _player.Id ? _player : _boss;
            target.UpdateStatusUI();
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
            Widget.Find<RaidBattle>().SetBossProfile(_boss.Model as Enemy);
            yield break;
        }

        public IEnumerator CoGetExp(long exp)
        {
            yield break;
        }

        public IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn)
        {
            _waveTurn = waveTurn;
            yield break;
        }

        public IEnumerator CoDead(CharacterBase character)
        {
            yield break;
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
