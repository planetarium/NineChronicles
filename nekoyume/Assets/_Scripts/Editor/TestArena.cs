using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Blockchain;
using Nekoyume.Editor;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using ArenaCharacter = Nekoyume.Model.ArenaCharacter;

namespace SimulationTest
{
    public class TestArena : MonoBehaviour, IArena
    {
        [SerializeField]
        private GameObject container;

        [SerializeField]
        private TestArenaCharacter me;

        [SerializeField]
        private TestArenaCharacter enemy;

        [SerializeField]
        private bool logCoExecute;
        public IObservable<Arena> OnArenaEnd => _onArenaEnd;
        private readonly ISubject<Arena> _onArenaEnd = new Subject<Arena>();
        private const float SkillDelay = 0.1f;
        private Coroutine _battleCoroutine;
        private int _turnNumber;
        private bool _isPlaying;

        // Only Changed on Thread Pool
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }
        public int TurnNumber => _turnNumber;

        public static TestArena Instance;
        public TableSheets TableSheets { get; set; }
        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            yield return ResourceManager.Instance.InitializeAsync().ToCoroutine();
            yield return CharacterManager.Instance.LoadCharacterAssetAsync().ToCoroutine();
            ResourcesHelper.Initialize();
            L10nManager.Initialize();
        }

        public IEnumerator CoSkill(ArenaActionParams param)
        {
            var infos = param.skillInfos.ToList();
            var turn = infos.First().Turn;
            var time = Time.time;
            yield return new WaitUntil(() => Time.time - time > 5f || _turnNumber == turn);

            foreach (var info in infos)
            {
                if (info.Target.Id.Equals(me.Id))
                {
                    me.CharacterModel = info.Target;
                }
                else
                {
                    enemy.CharacterModel = info.Target;
                }
            }

            yield return StartCoroutine(param.func(infos));

            me.UpdateStatusUI();
            enemy.UpdateStatusUI();

            yield return new WaitForSeconds(SkillDelay);
        }

        public void Enter(
            ArenaLog log,
            List<ItemBase> rewards,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            (int, int)? winDefeatCount = null)
        {
            if (_battleCoroutine is not null)
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }

            if (log?.Events.Count > 0)
            {
                _battleCoroutine =
                    StartCoroutine(CoEnter(log, rewards, myDigest, enemyDigest,
                        myAvatarAddress, enemyAvatarAddress, winDefeatCount));
            }
        }

        private IEnumerator CoEnter(
            ArenaLog log,
            IReadOnlyList<ItemBase> rewards,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            (int, int)? winDefeatCount = null)
        {
            NcDebug.Log(
                $"[CoEnter] avatar1: {myDigest.NameWithHash.Split("<").First()}, avatar2: {enemyDigest.NameWithHash.Split("<").First()}",
                "BattleSimulation");
            yield return StartCoroutine(CoStart(myDigest, enemyDigest, myAvatarAddress, enemyAvatarAddress));

            foreach (var e in log)
            {
                if (logCoExecute)
                {
                    NcDebug.Log(
                        $"[CoExecute] event: {e.GetType()}, turn: {_turnNumber}, character: {e.Character.Id}",
                        "BattleSimulation");
                }

                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoEnd(log));
        }

        private IEnumerator CoStart(
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest,
            Address myAvatarAddress,
            Address enemyAvatarAddress)
        {
            container.SetActive(true);
            me.Init(myDigest, myAvatarAddress, enemy, false);
            enemy.Init(enemyDigest, enemyAvatarAddress, me, true);

            _turnNumber = 1;

            yield return new WaitForSeconds(2.0f);
        }

        private IEnumerator CoEnd(
            ArenaLog log)
        {
            yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);
            yield return new WaitWhile(() => me.HasAction());
            yield return new WaitWhile(() => enemy.HasAction());
            yield return new WaitForSeconds(0.75f);

            var arenaCharacter = log.Result == ArenaLog.ArenaResult.Win ? me : enemy;
            arenaCharacter.Animator.Win();
            arenaCharacter.ShowSpeech("PLAYER_WIN");
            NcDebug.Log(
                $"[CoEnd] result: {log.Result}, turn: {_turnNumber}",
                "BattleSimulation");
            yield return null;
        }

        public IEnumerator CoSpawnCharacter(ArenaCharacter character)
        {
            if (character.IsEnemy)
            {
                enemy.Spawn(character);
            }
            else
            {
                me.Spawn(character);
                me.ShowSpeech("PLAYER_INIT");
            }

            yield return null;
        }

        public IEnumerator CoNormalAttack(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoNormalAttack);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoBlowAttack(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoBlowAttack);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoBuffRemovalAttack(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoBlowAttack);
            target.AddAction(actionParams);
            yield return null;
        }


        public IEnumerator CoDoubleAttackWithCombo(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoDoubleAttackWithCombo);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoDoubleAttack(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoDoubleAttack);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoAreaAttack(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoAreaAttack);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoHeal(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoHeal);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoBuff(
            ArenaCharacter caster,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoBuff);
            target.AddAction(actionParams);
            yield return null;
        }

        public IEnumerator CoTickDamage(ArenaCharacter affectedCharacter,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            var target = affectedCharacter.Id == me.Id ? me : enemy;
            foreach (var info in skillInfos)
            {
                yield return new WaitWhile(() => target.HasAction());
                yield return StartCoroutine(target.CoProcessDamage(info, true));
                yield return new WaitForSeconds(SkillDelay);
            }
        }

        public IEnumerator CoRemoveBuffs(ArenaCharacter caster)
        {
            var target = caster.Id == me.Id ? me : enemy;
            NcDebug.Log(
                $@"[CoRemoveBuffs] target: {target.Id}, buffs: {target.CharacterModel.Buffs.Select(pair =>
                    $"buff: {pair.Value.BuffInfo.Id}, duration: {pair.Value.RemainedDuration}/{pair.Value.OriginalDuration}"
                ).Aggregate((a,b) => $"{a}\n{b}")}",
                "BattleSimulation");
            target.UpdateStatusUI();
            yield break;
        }

        public IEnumerator CoDead(ArenaCharacter caster)
        {
            yield return new WaitWhile(() => me.HasAction());
            yield return new WaitWhile(() => enemy.HasAction());
            var target = caster.Id == me.Id ? me : enemy;
            target.Dead();
        }

        public IEnumerator CoTurnEnd(int turnNumber)
        {
            yield return new WaitWhile(() => me.HasAction());
            yield return new WaitWhile(() => enemy.HasAction());
            _turnNumber = turnNumber + 1;
            NcDebug.Log(
                $@"[CoTurnEnd({_turnNumber})] target: {me.Id}, buffs: [{(me.CharacterModel.Buffs?.Any() ?? false ? me.CharacterModel.Buffs.Select(pair =>
                    $"buff: {pair.Value.BuffInfo.GetType()}, id: {pair.Value.BuffInfo.Id}, duration: {pair.Value.RemainedDuration}/{pair.Value.OriginalDuration}"
                ).Aggregate((a,b) => $"{a}\n{b}") : "null")}]",
                "BattleSimulation");
            NcDebug.Log(
                $@"[CoTurnEnd({_turnNumber})] target: {enemy.Id}, buffs: [{(enemy.CharacterModel.Buffs?.Any() ?? false ? enemy.CharacterModel.Buffs.Select(pair =>
                    $"buff: {pair.Value.BuffInfo.GetType()}, id: {pair.Value.BuffInfo.Id}, duration: {pair.Value.RemainedDuration}/{pair.Value.OriginalDuration}"
                ).Aggregate((a,b) => $"{a}; {b}") : "null")}]",
                "BattleSimulation");
            yield return null;
        }

        public IEnumerator CoCustomEvent(ArenaCharacter caster, ArenaEventBase eventBase)
        {
            if (eventBase is ArenaTick tick)
            {
                var affectedCharacter = caster.Id == me.Id ? me : enemy;
                if (AuraIceShield.IsFrostBiteBuff(tick.SkillId))
                {
                    foreach (var kvp in caster.Buffs)
                    {
                        if (!AuraIceShield.IsFrostBiteBuff(kvp.Key))
                        {
                            continue;
                        }

                        var frostBite = kvp.Value;
                        var sourceCharacter = caster.Id == me.Id ? enemy : me;

                        IEnumerator CoFrostBite(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
                        {
                            sourceCharacter.CustomEvent(tick.SkillId);
                            yield return affectedCharacter.CoBuff(skillInfos);
                        }

                        var tickSkillInfo = new ArenaSkill.ArenaSkillInfo(
                            caster,
                            0,
                            false,
                            SkillCategory.Debuff,
                            _turnNumber,
                            buff: frostBite
                        );
                        var actionParams = new ArenaActionParams(
                            affectedCharacter,
                            ArraySegment<ArenaSkill.ArenaSkillInfo>.Empty.Append(tickSkillInfo),
                            tick.BuffInfos,
                            CoFrostBite);
                        affectedCharacter.AddAction(actionParams);
                        yield return null;
                        break;
                    }
                }

                // This Tick from 'Stun'
                if (!tick.SkillInfos.Any())
                {
                    IEnumerator StunTick(IReadOnlyList<ArenaSkill.ArenaSkillInfo> readOnlyList)
                    {
                        affectedCharacter.Animator.Hit();
                        yield return new WaitForSeconds(SkillDelay);
                    }

                    var tickSkillInfo = new ArenaSkill.ArenaSkillInfo(
                        caster,
                        0,
                        false,
                        SkillCategory.TickDamage,
                        _turnNumber
                    );
                    var actionParams = new ArenaActionParams(
                        affectedCharacter,
                        tick.SkillInfos.Append(tickSkillInfo),
                        tick.BuffInfos,
                        StunTick);
                    affectedCharacter.AddAction(actionParams);
                    yield return null;
                }
                // This Tick from 'Vampiric'
                else if (tick.SkillInfos.Any(info => info.SkillCategory == SkillCategory.Heal))
                {
                    var actionParams = new ArenaActionParams(
                        affectedCharacter,
                        tick.SkillInfos,
                        tick.BuffInfos,
                        affectedCharacter.CoHealWithoutAnimation);
                    affectedCharacter.AddAction(actionParams);
                    yield return null;
                }
            }
        }

        public IEnumerator CoShatterStrike(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoShatterStrike);
            target.AddAction(actionParams);
            yield return null;
        }
    }
}

