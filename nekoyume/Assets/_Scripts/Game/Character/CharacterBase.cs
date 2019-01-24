using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Data.Table;
using Nekoyume.Game.CC;
using Nekoyume.Game.Skill;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class CharacterBase : MonoBehaviour
    {
        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public int Power = 100;

        public virtual WeightType WeightType { get; protected set; } = WeightType.Small;
        public float RunSpeed = 0.0f;

        protected int _hpMax = 0;
        protected internal Animator _anim = null;
        protected ProgressBar _hpBar = null;
        protected Vector3 _hpBarOffset = new Vector3();
        protected ProgressBar _castingBar = null;
        protected Vector3 _castingBarOffset = new Vector3();
        protected float _dyingTime = 1.0f;

        protected List<SkillBase> _skills = new List<SkillBase>();
        protected const float kSkillGlobalCooltime = 0.6f;
        protected bool Casting => CastingSkill != null;
        protected SkillBase CastingSkill => _skills.Find(skill => skill.Casting);
        protected SkillBase CastedSkill => _skills.Find(skill => skill.Casted);

        public bool Rooted => gameObject.GetComponent<IRoot>() != null;
        public bool Silenced => gameObject.GetComponent<ISilence>() != null;
        public bool Stunned => gameObject.GetComponent<IStun>() != null;

        private void Start()
        {
            _anim = GetComponent<Animator>();
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

        protected virtual void Attack()
        {
            TryAttack();
        }

        protected virtual bool TryAttack()
        {
            if (Casting)
                return false;
            
            foreach (var skill in _skills)
            {
                if (UseSkill(skill)) return true;
            }
            return false;
        }

        public virtual bool UseSkill(SkillBase selectedSkill, bool checkRange = true)
        {
            if (checkRange && !selectedSkill.IsTargetInRange()) return false;
            if (Stunned) return false;
            if (selectedSkill.NeedsCasting && Silenced) return false;
            if (selectedSkill.Cast())
            {
                if (_anim != null)
                {
                    // TODO: Casting Animation
                    _anim.SetBool("Run", false);
                }
                return false;
            }

            if (!selectedSkill.Use(selectedSkill.NeedsCasting ? 1.0f : AttackSpeedMultiplier))
                return false;

            if (_anim != null)
            {
                _anim.SetTrigger("Attack");
                _anim.SetBool("Run", false);
            }
            foreach (var skill in _skills)
            {
                skill.SetGlobalCooltime(kSkillGlobalCooltime);
            }
            return true;
        }

        public virtual bool CancelCast()
        {
            if (!Casting) return false;

            CastingSkill.CancelCast();
            return true;
        }

        protected void Die()
        {
            StartCoroutine(Dying());
        }

        protected IEnumerator Dying()
        {
            if (_anim != null)
            {
                _anim.SetTrigger("Die");
            }

            yield return new WaitForSeconds(_dyingTime);

            OnDead();
        }

        protected  virtual void Update()
        {
            Root?.Tick();
            if (_hpBar != null)
            {
                _hpBar.UpdatePosition(gameObject, _hpBarOffset);
            }

            if (Casting)
            {
                if (_castingBar == null)
                {
                    _castingBar = Widget.Create<ProgressBar>(true);
                }
                var castingBarOffset = _hpBar == null ? _hpBarOffset : _castingBarOffset;
                _castingBar.UpdatePosition(gameObject, castingBarOffset);
                _castingBar.SetText($"{Mathf.FloorToInt(CastingSkill.CastingPercentage * 100)}%");
                _castingBar.SetValue(CastingSkill.CastingPercentage);
            }
            else
            {
                if (_castingBar != null)
                {
                    Destroy(_castingBar.gameObject);
                    _castingBar = null;
                }
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
            _hpBar.SetText($"{HP} / {_hpMax}");
            _hpBar.SetValue((float)HP / (float)_hpMax);
        }

        protected bool HasTargetInRange()
        {
            foreach (var skill in _skills)
            {
                if (skill.IsTargetInRange())
                {
                    return true;
                }
            }
            return false;
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

        public virtual void OnDamage(AttackType attackType, int dmg)
        {
            int calcDmg = CalcDamage(attackType, dmg);
            if (calcDmg <= 0)
                return;

            HP -= calcDmg;

            if (_anim != null)
            {
                var info = _anim.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("idle") || info.IsName("run"))
                {
                    _anim.SetTrigger("Hit");
                }
            }

            UpdateHpBar();
        }

        protected virtual void OnDead()
        {
            if (_anim != null)
            {
                _anim.ResetTrigger("Attack");
                _anim.ResetTrigger("Die");
                _anim.ResetTrigger("Hit");
                _anim.SetBool("Run", false);
            }

            gameObject.SetActive(false);
        }

        public void Attack(int atk, CharacterBase target)
        {
            if (_anim != null)
            {
                _anim.SetTrigger("Attack");
                _anim.SetBool("Run", false);
            }

            if (target != null)
            {
                target._anim.SetTrigger("Hit");
            }
        }

        public void Dead()
        {
            OnDead();
        }
    }
}
