using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;
using ArenaCharacter = Nekoyume.Model.ArenaCharacter;

namespace Nekoyume.Game
{
    using UniRx;

    public class Arena : MonoBehaviour, IArena
    {
        [SerializeField]
        private ObjectPool objectPool;

        [SerializeField]
        private ArenaBackground background;

        [SerializeField]
        private Character.ArenaCharacter me;

        [SerializeField]
        private Character.ArenaCharacter enemy;

        public readonly ISubject<Stage> OnRoomEnterEnd = new Subject<Stage>();
        public IObservable<Arena> OnArenaEnd => _onArenaEnd;
        private readonly ISubject<Arena> _onArenaEnd = new Subject<Arena>();
        private const float SkillDelay = 0.1f;
        private Coroutine _battleCoroutine;
        private int _turnNumber;
        private bool _isPlaying;

        public SkillController SkillController { get; private set; }
        public BuffController BuffController { get; private set; }
        public bool IsAvatarStateUpdatedAfterBattle { get; set; }

        public void Initialize()
        {
            objectPool.Initialize();
            SkillController = new SkillController(objectPool);
            BuffController = new BuffController(objectPool);
        }

        public IEnumerator CoSkill(ArenaActionParams param)
        {
            var infos = param.skillInfos.ToList();
            var turn = infos.First().WaveTurn;
            var time = Time.time;
            yield return new WaitUntil(() => Time.time - time > 5f || _turnNumber == turn);

            yield return StartCoroutine(param.func(infos));

            param.ArenaCharacter.UpdateStatusUI();

            if (!(param.buffInfos is null))
            {
                foreach (var buffInfo in param.buffInfos)
                {
                    var target = buffInfo.Target.Id == me.Id ? me : enemy;
                    target.UpdateStatusUI();
                }
            }

            yield return new WaitForSeconds(SkillDelay);
        }

        public void Enter(
            BattleLog log,
            List<ItemBase> rewards,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest)
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

                if (log?.Count > 0)
                {
                    _battleCoroutine = StartCoroutine(CoEnter(log, rewards, myDigest, enemyDigest));
                }
            }
            else
            {
                Debug.Log("Skip incoming battle. Battle is already simulating.");
            }
        }

        private IEnumerator CoEnter(
            BattleLog log,
            IReadOnlyList<ItemBase> rewards,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest)
        {
            yield return StartCoroutine(CoStart(log, myDigest, enemyDigest));

            foreach (var e in log)
            {
                yield return StartCoroutine(e.CoExecute(this));
            }

            yield return StartCoroutine(CoEnd(log, rewards));
        }

        private IEnumerator CoStart(
            BattleLog log,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest)
        {
            background.gameObject.SetActive(true);
            background.Show(3.0f);
            me.Init(myDigest, enemy);
            enemy.Init(enemyDigest, me);
            _turnNumber = 1;

            Widget.Find<ArenaBattle>().Show(myDigest, enemyDigest);
            yield return new WaitForSeconds(2.0f);

            AudioController.instance.PlayMusic(AudioController.MusicCode.PVPBattle);
            Widget.Find<ArenaBattleLoadingScreen>().Close();
            Game.instance.IsInWorld = true;
        }

        private IEnumerator CoEnd(BattleLog log, IReadOnlyList<ItemBase> rewards)
        {
            IsAvatarStateUpdatedAfterBattle = false;
            ActionRenderHandler.Instance.Pending = false;
            _onArenaEnd.OnNext(this);

            yield return new WaitUntil(() => IsAvatarStateUpdatedAfterBattle);
            yield return new WaitWhile(() => me.Actions.Any());
            yield return new WaitWhile(() => enemy.Actions.Any());
            yield return new WaitForSeconds(0.75f);

            var arenaCharacter = log.result == BattleLog.Result.Win ? me : enemy;
            arenaCharacter.Animator.Win();
            arenaCharacter.ShowSpeech("PLAYER_WIN");
            Widget.Find<ArenaBattle>().Close();
            Widget.Find<RankingBattleResultPopup>().Show(log, rewards, OnEnd);
            yield return null;
        }

        private void OnEnd()
        {
            background.gameObject.SetActive(false);
            objectPool.ReleaseAll();
            Game.instance.IsInWorld = false;
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            Widget.Find<ArenaBoard>().Show(RxProps.ArenaParticipantsOrderedWithScore.Value);
        }

        public IEnumerator CoSpawnArenaPlayer(ArenaCharacter character)
        {
            if (character.IsEnemy)
            {
                enemy.StartRun(character);
            }
            else
            {
                me.StartRun(character);
                me.ShowSpeech("PLAYER_INIT");
            }
            yield return null;
        }

        public IEnumerator CoNormalAttack(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoNormalAttack);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoBlowAttack(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoBlowAttack);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoDoubleAttack(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoDoubleAttack);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoAreaAttack(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoAreaAttack);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoHeal(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoHeal);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoBuff(
            ICharacter caster,
            IEnumerable<Skill.SkillInfo> skillInfos,
            IEnumerable<Skill.SkillInfo> buffInfos)
        {
            var target = caster.Id == me.Id ? me : enemy;
            var actionParams = new ArenaActionParams(target, skillInfos, buffInfos, target.CoBuff);
            target.Actions.Add(actionParams);
            yield return null;
        }

        public IEnumerator CoRemoveBuffs(ICharacter caster)
        {
            var target = caster.Id == me.Id ? me : enemy;
            target.UpdateStatusUI();
            target.RemoveBuff();
            yield break;
        }

        public IEnumerator CoDead(ICharacter caster)
        {
            yield return new WaitWhile(() => me.Actions.Any());
            yield return new WaitWhile(() => enemy.Actions.Any());
            var target = caster.Id == me.Id ? me : enemy;
            target.Dead();
        }

        public IEnumerator CoArenaTurnEnd(int turnNumber)
        {
            _turnNumber = turnNumber;
            yield return null;
        }
    }
}
