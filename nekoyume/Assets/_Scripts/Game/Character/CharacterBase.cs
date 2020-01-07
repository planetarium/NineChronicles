using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.UI;
using UnityEngine;
using UniRx;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterBase : MonoBehaviour
    {
        protected const float AnimatorTimeScale = 1.2f;

        public GameObject attackPoint;

        private bool _applicationQuitting = false;
        private Root _root;
        private int _currentHp;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public Model.CharacterBase Model { get; private set; }

        public readonly Subject<CharacterBase> OnUpdateHPBar = new Subject<CharacterBase>();

        protected abstract float RunSpeedDefault { get; }
        protected abstract Vector3 DamageTextForce { get; }
        protected abstract Vector3 HudTextPosition { get; }

        public string TargetTag { get; protected set; }

        public Guid Id => Model.Id;
        public SizeType SizeType => Model.SizeType;
        private float AttackRange => Model.attackRange;

        public int Level
        {
            get => Model.Stats.Level;
            set => Model.Stats.SetLevel(value);
        }

        public int HP => Model.HP;

        public int CurrentHP
        {
            get => _currentHp;
            private set
            {
                _currentHp = Math.Min(Math.Max(value, 0), HP);
                UpdateHpBar();
                
//                if (Animator?.Target != null)
//                {
//                    Debug.LogWarning($"{Animator.Target.name}'s {nameof(CurrentHP)} setter called: {CurrentHP}({Model.Stats.CurrentHP}) / {HP}({Model.Stats.LevelStats.HP}+{Model.Stats.BuffStats.HP})");
//                }
            }
        }

        protected bool IsDead => CurrentHP <= 0;

        public bool IsAlive => !IsDead;

        public float RunSpeed { get; set; }

        public HpBar HPBar { get; private set; }
        private ProgressBar CastingBar { get; set; }
        protected SpeechBubble SpeechBubble { get; set; }

        public ICharacterAnimator Animator { get; protected set; }
        protected Vector3 HUDOffset => Animator.GetHUDPosition();
        public bool AttackEndCalled { get; private set; }

        private bool _forceQuit = false;
        protected virtual bool CanRun => !Mathf.Approximately(RunSpeed, 0f);

        protected BoxCollider HitPointBoxCollider { get; private set; }
        protected Vector3 HitPointLocalOffset { get; set; }

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

            Event.OnAttackEnd.AddListener(AttackEnd);
        }

        protected virtual void OnDisable()
        {
            RunSpeed = 0.0f;
            _root = null;
            if (!_applicationQuitting)
                DisableHUD();
        }

        #endregion

        public virtual void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = model;

            if (updateCurrentHP)
            {
                CurrentHP = HP;
            }
        }

        protected virtual IEnumerator Dying()
        {
            StopRun();
            Animator.Die();
            yield return new WaitForSeconds(.2f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDead();
        }

        protected virtual void Update()
        {
            _root?.Tick();
            if (HPBar)
            {
                HPBar.UpdatePosition(gameObject, HUDOffset);
            }

            if (SpeechBubble)
            {
                SpeechBubble.UpdatePosition(gameObject, HUDOffset);
            }
        }

        public virtual void UpdateHpBar()
        {
            if (!Game.instance.Stage.IsInStage)
                return;

            if (!HPBar)
            {
                HPBar = Widget.Create<HpBar>(true);
            }

            HPBar.UpdatePosition(gameObject, HUDOffset);
            HPBar.Set(CurrentHP, Model.Stats.BuffStats.HP, HP);
            HPBar.SetBuffs(Model.Buffs);
            HPBar.SetLevel(Level);

            OnUpdateHPBar.OnNext(this);
        }

        public bool ShowSpeech(string key, params int[] list)
        {
            if (ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble = Widget.Create<SpeechBubble>();
            }

            if (SpeechBubble.gameObject.activeSelf)
            {
                return false;
            }

            if (list.Length > 0)
            {
                string join = string.Join("_", list.Select(x => x.ToString()).ToArray());
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

        protected virtual IEnumerator CoProcessDamage(Model.Skill.SkillInfo info, bool isConsiderDie,
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
            if (isConsiderDie && IsDead)
            {
                StartCoroutine(Dying());
            }
            else
            {
                Animator.Hit();
            }

            PopUpDmg(position, force, info, isConsiderElementalType);
        }

        protected virtual void OnDead()
        {
            Animator.Idle();
            gameObject.SetActive(false);
        }

        protected void PopUpDmg(Vector3 position, Vector3 force, Model.Skill.SkillInfo info,
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
                throw new NullReferenceException($"{nameof(GetAnimatorHitPointBoxCollider)}() returns null.");

            var scale = Animator.Target.transform.localScale;
            var center = source.center;
            var size = source.size;
            HitPointBoxCollider.center = new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            HitPointBoxCollider.size = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
        }

        protected abstract BoxCollider GetAnimatorHitPointBoxCollider();

        #endregion

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
                        BT.Call(StopRun)
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

        private void ExecuteRun()
        {
            Animator.Run();

            Vector2 position = transform.position;
            position.x += Time.deltaTime * RunSpeed;
            transform.position = position;
        }

        protected void StopRun()
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

            StopRun();
        }

        #endregion

        public bool TargetInAttackRange(CharacterBase target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return AttackRange > Mathf.Abs(targetHitPosition - attackRangeStartPosition);
        }

        private void AttackEnd(CharacterBase character)
        {
            if (ReferenceEquals(character, this))
                AttackEndCalled = true;
        }

        public void DisableHUD()
        {
            if (HPBar)
            {
                Destroy(HPBar.gameObject);
                HPBar = null;
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

        protected virtual void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill, bool isLastHit,
            bool isConsiderElementalType)
        {
            if (!target) return;
            target.StopRun();
            StartCoroutine(target.CoProcessDamage(skill, isLastHit, isConsiderElementalType));
        }

        protected virtual void ProcessHeal(CharacterBase target, Model.Skill.SkillInfo info)
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

        private void ProcessBuff(CharacterBase target, Model.Skill.SkillInfo info)
        {
            if (target && target.IsAlive)
            {
                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var buff = info.Buff;
                var effect = Game.instance.Stage.buffController.Get<BuffVFX>(target, buff);
                effect.Play();
                target.UpdateHpBar();
//                Debug.LogWarning($"{Animator.Target.name}'s {nameof(ProcessBuff)} called: {CurrentHP}({Model.Stats.CurrentHP}) / {HP}({Model.Stats.LevelStats.HP}+{Model.Stats.BuffStats.HP})");
            }
        }

        private void PopUpHeal(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            DamageText.Show(position, force, dmg, DamageText.TextGroupState.Heal);
            VFXController.instance.Create<BattleHeal01VFX>(transform, HUDOffset - new Vector3(0f, 0.4f));
        }

        #region Animation

        private void PreAnimationForTheKindOfAttack()
        {
            AttackEndCalled = false;
            RunSpeed = 0.0f;
        }

        private IEnumerator CoTimeOut()
        {
            yield return new WaitForSeconds(2f);
            _forceQuit = true;
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

            _forceQuit = false;
            var coroutine = StartCoroutine(CoTimeOut());
            yield return new WaitUntil(() => AttackEndCalled || _forceQuit);
            StopCoroutine(coroutine);
            if (_forceQuit)
            {
                if (isCritical)
                {
                    Animator.CriticalAttack();
                }
                else
                {
                    Animator.Attack();
                }
            }
            PostAnimationForTheKindOfAttack();
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
            _forceQuit = false;
            var coroutine = StartCoroutine(CoTimeOut());
            yield return new WaitUntil(() => AttackEndCalled || _forceQuit);
            StopCoroutine(coroutine);
            if (_forceQuit)
            {
                if (isCritical)
                {
                    Animator.CriticalAttack();
                }
                else
                {
                    Animator.CastAttack();
                }
            }

            PostAnimationForTheKindOfAttack();
        }


        private IEnumerator CoAnimationCastBlow(IReadOnlyList<Model.Skill.SkillInfo> infos)
        {
            var info = infos.First();
            var copy = new Model.Skill.SkillInfo(info.Target, info.Effect, info.Critical, info.SkillCategory,
                ElementalType.Normal, info.SkillTargetType, info.Buff);
            yield return StartCoroutine(CoAnimationCast(copy));

            var pos = transform.position;
            yield return CoAnimationCastAttack(infos.Any(skillInfo => skillInfo.Critical));
            var effect = Game.instance.Stage.skillController.GetBlow(pos, info);
            effect.Play();
            yield return new WaitForSeconds(0.2f);

            PostAnimationForTheKindOfAttack();
        }

        protected virtual IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.skillController.Get(pos, info);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            PostAnimationForTheKindOfAttack();
        }

        private IEnumerator CoAnimationBuffCast(Model.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.buffController.Get(pos, info.Buff);
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

        public IEnumerator CoNormalAttack(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            yield return StartCoroutine(CoAnimationAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = Game.instance.Stage.GetCharacter(info.Target);
                ProcessAttack(target, info, info.Target.IsDead, false);
            }
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            if (skillInfos.First().SkillTargetType == SkillTargetType.Enemy)
            {
                yield return StartCoroutine(CoAnimationCast(skillInfos.First()));
                yield return StartCoroutine(CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));
            }
            else
            {
                yield return StartCoroutine(CoAnimationCastBlow(skillInfos));
            }

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = Game.instance.Stage.GetCharacter(info.Target);
                var effect = Game.instance.Stage.skillController.Get<SkillBlowVFX>(target, info);
                effect.Play();
                ProcessAttack(target, info, info.Target.IsDead, true);
            }
        }

        public IEnumerator CoDoubleAttack(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
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
                var first = skillInfosFirst == info;
                var effect = Game.instance.Stage.skillController.Get<SkillDoubleVFX>(target, info);

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

        public IEnumerator CoAreaAttack(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            var effectTarget = Game.instance.Stage.GetCharacter(skillInfosFirst.Target);
            var effect = Game.instance.Stage.skillController.Get<SkillAreaVFX>(effectTarget, skillInfosFirst);
            Model.Skill.SkillInfo trigger = null;
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

        public IEnumerator CoHeal(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
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

        public IEnumerator CoBuff(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
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
    }
}
