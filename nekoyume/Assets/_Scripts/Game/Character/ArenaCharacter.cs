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
using UnityEngine.Rendering;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class ArenaCharacter : BaseCharacter
    {
        private const float AnimatorTimeScale = 1.2f;

        [SerializeField]
        private CharacterAppearance appearance;

        [SerializeField]
        private bool shouldContainHUD = true;

        public GameObject attackPoint;
        public SortingGroup sortingGroup;

        private Root _root;
        private int _currentHp;

        protected float RunSpeedDefault => _characterModel.RunSpeed;
        private Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        private Vector3 HudTextPosition => transform.TransformPoint(0f, 1.7f, 0f);

        private float AttackRange => _characterModel.AttackRange;

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
        private float RunSpeed { get; set; }
        private HudContainer HudContainer { get; set; }
        private ProgressBar CastingBar { get; set; }
        private SpeechBubble SpeechBubble { get; set; }

        private bool CanRun
        {
            get
            {
                if (IsDead)
                {
                    return false;
                }

                return !Mathf.Approximately(RunSpeed, 0f);
            }
        }


        private Vector3 HitPointLocalOffset { get; set; }

        private Model.ArenaCharacter _characterModel;
        private ArenaCharacter _target;
        private HpBar _hpBar;
        private ArenaBattle _arenaBattle;
        private ArenaActionParams _runningAction;
        private bool _forceQuit;

        private readonly List<Costume> _costumes = new List<Costume>();
        private readonly List<Equipment> _equipments = new List<Equipment>();

        public List<ArenaActionParams> Actions { get; } = new List<ArenaActionParams>();

        protected void Awake()
        {
#if !UNITY_EDITOR
            attackPoint.SetActive(false);
#endif

            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
        }

        protected void Start()
        {
            InitializeHudContainer();
        }

        protected void OnDisable()
        {
            RunSpeed = 0.0f;
            _root = null;
            Actions.Clear();
            _runningAction = null;
            DisableHUD();
        }

        private void Update()
        {
            _root?.Tick();
            if (HudContainer)
            {
                HudContainer.UpdateAlpha(appearance.SpineController.SkeletonAnimation.skeleton.A);
            }
        }

        private void LateUpdate()
        {
            if (HudContainer)
            {
                HudContainer.UpdatePosition(gameObject, HUDOffset);
            }

            if (SpeechBubble)
            {
                SpeechBubble.UpdatePosition(gameObject, HUDOffset);
            }
        }

        private void InitializeHudContainer()
        {
            // No pooling. Widget.Create<HudContainer> didn't pooling HUD object.
            // HUD Pooling causes HUD positioning bug.
            if (!HudContainer && shouldContainHUD)
            {
                HudContainer = Widget.Create<HudContainer>(true);
            }
        }

        public void Init(ArenaPlayerDigest digest, ArenaCharacter target)
        {
            _costumes.Clear();
            _costumes.AddRange(digest.Costumes);
            _equipments.Clear();
            _equipments.AddRange(digest.Equipments);
            _target = target;

            appearance.Set(digest, Animator, HudContainer);

            InitializeHudContainer();
        }

        public void StartRun(Model.ArenaCharacter model)
        {
            _characterModel = model;
            Id = _characterModel.Id;
            SizeType = _characterModel.SizeType;
            CurrentHp = Hp;
            RunSpeed = _characterModel.IsEnemy ? -_characterModel.RunSpeed : _characterModel.RunSpeed;

            if (_root == null)
            {
                InitBT();
            }
        }

        public void UpdateStatusUI()
        {
            if (!Game.instance.IsInWorld)
                return;

            if (_hpBar == null)
            {
                _hpBar = Widget.Create<HpBar>(true);
                _hpBar.transform.SetParent(HudContainer.transform);
                _hpBar.transform.localPosition = Vector3.zero;
                _hpBar.transform.localScale = Vector3.one;
                HudContainer.UpdateAlpha(1);
            }

            if (_arenaBattle == null)
            {
                _arenaBattle = Widget.Find<ArenaBattle>();
            }

            HudContainer.UpdatePosition(gameObject, HUDOffset);
            _hpBar.Set(CurrentHp, _characterModel.AdditionalHP, Hp);
            _hpBar.SetBuffs(_characterModel.Buffs);
            _hpBar.SetLevel(_characterModel.Level);

            _arenaBattle.MyStatus.SetHp(CurrentHp, Hp);
            _arenaBattle.MyStatus.SetBuff(_characterModel.Buffs);
        }

        public void RemoveBuff()
        {
            if(_hpBar.HpVFX != null)
            {
                _hpBar.HpVFX.Stop();
            }
        }

        public void ShowSpeech(string key, params int[] list)
        {
            if (ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.enable = true;

            if (SpeechBubble.gameObject.activeSelf)
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

            if (!SpeechBubble.SetKey(key))
            {
                return;
            }

            if (!gameObject.activeSelf)
                return;

            StartCoroutine(SpeechBubble.CoShowText());
            return;
        }

        private IEnumerator CoProcessDamage(
            Model.BattleStatus.Skill.SkillInfo info,
            bool isConsiderDie,
            bool isConsiderElementalType)
        {
            var dmg = info.Effect;
            var position = HudTextPosition;
            var force = DamageTextForce;

            // damage 0 = dodged.
            if (dmg <= 0)
            {
                var index = 0;
                if (this is Enemy)
                {
                    index = 1;
                }

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
            position.x += Time.deltaTime * RunSpeed;
            transform.position = position;
        }

        private void StopRun()
        {
            RunSpeed = 0.0f;
            Animator.StopRun();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(gameObject.tag))
                return;

            var character = other.gameObject.GetComponent<ArenaCharacter>();
            if (!character)
                return;

            StopRunIfTargetInAttackRange(character);
        }


        private void StopRunIfTargetInAttackRange(ArenaCharacter target)
        {
            if (target.IsDead)
                return;

            StartCoroutine(CoStop(target));
            StopRun();
        }

        private IEnumerator CoStop(ArenaCharacter target)
        {
            yield return new WaitUntil(() => IsDead || target.IsDead);
        }

        #endregion

        public virtual float CalculateRange(ArenaCharacter target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return attackRangeStartPosition - targetHitPosition;
        }

        // public bool TargetInAttackRange(ArenaCharacter target)
        // {
        //     var diff = CalculateRange(target);
        //     return AttackRange > diff;
        // }

        private void DisableHUD()
        {
            // No pooling. HUD Pooling causes HUD positioning bug.
            if (_hpBar)
            {
                Destroy(_hpBar.gameObject);
                _hpBar = null;
            }

            // No pooling. HUD Pooling causes HUD positioning bug.
            if (HudContainer)
            {
                Destroy(HudContainer.gameObject);
                HudContainer = null;
            }

            if (!ReferenceEquals(CastingBar, null))
            {
                Destroy(CastingBar.gameObject);
                CastingBar = null;
            }

            if (!ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble.StopAllCoroutines();
                SpeechBubble.gameObject.SetActive(false);
                Destroy(SpeechBubble.gameObject, SpeechBubble.destroyTime);
                SpeechBubble = null;
            }
        }

        private void ProcessAttack(
            ArenaCharacter target,
            Model.BattleStatus.Skill.SkillInfo skill,
            bool isLastHit,
            bool isConsiderElementalType)
        {
            if (!target) return;
            target.StopRun();
            StartCoroutine(target.CoProcessDamage(skill, isLastHit, isConsiderElementalType));
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
            RunSpeed = 0.0f;
        }

        private IEnumerator CoTimeOut()
        {
            yield return new WaitForSeconds(5f);
            _forceQuit = true;
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
            while (true)
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

                _forceQuit = false;
                var coroutine = StartCoroutine(CoTimeOut());
                yield return new WaitUntil(() =>
                    AttackEndCalled ||
                    _forceQuit ||
                    Animator.IsIdle());
                StopCoroutine(coroutine);
                if (_forceQuit)
                {
                    continue;
                }
                break;
            }
        }

        private IEnumerator CoAnimationCastAttack(bool isCritical)
        {
            while (true)
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

                _forceQuit = false;
                var coroutine = StartCoroutine(CoTimeOut());
                yield return new WaitUntil(() =>
                    AttackEndCalled ||
                    _forceQuit ||
                    Animator.IsIdle());
                StopCoroutine(coroutine);
                if (_forceQuit)
                {
                    continue;
                }
                break;
            }
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
            var battleWidget = Widget.Find<Nekoyume.UI.Battle>();

            yield return StartCoroutine(
                CoAnimationAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                if (info.Target is Model.ArenaCharacter arenaCharacter)
                {
                    var target = info.Target.Id == Id ? this : _target;
                    ProcessAttack(target, info, arenaCharacter.IsDead, false);
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
                    ProcessAttack(target, info, arenaCharacter.IsDead, true);
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

                ProcessAttack(target, info, !first, true);
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
                    ProcessAttack(target, info, true, true);
                    if (info.ElementalType != ElementalType.Fire
                        && info.ElementalType != ElementalType.Water)
                    {
                        effect.StopLoop();
                    }

                    yield return new WaitUntil(() => effect.last.isStopped);
                }
                else
                {
                    ProcessAttack(target, info, isTriggerOn, isTriggerOn);
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
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> enumerable,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffInfos1,
            Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> coNormalAttack)
        {
            ArenaCharacter = arenaCharacter;
            skillInfos = enumerable;
            buffInfos = buffInfos1;
            func = coNormalAttack;
        }
    }
}
