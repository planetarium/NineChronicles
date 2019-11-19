using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.EnumType;
using Nekoyume.Game.CC;
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

        private bool _applicationQuitting = false;
        private Root _root;
        private int _currentHp;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        
        public readonly ReactiveProperty<Model.CharacterBase> Model = new ReactiveProperty<Model.CharacterBase>();
        
        public readonly Subject<CharacterBase> OnUpdateHPBar = new Subject<CharacterBase>();
        
        protected abstract float RunSpeedDefault { get; }
        protected abstract Vector3 DamageTextForce { get; }
        protected abstract Vector3 HudTextPosition { get; }

        public string TargetTag { get; protected set; }

        public Guid Id => Model.Value.Id;
        public SizeType SizeType => Model.Value.SizeType;
        private float AttackRange => Model.Value.attackRange;

        public int Level
        {
            get => Model.Value.Stats.Level;
            set => Model.Value.Stats.SetLevel(value);
        }

        public int HP => Model.Value.HP;

        public int CurrentHP
        {
            get => _currentHp;
            private set
            {
                _currentHp = Math.Max(0, value);
                UpdateHpBar();
            }
        }

        public float RunSpeed { get; set; }

        public HpBar HPBar { get; private set; }
        private ProgressBar CastingBar { get; set; }
        protected SpeechBubble SpeechBubble { get; set; }

        public bool Rooted => gameObject.GetComponent<IRoot>() != null;

        public ICharacterAnimator Animator { get; protected set; }
        protected Vector3 HUDOffset => Animator.GetHUDPosition();
        public bool AttackEndCalled { get; private set; }

        #region Mono

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        protected virtual void Awake()
        {
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
            Model.SetValueAndForceNotify(model);

            if (updateCurrentHP)
            {
                CurrentHP = HP;
            }
        }

        public bool IsDead()
        {
            return CurrentHP <= 0;
        }

        public bool IsAlive()
        {
            return !IsDead();
        }

        protected float AttackSpeedMultiplier
        {
            get
            {
                var slows = GetComponents<ISlow>();
                var multiplierBySlow = slows.Select(slow => slow.AttackSpeedMultiplier).DefaultIfEmpty(1.0f).Min();
                return multiplierBySlow;
            }
        }

        protected float RunSpeedMultiplier
        {
            get
            {
                var slows = GetComponents<ISlow>();
                var multiplierBySlow = slows.Select(slow => slow.RunSpeedMultiplier).DefaultIfEmpty(1.0f).Min();
                return multiplierBySlow;
            }
        }

        private void Run()
        {
            if (Rooted)
            {
                Animator.StopRun();
                return;
            }

            Animator.Run();

            Vector2 position = transform.position;
            position.x += Time.deltaTime * RunSpeed * RunSpeedMultiplier;
            transform.position = position;
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
            if (!ReferenceEquals(HPBar, null))
            {
                HPBar.UpdatePosition(gameObject, HUDOffset);
            }

            if (!ReferenceEquals(SpeechBubble, null))
            {
                SpeechBubble.UpdatePosition(gameObject, HUDOffset);
            }
        }

        public virtual void UpdateHpBar()
        {
            if (!Game.instance.stage.IsInStage)
                return;
            
            if (!HPBar)
            {
                HPBar = Widget.Create<HpBar>(true);
                HPBar.SetLevel(Level);
            }

            HPBar.UpdatePosition(gameObject, HUDOffset);
            HPBar.Set(CurrentHP, Model.Value.Stats.BuffStats.HP, HP);
            HPBar.SetBuffs(Model.Value.Buffs);
            
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
            if (isConsiderDie && IsDead())
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
            if (info.Critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged(isConsiderElementalType
                    ? info.ElementalType
                    : ElementalType.Normal);
                DamageText.Show(position, force, dmg);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        private void InitBT()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(CanRun).OpenBranch(
                        BT.Call(Run)
                    ),
                    BT.If(() => !CanRun()).OpenBranch(
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

        protected virtual bool CanRun()
        {
            return !(Mathf.Approximately(RunSpeed, 0f));
        }

        private void AttackEnd(CharacterBase character)
        {
            if (ReferenceEquals(character, this))
                AttackEndCalled = true;
        }

        // FixMe. 캐릭터와 몬스터가 겹치는 현상 있음.
        public bool TargetInRange(CharacterBase target) =>
            AttackRange > Mathf.Abs(gameObject.transform.position.x - target.transform.position.x);

        public void StopRun()
        {
            RunSpeed = 0.0f;
            Animator.StopRun();
        }

        public void DisableHUD()
        {
            if (!ReferenceEquals(HPBar, null))
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
            if (target && target.IsAlive())
            {
                target.CurrentHP = Math.Min(info.Effect + target.CurrentHP, target.HP);

                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var txt = info.Effect.ToString();
                PopUpHeal(position, force, txt, info.Critical);
            }
        }

        private void ProcessBuff(CharacterBase target, Model.Skill.SkillInfo info)
        {
            if (target && target.IsAlive())
            {
                var position = transform.TransformPoint(0f, 1.7f, 0f);
                var force = new Vector3(-0.1f, 0.5f);
                var buff = info.Buff;
                var effect = Game.instance.stage.buffController.Get<BuffVFX>(target, buff);
                effect.Play();
                target.UpdateHpBar();
            }
        }

        private void PopUpHeal(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            DamageText.Show(position, force, dmg);
            VFXController.instance.Create<BattleHeal01VFX>(transform, HUDOffset - new Vector3(0f, 0.4f));
        }

        private void PreAnimationForTheKindOfAttack()
        {
            AttackEndCalled = false;
            RunSpeed = 0.0f;
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

            yield return new WaitUntil(() => AttackEndCalled);
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

            yield return new WaitUntil(() => AttackEndCalled);
            PostAnimationForTheKindOfAttack();
        }

        protected virtual IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            AudioController.instance.PlaySfx(AudioController.SfxCode.BattleCast);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.stage.skillController.Get(pos, info);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            PostAnimationForTheKindOfAttack();
        }

        private IEnumerator CoAnimationBuffCast(Model.Skill.SkillInfo info)
        {
            PreAnimationForTheKindOfAttack();

            AudioController.instance.PlaySfx(AudioController.SfxCode.BattleCast);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.stage.buffController.Get(pos, info.Buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            PostAnimationForTheKindOfAttack();
        }

        private void PostAnimationForTheKindOfAttack()
        {
            var enemy = GetComponentsInChildren<CharacterBase>()
                .Where(c => c.gameObject.CompareTag(TargetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null && !TargetInRange(enemy))
                RunSpeed = RunSpeedDefault;
        }

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
                var target = Game.instance.stage.GetCharacter(info.Target);
                ProcessAttack(target, info, i == skillInfosCount - 1, false);
            }
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            yield return StartCoroutine(CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = Game.instance.stage.GetCharacter(info.Target);
                var effect = Game.instance.stage.skillController.Get<SkillBlowVFX>(target, info);
                effect.Play();
                ProcessAttack(target, info, i == skillInfosCount - 1, true);
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
                var target = Game.instance.stage.GetCharacter(info.Target);
                var first = skillInfosFirst == info;
                var effect = Game.instance.stage.skillController.Get<SkillDoubleVFX>(target, info);

                yield return StartCoroutine(CoAnimationAttack(info.Critical));
                if (first)
                {
                    effect.FirstStrike();
                }
                else
                {
                    effect.SecondStrike();
                }

                ProcessAttack(target, info, i == skillInfosCount - 1, true);
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

            var effectTarget = Game.instance.stage.GetCharacter(skillInfosFirst.Target);
            var effect = Game.instance.stage.skillController.Get<SkillAreaVFX>(effectTarget, skillInfosFirst);
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
                var target = Game.instance.stage.GetCharacter(info.Target);
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
                var target = Game.instance.stage.GetCharacter(info.Target);
                ProcessHeal(target, info);
            }
        }

        public IEnumerator CoBuff(IReadOnlyList<Model.Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;
            
            yield return StartCoroutine(CoAnimationBuffCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                ProcessBuff(target, info);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(TargetTag))
            {
                var character = other.gameObject.GetComponent<CharacterBase>();
                if (TargetInRange(character) && character.IsAlive())
                {
                    StopRun();
                }
            }
        }
    }
}
