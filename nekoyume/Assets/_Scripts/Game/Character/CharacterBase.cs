using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Data.Table;
using Nekoyume.Game.CC;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterBase : MonoBehaviour
    {
        protected const float AnimatorTimeScale = 1.2f;
        protected const float KSkillGlobalCooltime = 0.6f;
        
        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public int Power = 100;
        public float RunSpeed = 0.0f;
        public string targetTag = "";
        public string characterSize = "s";

        protected virtual WeightType WeightType => WeightType.Small;

        protected float dyingTime = 1.0f;

        private ProgressBar _hpBar;
        private ProgressBar _castingBar;
        protected SpeechBubble _speechBubble;

        public abstract Guid Id { get; }
        public abstract float Speed { get; }
        public int HPMax { get; protected set; } = 0;
        public ICharacterAnimator animator { get; protected set; }
        public bool attackEnd { get; private set; }
        public bool hitEnd { get; private set; }
        public bool dieEnd { get; private set; }
        public bool Rooted => gameObject.GetComponent<IRoot>() != null;
        public bool Silenced => gameObject.GetComponent<ISilence>() != null;
        public bool Stunned => gameObject.GetComponent<IStun>() != null;

        protected virtual float Range { get; set; }
        protected virtual Vector3 HUDOffset => new Vector3();
        protected virtual Vector3 DamageTextForce => default;

        protected virtual void Awake()
        {
            Event.OnAttackEnd.AddListener(AttackEnd);
        }

        protected virtual void OnDisable()
        {
            RunSpeed = 0.0f;
            Root = null;
        }

        public bool IsDead()
        {
            return HP <= 0;
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
                animator.StopRun();
                return;
            }
            
            animator.Run();

            Vector2 position = transform.position;
            position.x += Time.deltaTime * RunSpeed * RunSpeedMultiplier;
            transform.position = position;
        }

        private IEnumerator Dying()
        {
            StopRun();
            animator.Die();
            yield return new WaitForSeconds(.2f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDead();
        }

        protected virtual void Update()
        {
            Root?.Tick();
            if (!ReferenceEquals(_hpBar, null))
            {
                _hpBar.UpdatePosition(gameObject, HUDOffset);
            }
            if (!ReferenceEquals(_speechBubble, null))
            {
                _speechBubble.UpdatePosition(gameObject, HUDOffset);
            }
        }

        public int CalcAtk()
        {
            var r = ATK * 0.1f;
            return Mathf.FloorToInt((ATK + UnityEngine.Random.Range(-r, r)) * (Power * 0.01f));
        }

        public void UpdateHpBar()
        {
            if (ReferenceEquals(_hpBar, null))
            {
                _hpBar = Widget.Create<ProgressBar>(true);
            }
            _hpBar.UpdatePosition(gameObject, HUDOffset);
            _hpBar.SetText($"{HP} / {HPMax}");
            _hpBar.SetValue((float)HP / HPMax);
        }

        public bool ShowSpeech(string key, params int[] list)
        {
            if (ReferenceEquals(_speechBubble, null))
            {
                _speechBubble = Widget.Create<SpeechBubble>();
            }

            if (_speechBubble.gameObject.activeSelf)
            {
                return false;
            }

            if (list.Length > 0)
            {
                string join = string.Join(",", list.Select(x => x.ToString()).ToArray());
                key = $"{key}_{join}_";
            }
            else
            {
                key = $"{key}_";
            }

            if (!_speechBubble.SetKey(key))
            {
                return false;
            }

            StartCoroutine(_speechBubble.CoShowText());
            return true;
        }

        private float GetDamageFactor(AttackType attackType)
        {
            var damageFactorMap = new Dictionary<Tuple<AttackType, WeightType>, float>()
            {
                { new Tuple<AttackType, WeightType>(AttackType.Light, WeightType.Small), 1.25f },
                { new Tuple<AttackType, WeightType>(AttackType.Light, WeightType.Medium), 1.5f },
                { new Tuple<AttackType, WeightType>(AttackType.Light, WeightType.Large), 0.5f },
                { new Tuple<AttackType, WeightType>(AttackType.Light, WeightType.Boss), 0.75f },
                { new Tuple<AttackType, WeightType>(AttackType.Middle, WeightType.Small), 1.0f },
                { new Tuple<AttackType, WeightType>(AttackType.Middle, WeightType.Medium), 1.0f },
                { new Tuple<AttackType, WeightType>(AttackType.Middle, WeightType.Large), 1.25f },
                { new Tuple<AttackType, WeightType>(AttackType.Middle, WeightType.Boss), 0.75f },
                { new Tuple<AttackType, WeightType>(AttackType.Heavy, WeightType.Small), 0.75f },
                { new Tuple<AttackType, WeightType>(AttackType.Heavy, WeightType.Medium), 1.25f },
                { new Tuple<AttackType, WeightType>(AttackType.Heavy, WeightType.Large), 1.5f },
                { new Tuple<AttackType, WeightType>(AttackType.Heavy, WeightType.Boss), 0.75f },
            };
            var factor = damageFactorMap[new Tuple<AttackType, WeightType>(attackType, WeightType)];
            return factor;
        }

        protected int CalcDamage(AttackType attackType, int dmg)
        {
            const float attackDamageFactor = 0.5f;
            const float defenseDamageFactor = 0.25f;
            return Mathf.FloorToInt(
                (attackDamageFactor * dmg - defenseDamageFactor * DEF) *
                GetDamageFactor(attackType)
            );
        }

        public virtual IEnumerator CoProcessDamage(Model.Skill.SkillInfo info)
        {
            var dmg = info.Effect;

            if (dmg <= 0)
                yield break;

            HP -= dmg;
            UpdateHpBar();

            animator.Hit();
        }

        protected virtual void OnDead()
        {
            animator.Idle();
            gameObject.SetActive(false);
        }

        protected void PopUpDmg(Vector3 position, Vector3 force, Model.Skill.SkillInfo info)
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
                if (info.Category == SkillEffect.Category.Normal)
                    VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged();
                DamageText.Show(position, force, dmg);
                if (info.Category == SkillEffect.Category.Normal)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        private void InitBT()
        {
            Root = new Root();
            Root.OpenBranch(
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
            RunSpeed = Speed;
            if (Root == null)
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
                attackEnd = true;
        }

        // FixMe. 캐릭터와 몬스터가 겹치는 현상 있음.
        public bool TargetInRange(CharacterBase target) =>
            Range > Mathf.Abs(gameObject.transform.position.x - target.transform.position.x);

        public void StopRun()
        {
            RunSpeed = 0.0f;
            animator.StopRun();
        }

        public void DisableHUD()
        {
            if (!ReferenceEquals(_hpBar, null))
            {
                if (!ReferenceEquals(_hpBar.gameObject, null))
                    Destroy(_hpBar.gameObject);
                _hpBar = null;
            }
            
            if (!ReferenceEquals(_castingBar, null))
            {
                if (!ReferenceEquals(_castingBar.gameObject, null))
                    Destroy(_castingBar.gameObject);
                _castingBar = null;
            }

            if (!ReferenceEquals(_speechBubble, null))
            {
                _speechBubble.gameObject.SetActive(false);
                if (!ReferenceEquals(_speechBubble.gameObject, null))
                    Destroy(_speechBubble.gameObject, _speechBubble.destroyTime);
                _speechBubble = null;
            }
        }

        private void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill)
        {
            target.StopRun();
            StartCoroutine(target.CoProcessDamage(skill));
        }

        private void ProcessHeal(CharacterBase target, Model.Skill.SkillInfo info)
        {
            var calc = info.Effect - target.HP;
            if (calc <= 0)
            {
                calc = 0;
            }
            target.HP += calc;

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            var txt = calc.ToString();
            PopUpHeal(position, force, txt, info.Critical);

            UpdateHpBar();

            Event.OnUpdateStatus.Invoke();
        }

        private void PopUpHeal(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            DamageText.Show(position, force, dmg);
            VFXController.instance.Create<BattleHeal01VFX>(transform, HUDOffset - new Vector3(0f, 0.4f));
        }

        private IEnumerator CoAnimationAttack()
        {
            attackEnd = false;
            RunSpeed = 0.0f;

            animator.Attack();
            yield return new WaitUntil(() => attackEnd);

            var enemy = GetComponentsInChildren<CharacterBase>()
                .Where(c => c.gameObject.CompareTag(targetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null && !TargetInRange(enemy))
                RunSpeed = Speed;
        }
        
        private IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            attackEnd = false;
            RunSpeed = 0.0f;

            animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.stage.SkillController.Get(pos, info);
            effect.Play();
            yield return new WaitForSeconds(0.6f);

            var enemy = GetComponentsInChildren<CharacterBase>()
                .Where(c => c.gameObject.CompareTag(targetTag))
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null && !TargetInRange(enemy))
                RunSpeed = Speed;
        }

        public IEnumerator CoAttack(IEnumerable<Model.Skill.SkillInfo> infos)
        {
            yield return StartCoroutine(CoAnimationAttack());

            var skillInfos = infos.ToList();
            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                ProcessAttack(target, info);
            }

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                if (target.IsDead())
                    StartCoroutine(target.Dying());
            }

        }

        public IEnumerator CoAreaAttack(IEnumerable<Model.Skill.SkillInfo> infos)
        {
            var skillInfos = infos.ToList();
            var skillInfo = skillInfos.First();

            yield return StartCoroutine(CoAnimationCast(skillInfo));

            var effectTarget = Game.instance.stage.GetCharacter(skillInfo.Target);
            var effect = Game.instance.stage.SkillController.Get<SkillAreaVFX>(effectTarget, skillInfo);
            effect.Play();
            yield return new WaitForSeconds(0.5f);

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                yield return new WaitForSeconds(0.1f);
                if (skillInfos.Last() == info && effect.finisher)
                {
                    yield return new WaitForSeconds(0.4f);
                    effect.Finisher();
                }
                ProcessAttack(target, info);
            }

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                if (target.IsDead())
                    StartCoroutine(target.Dying());
            }
            yield return new WaitWhile(() => effect.isActiveAndEnabled);
        }

        public IEnumerator CoDoubleAttack(IEnumerable<Model.Skill.SkillInfo> infos)
        {
            var skillInfos = infos.ToList();
            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                var first = skillInfos.First() == info;
                var effect = Game.instance.stage.SkillController.Get<SkillDoubleVFX>(target, info);

                yield return StartCoroutine(CoAnimationAttack());
                if (first)
                {
                    effect.FirstStrike();
                }
                else
                {
                    effect.SecondStrike();
                }
                ProcessAttack(target, info);
            }

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                if (target.IsDead())
                    StartCoroutine(target.Dying());
            }

            yield return new WaitForSeconds(1.2f);
        }

        public IEnumerator CoBlow(IEnumerable<Model.Skill.SkillInfo> infos)
        {
            var skillInfos = infos.ToList();

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            yield return StartCoroutine(CoAnimationAttack());

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                var effect = Game.instance.stage.SkillController.Get<SkillBlowVFX>(target, info);
                effect.Play();
                ProcessAttack(target, info);
            }

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                if (target.IsDead())
                    StartCoroutine(target.Dying());
            }
        }

        public IEnumerator CoHeal(IEnumerable<Model.Skill.SkillInfo> infos)
        {
            var skillInfos = infos.ToList();

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = Game.instance.stage.GetCharacter(info.Target);
                ProcessHeal(target, info);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(targetTag))
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
