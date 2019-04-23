using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Data.Table;
using Nekoyume.Game.CC;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Vfx;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class CharacterBase : MonoBehaviour
    {
        protected const float AnimatorSpeed = 1.5f;
        
        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public int Power = 100;

        public virtual WeightType WeightType { get; protected set; } = WeightType.Small;
        public float RunSpeed = 0.0f;

        public int HPMax { get; protected set; } = 0;
        protected internal Animator _anim = null;
        protected ProgressBar _hpBar = null;
        protected virtual Vector3 _hpBarOffset => new Vector3();
        protected ProgressBar _castingBar = null;
        protected virtual Vector3 _castingBarOffset => new Vector3();
        protected float _dyingTime = 1.0f;

        protected const float kSkillGlobalCooltime = 0.6f;

        public bool Rooted => gameObject.GetComponent<IRoot>() != null;
        public bool Silenced => gameObject.GetComponent<ISilence>() != null;
        public bool Stunned => gameObject.GetComponent<IStun>() != null;
        private const float Range = 1.6f;
        protected string _targetTag = "";
        public bool attackEnd { get; private set; }
        public bool hitEnd { get; private set; }
        public bool dieEnd { get; private set; }
        public abstract float Speed { get; }

        protected virtual void Awake()
        {
            _anim = GetComponent<Animator>();
            SetAnimatorSpeed(AnimatorSpeed);
        }

        protected void SetAnimatorSpeed(float speed)
        {
            if (_anim != null)
            {
                _anim.speed = speed;    
            }
        }

        protected virtual void OnDisable()
        {
            RunSpeed = 0.0f;
            Root = null;
            if (_hpBar != null)
            {
                Destroy(_hpBar.gameObject);
                _hpBar = null;
            }
            if (_castingBar != null)
            {
                Destroy(_castingBar.gameObject);
                _castingBar = null;
            }
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

        protected virtual void Run()
        {
            if (Rooted)
            {
                _anim.SetBool("Run", false);
                return;
            }
            if (_anim != null)
            {
                _anim.SetBool("Run", true);
            }

            Vector2 position = transform.position;
            position.x += Time.deltaTime * RunSpeed * RunSpeedMultiplier;
            transform.position = position;
        }

        public void Die()
        {
            StartCoroutine(Dying());
        }

        protected IEnumerator Dying()
        {
            if (_anim != null)
            {
                dieEnd = false;
                _anim.Play("Die");
                yield return new WaitUntil(() => dieEnd);
            }

            OnDead();
        }

        protected  virtual void Update()
        {
            Root?.Tick();
            if (_hpBar != null)
            {
                _hpBar.UpdatePosition(gameObject, _hpBarOffset);
            }
            if (_anim == null)
            {
                _anim = GetComponentInChildren<Animator>();
                SetAnimatorSpeed(AnimatorSpeed);
            }
        }

        public int CalcAtk()
        {
            var r = ATK * 0.1f;
            return Mathf.FloorToInt((ATK + UnityEngine.Random.Range(-r, r)) * (Power * 0.01f));
        }

        public void UpdateHpBar()
        {
            if (_hpBar == null)
            {
                _hpBar = Widget.Create<ProgressBar>(true);
            }
            _hpBar.UpdatePosition(gameObject, _hpBarOffset);
            _hpBar.SetText($"{HP} / {HPMax}");
            _hpBar.SetValue((float)HP / (float)HPMax);
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

        public virtual IEnumerator CoProcessDamage(int dmg, bool critical)
        {
            if (dmg <= 0)
                yield break;

            HP -= dmg;

            if (_anim != null)
            {
                if (HP > 0)
                {
                    hitEnd = false;
                    _anim.Play("Hit");
                    yield return new WaitUntil(() => hitEnd);    
                }
                else
                {
                    StartCoroutine(Dying());
                }
            }

            UpdateHpBar();
        }

        protected virtual void OnDead()
        {
            if (_anim != null)
            {
                _anim.ResetTrigger("Attack");
                _anim.SetBool("Run", false);
            }
            gameObject.SetActive(false);
        }

        public IEnumerator CoAttack(int atk, CharacterBase target, bool critical)
        {
            attackEnd = false;
            RunSpeed = 0.0f;
            if (_anim != null)
            {
                _anim.SetTrigger("Attack");
                _anim.SetBool("Run", false);
            }
            yield return new WaitUntil(() => attackEnd);

            if (target != null)
            {
                yield return StartCoroutine(target.CoProcessDamage(atk, critical));
            }
        }

        protected virtual void PopUpDmg(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            if (critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg);
            }
            else
            {
                AudioController.PlayDamaged();
                DamageText.Show(position, force, dmg);
            }
        }

        private void InitBT()
        {
            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(CanRun).OpenBranch(
                        BT.Call(Run)
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

        private bool CanRun()
        {
            return !(Mathf.Approximately(RunSpeed, 0f));
        }

        protected void AttackEnd()
        {
            attackEnd = true;
        }

        protected void HitEnd()
        {
            hitEnd = true;
        }

        protected void DieEnd()
        public bool TargetInRange(CharacterBase target) =>
            Range > Mathf.Abs(gameObject.transform.position.x - target.transform.position.x);
        {
            dieEnd = true;
        }
    }
}
