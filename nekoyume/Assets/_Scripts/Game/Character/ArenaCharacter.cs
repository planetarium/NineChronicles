using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Arena;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class ArenaCharacter : BaseCharacter
    {
        [SerializeField]
        private CharacterAppearance appearance;

        private static readonly WaitForSeconds AttackTimeOut = new WaitForSeconds(999f);
        private const float AnimatorTimeScale = 1.2f;
        private Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        private Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        private Model.ArenaCharacter _characterModel;
        private ArenaCharacter _target;
        private ArenaBattle _arenaBattle;
        private ArenaActionParams _runningAction;
        private HudContainer _hudContainer;
        private SpeechBubble _speechBubble;
        private Root _root;
        private float _runSpeed;
        private int _currentHp;
        private bool _forceStop;

        private readonly List<Costume> _costumes = new List<Costume>();
        private readonly List<Equipment> _equipments = new List<Equipment>();


        private int Hp => _characterModel.HP;
        private int CurrentHp
        {
            get => _currentHp;
            set
            {
                _currentHp = Mathf.Clamp(value, 0, Hp);
                UpdateStatusUI();
            }
        }
        private bool IsDead => CurrentHp <= 0;
        private bool CanRun
        {
            get
            {
                if (IsDead || _forceStop)
                {
                    return false;
                }

                return !Mathf.Approximately(_runSpeed, 0f);
            }
        }

        public List<ArenaActionParams> Actions { get; } = new List<ArenaActionParams>();

        private void Awake()
        {
            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
        }

        private void OnDisable()
        {
            _runSpeed = 0.0f;
            _root = null;
            Actions.Clear();
            _runningAction = null;
            DisableHUD();
        }

        private void Update()
        {
            _root?.Tick();
        }

        private void LateUpdate()
        {
            _hudContainer.UpdatePosition(gameObject, HUDOffset);
            _speechBubble.UpdatePosition(gameObject, HUDOffset);
        }

        public void Init(ArenaPlayerDigest digest, ArenaCharacter target)
        {
            _hudContainer ??= Widget.Create<HudContainer>(true);
            _speechBubble ??= Widget.Create<SpeechBubble>();

            _costumes.Clear();
            _costumes.AddRange(digest.Costumes);
            _equipments.Clear();
            _equipments.AddRange(digest.Equipments);
            _target = target;
            appearance.Set(digest, Animator, _hudContainer);
        }

        public void StartRun(Model.ArenaCharacter model)
        {
            _characterModel = model;
            Id = _characterModel.Id;
            SizeType = _characterModel.SizeType;
            CurrentHp = Hp;
            _runSpeed = _characterModel.IsEnemy ? -_characterModel.RunSpeed : _characterModel.RunSpeed;

            Invoke(nameof(Test) , 0.5f);
            if (_root == null)
            {
                InitBT();
            }
        }

        private void Test()
        {
            _forceStop = true;
        }

        public void UpdateStatusUI()
        {
            if (!Game.instance.IsInWorld)
                return;

            _hudContainer.UpdatePosition(gameObject, HUDOffset);

            if (_arenaBattle == null)
            {
                _arenaBattle = Widget.Find<ArenaBattle>();
            }

            _arenaBattle.UpdateStatus(_characterModel.IsEnemy, CurrentHp, Hp, _characterModel.Buffs);
        }

        public void ShowSpeech(string key, params int[] list)
        {
            _speechBubble.enable = true;

            if (_speechBubble.gameObject.activeSelf)
            {
                return;
            }

            if (list.Length > 0)
            {
                var join = string.Join("_", list.Select(x => x.ToString()).ToArray());
                key = $"{key}_{join}_";
            }
            else
            {
                key = $"{key}_";
            }

            if (!_speechBubble.SetKey(key))
            {
                return;
            }

            if (!gameObject.activeSelf)
                return;

            StartCoroutine(_speechBubble.CoShowText());
        }

        private IEnumerator CoProcessDamage(Model.BattleStatus.Skill.SkillInfo info, bool isConsiderElementalType)
        {
            var dmg = info.Effect;
            var position = HudTextPosition;
            var force = DamageTextForce;

            if (dmg <= 0)
            {
                var index = _characterModel.IsEnemy ? 1 : 0;
                MissText.Show(position, force, index);
                yield break;
            }

            CurrentHp -= dmg;
            Animator.Hit();

            PopUpDmg(position, force, info, isConsiderElementalType);
        }

        private void PopUpDmg(
            Vector3 position,
            Vector3 force,
            Model.BattleStatus.Skill.SkillInfo info,
            bool isConsiderElementalType)
        {
            var dmg = info.Effect.ToString();
            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            var group = DamageText.TextGroupState.Basic;
            if (this is Player)
            {
                group = DamageText.TextGroupState.Damage;
            }

            if (info.Critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg, group);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged(isConsiderElementalType
                    ? info.ElementalType
                    : ElementalType.Normal);
                DamageText.Show(position, force, dmg, group);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        #region Run

        private void InitBT()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(() => CanRun).OpenBranch(
                        BT.Call(ExecuteRun)
                    ),
                    BT.If(() => !CanRun).OpenBranch(
                        BT.Sequence().OpenBranch(
                            BT.Call(StopRun),
                            BT.If(() => Actions.Any()).OpenBranch(
                                BT.Call(ExecuteAction)
                            )
                        )
                    )
                )
            );
        }

        protected virtual void ExecuteRun()
        {
            Animator.Run();
            Vector2 position = transform.position;
            position.x += Time.deltaTime * _runSpeed;
            transform.position = position;
        }

        private void StopRun()
        {
            _runSpeed = 0.0f;
            Animator.StopRun();
        }

        #endregion

        private void DisableHUD()
        {
            _speechBubble.StopAllCoroutines();
            _speechBubble.gameObject.SetActive(false);
        }

        private void ProcessAttack(
            ArenaCharacter target,
            Model.BattleStatus.Skill.SkillInfo skill,
            bool isConsiderElementalType)
        {
            if (target == null)
            {
                return;
            }

            StartCoroutine(target.CoProcessDamage(skill, isConsiderElementalType));
        }

        private void ProcessHeal(
            ArenaCharacter target,
            Model.BattleStatus.Skill.SkillInfo info)
        {
            if (target && !target.IsDead)
            {
                target.CurrentHp = Math.Min(target.CurrentHp + info.Effect, target.Hp);

                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var txt = info.Effect.ToString();
                PopUpHeal(position, force, txt, info.Critical);
            }
        }

        private void ProcessBuff(ArenaCharacter target, Model.BattleStatus.Skill.SkillInfo info)
        {
            if (target && !target.IsDead)
            {
                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var buff = info.Buff;
                var effect = Game.instance.Arena.BuffController.Get<BuffVFX>(target, buff);
                effect.Play();
                target.UpdateStatusUI();
            }
        }

        private void PopUpHeal(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            DamageText.Show(position, force, dmg, DamageText.TextGroupState.Heal);
            VFXController.instance.CreateAndChase<BattleHeal01VFX>(transform, HealOffset);
        }

        #region Animation

        private void PreAnimationForTheKindOfAttack()
        {
            AttackEndCalled = false;
            _runSpeed = 0.0f;
        }



        private void ShowCutscene()
        {
            if (_costumes.Exists(x => x.ItemSubType == ItemSubType.FullCostume))
            {
                return;
            }

            var armor = _equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            AreaAttackCutscene.Show(armor?.Id ?? GameConfig.DefaultAvatarArmorId);
        }

        private IEnumerator CoAnimationAttack(bool isCritical)
        {
            PreAnimationForTheKindOfAttack();
            if (isCritical)
            {
                Animator.CriticalAttack();
            }
            else
            {
                Animator.Attack();
            }

            yield return new WaitUntil(CheckAttackEnd);
        }

        private bool CheckAttackEnd()
        {
            return AttackEndCalled || Animator.IsIdle();
        }

        private IEnumerator CoAnimationCastAttack(bool isCritical)
        {
            PreAnimationForTheKindOfAttack();
            if (isCritical)
            {
                Animator.CriticalAttack();
            }
            else
            {
                Animator.CastAttack();
            }

            yield return new WaitUntil(CheckAttackEnd);
        }

        private IEnumerator CoAnimationCastBlow(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> infos)
        {
            var info = infos.First();
            var copy = new Model.BattleStatus.Skill.SkillInfo(info.Target, info.Effect,
                info.Critical, info.SkillCategory,
                info.WaveTurn, ElementalType.Normal, info.SkillTargetType, info.Buff);
            yield return StartCoroutine(CoAnimationCast(copy));

            var pos = transform.position;
            yield return CoAnimationCastAttack(infos.Any(skillInfo => skillInfo.Critical));
            var effect = Game.instance.Arena.SkillController.GetBlowCasting(
                pos,
                info.SkillCategory,
                info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.2f);
        }

        protected virtual IEnumerator CoAnimationCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Arena.SkillController.Get(pos, info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CoAnimationBuffCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Arena.BuffController.Get(pos, info.Buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }
        #endregion

        #region Skill

        public IEnumerator CoNormalAttack(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;
            var battleWidget = Widget.Find<ArenaBattle>();

            yield return StartCoroutine(
                CoAnimationAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                if (info.Target is Model.ArenaCharacter arenaCharacter)
                {
                    var target = info.Target.Id == Id ? this : _target;
                    ProcessAttack(target, info, false);
                    if (!arenaCharacter.IsEnemy)
                    {
                        battleWidget.ShowComboText(info.Effect > 0);
                    }
                }
            }
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            if (skillInfos.First().SkillTargetType == SkillTargetType.Enemy)
            {
                yield return StartCoroutine(CoAnimationCast(skillInfos.First()));
                yield return StartCoroutine(
                    CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));
            }
            else
            {
                yield return StartCoroutine(CoAnimationCastBlow(skillInfos));
            }

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                var effect = Game.instance.Arena.SkillController.Get<SkillBlowVFX>(target, info);
                if (effect is null)
                    continue;

                effect.Play();
                if (info.Target is Model.ArenaCharacter arenaCharacter)
                {
                    ProcessAttack(target, info, true);
                }
            }
        }

        public IEnumerator CoDoubleAttack(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                var first = skillInfosFirst == info;
                var effect = Game.instance.Arena.SkillController.Get<SkillDoubleVFX>(target, info);
                if (effect is null)
                    continue;

                yield return StartCoroutine(CoAnimationAttack(info.Critical));
                if (first)
                {
                    effect.FirstStrike();
                }
                else
                {
                    effect.SecondStrike();
                }

                ProcessAttack(target, info, true);
            }
        }

        public IEnumerator CoAreaAttack(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            ShowCutscene();
            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            var effectTarget = skillInfosFirst.Target.Id == Id ? this : _target;
            var effect = Game.instance.Arena.SkillController.Get<SkillAreaVFX>(effectTarget,
                    skillInfosFirst);
            if (effect is null)
                yield break;

            Model.BattleStatus.Skill.SkillInfo trigger = null;
            if (effect.finisher)
            {
                var count = FindObjectsOfType(effectTarget.GetType()).Length;
                trigger = skillInfos.Skip(skillInfosCount - count).First();
            }

            effect.Play();
            yield return new WaitForSeconds(0.5f);

            var isTriggerOn = false;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;

                yield return new WaitForSeconds(0.14f);
                if (trigger == info)
                {
                    isTriggerOn = true;

                    if (!info.Critical)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }

                    if (info.ElementalType == ElementalType.Fire)
                    {
                        effect.StopLoop();
                        yield return new WaitForSeconds(0.1f);
                    }

                    var coroutine = StartCoroutine(CoAnimationCastAttack(info.Critical));
                    if (info.ElementalType == ElementalType.Water)
                    {
                        yield return new WaitForSeconds(0.1f);
                        effect.StopLoop();
                    }

                    yield return coroutine;
                    effect.Finisher();
                    ProcessAttack(target, info,  true);
                    if (info.ElementalType != ElementalType.Fire
                        && info.ElementalType != ElementalType.Water)
                    {
                        effect.StopLoop();
                    }

                    yield return new WaitUntil(() => effect.last.isStopped);
                }
                else
                {
                    ProcessAttack(target, info, isTriggerOn);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator CoHeal(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                ProcessHeal(target, info);
            }

            Animator.Idle();
        }

        public IEnumerator CoBuff(IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationBuffCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                ProcessBuff(target, info);
            }

            Animator.Idle();
        }

        #endregion

        private void ExecuteAction()
        {
            StartCoroutine(CoExecuteAction());
        }

        private IEnumerator CoExecuteAction()
        {
            if (_runningAction is null)
            {
                _runningAction = Actions.First();

                var arena = Game.instance.Arena;
                var waitSeconds = StageConfig.instance.actionDelay;

                foreach (var info in _runningAction.skillInfos)
                {
                    if (info.Target is Model.ArenaCharacter arenaCharacter)
                    {
                        if (arenaCharacter.IsDead)
                        {
                            var target = info.Target.Id == Id ? this : _target;
                            if (target.Actions.Any())
                            {
                                var time = Time.time;
                                yield return new WaitWhile(() =>
                                    Time.time - time > 10f || target.Actions.Any());
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(waitSeconds);
                var coroutine = StartCoroutine(arena.CoSkill(_runningAction));
                yield return coroutine;
                Actions.Remove(_runningAction);
                _runningAction = null;
            }
        }

        public void Dead()
        {
            StartCoroutine(Dying());
        }

        private IEnumerator Dying()
        {
            yield return new WaitWhile(() => Actions.Any());
            OnDeadStart();
            yield return new WaitForSeconds(.2f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDeadEnd();
        }

        private void OnDeadStart()
        {
            _hudContainer.UpdateAlpha(0);
            Animator.Die();
        }

        private void OnDeadEnd()
        {
            Animator.Idle();
            gameObject.SetActive(false);
            Actions.Clear();
        }
    }

    public class ArenaActionParams
    {
        public readonly ArenaCharacter ArenaCharacter;
        public readonly IEnumerable<Model.BattleStatus.Skill.SkillInfo> skillInfos;
        public readonly IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffInfos;
        public readonly Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> func;

        public ArenaActionParams(ArenaCharacter arenaCharacter,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> skills,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffs,
            Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> coNormalAttack)
        {
            ArenaCharacter = arenaCharacter;
            skillInfos = skills;
            buffInfos = buffs;
            func = coNormalAttack;
        }
    }
}
