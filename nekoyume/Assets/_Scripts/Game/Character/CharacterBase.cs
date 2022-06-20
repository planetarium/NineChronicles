using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.UI;
using UnityEngine;
using UniRx;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Character;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterBase : Character
    {
        protected const float AnimatorTimeScale = 1.2f;
        protected static readonly WaitForSeconds AttackTimeOut = new WaitForSeconds(5f);

        [SerializeField]
        private bool shouldContainHUD = true;

        public GameObject attackPoint;
        public SortingGroup sortingGroup;

        private bool _applicationQuitting = false;
        private Root _root;
        private int _currentHp;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public Model.CharacterBase CharacterModel { get; protected set; }

        public readonly Subject<CharacterBase> OnUpdateHPBar = new Subject<CharacterBase>();

        protected abstract float RunSpeedDefault { get; }
        protected abstract Vector3 DamageTextForce { get; }
        protected abstract Vector3 HudTextPosition { get; }
        public virtual string TargetTag { get; protected set; }

        public Guid Id => CharacterModel.Id;
        public SizeType SizeType => CharacterModel.SizeType;
        private float AttackRange => CharacterModel.attackRange;

        public int Level
        {
            get => CharacterModel.Level;
            set => CharacterModel.Level = value;
        }

        public int HP => CharacterModel.HP;

        public int CurrentHP
        {
            get => _currentHp;
            private set
            {
                _currentHp = Math.Min(Math.Max(value, 0), HP);
                UpdateHpBar();
            }
        }

        protected bool IsDead => CurrentHP <= 0;

        public bool IsAlive => !IsDead;

        public float RunSpeed { get; set; }

        public HpBar HPBar { get; private set; }
        public HudContainer HudContainer { get; private set; }
        private ProgressBar CastingBar { get; set; }
        protected SpeechBubble SpeechBubble { get; set; }


        protected virtual bool CanRun
        {
            get
            {
                if (_forceStop)
                {
                    return false;
                }

                return !Mathf.Approximately(RunSpeed, 0f);
            }
        }

        protected BoxCollider HitPointBoxCollider { get; private set; }
        public Vector3 HitPointLocalOffset { get; set; }

        public List<ActionParams> actions = new List<ActionParams>();

        public ActionParams action;


        private bool _forceStop = false;

        #region Mono

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        protected virtual void Awake()
        {
#if !UNITY_EDITOR
            attackPoint.SetActive(false);
#endif

            HitPointBoxCollider = GetComponent<BoxCollider>();
        }

        protected virtual void Start()
        {
            InitializeHudContainer();
        }

        protected virtual void OnDisable()
        {
            RunSpeed = 0.0f;
            _root = null;
            actions.Clear();
            action = null;
            if (!_applicationQuitting)
                DisableHUD();
            _forceStop = false;
        }

        #endregion

        public virtual void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            _disposablesForModel.DisposeAllAndClear();
            CharacterModel = model;
            InitializeHudContainer();
            if (updateCurrentHP)
            {
                CurrentHP = HP;
            }
        }

        protected virtual IEnumerator Dying()
        {
            yield return new WaitWhile(() => actions.Any());
            OnDeadStart();
            StopRun();
            Animator.Die();
            yield return new WaitForSeconds(.2f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDeadEnd();
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

        protected virtual void Update()
        {
            _root?.Tick();
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

        protected virtual void InitializeHpBar()
        {
            HPBar = Widget.Create<HpBar>(true);
            HPBar.transform.SetParent(HudContainer.transform);
            HPBar.transform.localPosition = Vector3.zero;
            HPBar.transform.localScale = Vector3.one;
        }

        public virtual void UpdateHpBar()
        {
            if (!Game.instance.IsInWorld)
                return;

            if (!HPBar)
            {
                InitializeHpBar();
                HudContainer.UpdateAlpha(1);
            }

            HudContainer.UpdatePosition(gameObject, HUDOffset);
            HPBar.Set(CurrentHP, CharacterModel.Stats.BuffStats.HP, HP);
            HPBar.SetBuffs(CharacterModel.Buffs);
            HPBar.SetLevel(Level);

            OnUpdateHPBar.OnNext(this);
        }

        public bool ShowSpeech(string key, params int[] list)
        {
            if (ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            SpeechBubble.enable = true;

            if (SpeechBubble.gameObject.activeSelf)
            {
                return false;
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
                return false;
            }

            if (!gameObject.activeSelf)
                return true;

            StartCoroutine(SpeechBubble.CoShowText());
            return true;
        }

        protected virtual IEnumerator CoProcessDamage(
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

            CurrentHP -= dmg;
            Animator.Hit();

            PopUpDmg(position, force, info, isConsiderElementalType);
        }

        protected virtual void OnDeadStart()
        {
        }

        protected virtual void OnDeadEnd()
        {
            Animator.Idle();
            gameObject.SetActive(false);
            actions.Clear();
        }

        protected void PopUpDmg(
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

        #region AttackPoint & HitPoint

        protected virtual void UpdateHitPoint()
        {
            var source = GetAnimatorHitPointBoxCollider();
            if (!source)
            {
                throw new NullReferenceException(
                    $"{nameof(GetAnimatorHitPointBoxCollider)}() returns null.");
            }

            var scale = Animator.Target.transform.localScale;
            var center = source.center;
            var size = source.size;
            HitPointBoxCollider.center =
                new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            HitPointBoxCollider.size =
                new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
        }

        protected abstract BoxCollider GetAnimatorHitPointBoxCollider();

        #endregion

        #region Run

        internal void InitBT()
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
                            BT.If(() => actions.Any()).OpenBranch(
                                BT.Call(ExecuteAction)
                            )
                        )
                    )
                )
            );
        }

        public void StartRun()
        {
            RunSpeed = RunSpeedDefault;
            if (_root == null)
            {
                InitBT();
            }
        }

        protected virtual void ExecuteRun()
        {
            Animator.Run();

            Vector2 position = transform.position;
            position.x += Time.deltaTime * RunSpeed;
            transform.position = position;
        }

        public void StopRun()
        {
            RunSpeed = 0.0f;
            Animator.StopRun();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag(TargetTag))
                return;

            var character = other.gameObject.GetComponent<CharacterBase>();
            if (!character)
                return;

            StopRunIfTargetInAttackRange(character);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.gameObject.CompareTag(TargetTag))
                return;

            var character = other.gameObject.GetComponent<CharacterBase>();
            if (!character)
                return;

            StopRunIfTargetInAttackRange(character);
        }

        private void StopRunIfTargetInAttackRange(CharacterBase target)
        {
            if (target.IsDead || !TargetInAttackRange(target))
                return;

            _forceStop = true;
            StartCoroutine(CoStop(target));
            StopRun();
        }

        private IEnumerator CoStop(CharacterBase target)
        {
            yield return new WaitUntil(() => IsDead || target.IsDead);
            _forceStop = false;
        }

        #endregion

        public virtual float CalculateRange(CharacterBase target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return attackRangeStartPosition - targetHitPosition;
        }

        public bool TargetInAttackRange(CharacterBase target)
        {
            var diff = CalculateRange(target);
            return AttackRange > diff;
        }

        public void DisableHUD()
        {
            // No pooling. HUD Pooling causes HUD positioning bug.
            if (HPBar)
            {
                Destroy(HPBar.gameObject);
                HPBar = null;
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

        public void DisableHudContainer()
        {
            if (HudContainer)
            {
                HudContainer.UpdateAlpha(0);
                HudContainer.gameObject.SetActive(false);
                HudContainer = null;
            }
        }

        protected virtual void ProcessAttack(
            CharacterBase target,
            Model.BattleStatus.Skill.SkillInfo skill,
            bool isLastHit,
            bool isConsiderElementalType)
        {
            if (!target) return;
            target.StopRun();
            StartCoroutine(target.CoProcessDamage(skill, isLastHit, isConsiderElementalType));
        }

        protected virtual void ProcessHeal(
            CharacterBase target,
            Model.BattleStatus.Skill.SkillInfo info)
        {
            if (target && target.IsAlive)
            {
                target.CurrentHP = Math.Min(target.CurrentHP + info.Effect, target.HP);

                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var txt = info.Effect.ToString();
                PopUpHeal(position, force, txt, info.Critical);
//                Debug.LogWarning($"{Animator.Target.name}'s {nameof(ProcessHeal)} called: {CurrentHP}({Model.Stats.CurrentHP}) / {HP}({Model.Stats.LevelStats.HP}+{Model.Stats.BuffStats.HP})");
            }
        }

        private void ProcessBuff(CharacterBase target, Model.BattleStatus.Skill.SkillInfo info)
        {
            if (target && target.IsAlive)
            {
                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var buff = info.Buff;
                var effect = Game.instance.Stage.BuffController.Get<BuffVFX>(target, buff);
                effect.Play();
                target.UpdateHpBar();
//                Debug.LogWarning($"{Animator.Target.name}'s {nameof(ProcessBuff)} called: {CurrentHP}({Model.Stats.CurrentHP}) / {HP}({Model.Stats.LevelStats.HP}+{Model.Stats.BuffStats.HP})");
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

        private bool CheckAttackEnd()
        {
            return AttackEndCalled || Animator.IsIdle();
        }

        protected virtual void ShowCutscene()
        {
            // Do nothing.
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

                yield return new WaitForEndOfFrame();
                yield return new WaitUntil(CheckAttackEnd);
                if (Animator.IsIdle())
                {
                    continue;
                }

                PostAnimationForTheKindOfAttack();
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

                yield return new WaitForEndOfFrame();
                yield return new WaitUntil(CheckAttackEnd);
                if (Animator.IsIdle())
                {
                    continue;
                }

                PostAnimationForTheKindOfAttack();
                break;
            }
        }


        private IEnumerator CoAnimationCastBlow(
            IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> infos)
        {
            var info = infos.First();
            var copy = new Model.BattleStatus.Skill.SkillInfo(info.Target, info.Effect,
                info.Critical, info.SkillCategory,
                info.WaveTurn, ElementalType.Normal, info.SkillTargetType, info.Buff);
            yield return StartCoroutine(CoAnimationCast(copy));

            var pos = transform.position;
            yield return CoAnimationCastAttack(infos.Any(skillInfo => skillInfo.Critical));
            var effect = Game.instance.Stage.SkillController.GetBlowCasting(
                pos,
                info.SkillCategory,
                info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.2f);

            PostAnimationForTheKindOfAttack();
        }

        protected virtual IEnumerator CoAnimationCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.SkillController.Get(pos, info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            PostAnimationForTheKindOfAttack();
        }

        private IEnumerator CoAnimationBuffCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.BuffController.Get(pos, info.Buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            PostAnimationForTheKindOfAttack();
        }

        private void PostAnimationForTheKindOfAttack()
        {
            var enemy = GetComponentsInChildren<CharacterBase>()
                .Where(c => c.gameObject.CompareTag(TargetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null && !TargetInAttackRange(enemy))
                RunSpeed = RunSpeedDefault;
        }

        #endregion

        #region Skill

        public IEnumerator CoNormalAttack(
            IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
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
                var target = Game.instance.Stage.GetCharacter(info.Target);
                ProcessAttack(target, info, info.Target.IsDead, false);
                if (this is Player && !(this is EnemyPlayer))
                    battleWidget.ShowComboText(info.Effect > 0);
            }
        }

        public IEnumerator CoBlowAttack(
            IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
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
                var target = Game.instance.Stage.GetCharacter(info.Target);
                if (target is null)
                    continue;

                var effect = Game.instance.Stage.SkillController.Get<SkillBlowVFX>(target, info);
                if (effect is null)
                    continue;

                effect.Play();
                ProcessAttack(target, info, info.Target.IsDead, true);
            }
        }

        public IEnumerator CoDoubleAttack(
            IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = Game.instance.Stage.GetCharacter(info.Target);
                if (target is null)
                    continue;

                var first = skillInfosFirst == info;
                var effect = Game.instance.Stage.SkillController.Get<SkillDoubleVFX>(target, info);
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

        public IEnumerator CoAreaAttack(
            IReadOnlyList<Model.BattleStatus.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            ShowCutscene();
            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            var effectTarget = Game.instance.Stage.GetCharacter(skillInfosFirst.Target);
            if (effectTarget is null)
                yield break;

            var effect =
                Game.instance.Stage.SkillController.Get<SkillAreaVFX>(effectTarget,
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
                var target = Game.instance.Stage.GetCharacter(info.Target);
                if (target is null)
                    continue;

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
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = Game.instance.Stage.GetCharacter(info.Target);
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
                var target = Game.instance.Stage.GetCharacter(info.Target);
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
            if (action is null)
            {
                action = actions.First();

                var stage = Game.instance.Stage;
                var waitSeconds = StageConfig.instance.actionDelay;

                foreach (var info in action.skillInfos)
                {
                    var target = info.Target;
                    if (target.IsDead)
                    {
                        var character = Game.instance.Stage.GetCharacter(target);
                        if (character)
                        {
                            if (character.actions.Any())
                            {
                                var time = Time.time;
                                yield return new WaitWhile(() =>
                                    Time.time - time > 10f || character.actions.Any());
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(waitSeconds);
                var coroutine = StartCoroutine(stage.CoSkill(action));
                yield return coroutine;
                actions.Remove(action);
                action = null;
                _forceStop = false;
            }
        }

        public void Dead()
        {
            StartCoroutine(Dying());
        }

        public void SetSortingLayer(int layerId, int orderInLayer = 0)
        {
            sortingGroup.sortingLayerID = layerId;
            sortingGroup.sortingOrder = orderInLayer;
        }

        public void Ready()
        {
            AttackEndCalled = false;
        }
    }

    public class ActionParams
    {
        public CharacterBase character;
        public IEnumerable<Model.BattleStatus.Skill.SkillInfo> skillInfos;
        public IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffInfos;
        public Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> func;

        public ActionParams(CharacterBase characterBase,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> enumerable,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffInfos1,
            Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> coNormalAttack)
        {
            character = characterBase;
            skillInfos = enumerable;
            buffInfos = buffInfos1;
            func = coNormalAttack;
        }
    }
}
