using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using ArenaCharacter = Nekoyume.Model.ArenaCharacter;

namespace Nekoyume.Game.Battle
{
    public class Arena : MonoBehaviour, IArena
    {
        [SerializeField]
        private ObjectPool objectPool;

        [SerializeField]
        private GameObject container;

        [SerializeField]
        private Character.ArenaCharacter me;

        [SerializeField]
        private Character.ArenaCharacter enemy;

        public readonly ISubject<Stage> OnRoomEnterEnd = new Subject<Stage>();
        public IObservable<Arena> OnArenaEnd => _onArenaEnd;
        private readonly ISubject<Arena> _onArenaEnd = new Subject<Arena>();
        private const float SkillDelay = 0.1f;
        private const float AnimatorTimeScale = 2.5f;
        private Coroutine _battleCoroutine;
        private int _turnNumber;
        private bool _isPlaying;

        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        // Only Changed on Thread Pool
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }
        public int TurnNumber => _turnNumber;

        public void Initialize()
        {
            objectPool.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
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
            if (!_isPlaying)
            {
                _isPlaying = true;

                if (!(_battleCoroutine is null))
                {
                    StopCoroutine(_battleCoroutine);
                    _battleCoroutine = null;
                    objectPool.ReleaseAll();
                }

                if (log?.Events.Count > 0)
                {
                    _battleCoroutine =
                        StartCoroutine(CoEnter(log, rewards, myDigest, enemyDigest,
                            myAvatarAddress, enemyAvatarAddress, winDefeatCount));
                }
            }
            else
            {
                NcDebug.Log("Skip incoming battle. Battle is already simulating.");
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
            yield return StartCoroutine(CoStart(myDigest, enemyDigest, myAvatarAddress, enemyAvatarAddress));

            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoEnd(log, rewards, winDefeatCount));
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

            Widget.Find<ArenaBattle>().Show(myDigest, enemyDigest, myAvatarAddress, enemyAvatarAddress);
            enemy.Pet.Animator.DestroyTarget();
            yield return new WaitForSeconds(2.0f);

            AudioController.instance.PlayMusic(AudioController.MusicCode.PVPBattle);
            me.Pet.Animator.Play(PetAnimation.Type.BattleStart);
            Widget.Find<ArenaBattleLoadingScreen>().Close();
            BattleRenderer.Instance.IsOnBattle = true;
        }

        private IEnumerator CoEnd(
            ArenaLog log,
            IReadOnlyList<ItemBase> rewards,
            (int, int)? winDefeatCount = null)
        {
            IsAvatarStateUpdatedAfterBattle = false;
            ActionRenderHandler.Instance.Pending = false;
            _onArenaEnd.OnNext(this);

            yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);
            yield return new WaitWhile(() => me.HasAction());
            yield return new WaitWhile(() => enemy.HasAction());
            yield return new WaitForSeconds(0.75f);

            var arenaCharacter = log.Result == ArenaLog.ArenaResult.Win ? me : enemy;
            arenaCharacter.Animator.Win();
            arenaCharacter.ShowSpeech("PLAYER_WIN");
            arenaCharacter.Pet.Animator.Play(PetAnimation.Type.BattleEnd);
            Widget.Find<ArenaBattle>().Close();
            Widget.Find<RankingBattleResultPopup>().Show(log, rewards, OnEnd, winDefeatCount);
            yield return null;
        }

        private void OnEnd()
        {
            container.SetActive(false);
            me.gameObject.SetActive(false);
            enemy.gameObject.SetActive(false);
            objectPool.ReleaseAll();
            BattleRenderer.Instance.IsOnBattle = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            Widget.Find<ArenaBoard>().ShowAsync().Forget();
            _isPlaying = false;
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
            Character.ArenaCharacter target = affectedCharacter.Id == me.Id ? me : enemy;
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
            yield return null;
        }

        public IEnumerator CoCustomEvent(ArenaCharacter caster, ArenaEventBase eventBase)
        {
            if (eventBase is ArenaTick tick)
            {
                var affectedCharacter = caster.Id == me.Id ? me : enemy;
                if (tick.SkillId == AuraIceShield.FrostBiteId)
                {
                    if (!caster.Buffs.TryGetValue(AuraIceShield.FrostBiteId, out var frostBite))
                    {
                        yield break;
                    }

                    var sourceCharacter = caster.Id == me.Id ? enemy : me;
                    IEnumerator CoFrostBite(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
                    {
                        sourceCharacter.CustomEvent(AuraIceShield.FrostBiteId);
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
