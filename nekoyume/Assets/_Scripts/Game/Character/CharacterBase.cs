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

        protected float _walkSpeed = 0.0f;
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
            position.x += Time.deltaTime * _walkSpeed;
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
            return Mathf.FloorToInt((float)this.ATK * (this.Power * 0.01f));
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

        public virtual void OnDamage(int dmg)
        {
            HP -= dmg - DEF;
            Debug.Log($"{name} HP: {HP}");

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
