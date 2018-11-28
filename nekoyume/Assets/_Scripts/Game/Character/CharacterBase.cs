using System;
using System.Collections;
using System.Collections.Generic;
using BTAI;
using Nekoyume.Data.Table;
using Nekoyume.Game.Trigger;
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
        public float WalkSpeed = 0.0f;

        protected int _hpMax = 0;
        protected Animator _anim = null;
        protected UI.ProgressBar _hpBar = null;
        protected Vector3 _hpBarOffset = new Vector3();

        protected List<Skill.SkillBase> _skills = new List<Skill.SkillBase>();

        private void Start()
        {
            _anim = GetComponent<Animator>();
        }

        private void OnDisable()
        {
            WalkSpeed = 0.0f;
            Root = null;
            if (_hpBar != null)
            {
                Destroy(_hpBar.gameObject);
                _hpBar = null;
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

        protected virtual void Walk()
        {
            if (_anim != null)
            {
                _anim.SetBool("Walk", true);
            }

            Vector2 position = transform.position;
            position.x += Time.deltaTime * WalkSpeed;
            transform.position = position;
        }

        protected void Attack()
        {
            foreach (var skill in _skills)
            {
                if (skill.Use())
                {
                    if (_anim != null)
                    {
                        _anim.SetTrigger("Attack");
                        _anim.SetBool("Walk", false);
                    }
                }
            }
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

            yield return new WaitForSeconds(1.0f);

            OnDead();
        }

        private void Update()
        {
            Root?.Tick();

            if (_hpBar != null)
            {
                _hpBar.UpdatePosition(gameObject, _hpBarOffset);
            }
        }

        public int CalcAtk()
        {
            var r = ATK * 0.1f;
            return Mathf.FloorToInt((ATK + UnityEngine.Random.Range(-r, r)) * (Power * 0.01f));
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

        private int CalcDamage(AttackType attackType, int dmg)
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
            HP -= CalcDamage(attackType, dmg);

            if (_hpBar == null)
            {
                _hpBar = UI.Widget.Create<UI.ProgressBar>(true);
            }
            _hpBar.SetText($"{HP} / {_hpMax}");
            _hpBar.SetValue((float)HP / (float)_hpMax);
        }

        protected virtual void OnDead()
        {
            if (_anim != null)
            {
                _anim.ResetTrigger("Attack");
                _anim.ResetTrigger("Die");
                _anim.SetBool("Walk", false);
            }

            gameObject.SetActive(false);
        }
    }
}
