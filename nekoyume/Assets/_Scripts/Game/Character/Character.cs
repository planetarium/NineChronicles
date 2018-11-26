using System.Collections.Generic;
using System.Linq;
using BTAI;
using UnityEngine;
using Time = UnityEngine.Time;


namespace Nekoyume.Game.Character
{
    public class Base : MonoBehaviour
    {
        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public int Power = 100;

        protected float _walkSpeed = 0.0f;
        protected Animator _anim = null;

        protected List<Skill.Skill> _skills = new List<Skill.Skill>();

        private void Awake()
        {
            _anim = GetComponent<Animator>();
        }

        public bool IsDead()
        {
            return HP <= 0;
        }

        public bool IsAlive()
        {
            return !IsDead();
        }

        protected bool CanWalk()
        {
            return true;
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

        private void Update()
        {
            Root?.Tick();
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

        public void OnDamage(int dmg)
        {
            HP -= dmg - DEF;
            Debug.Log($"{name} HP: {HP}");
            if (IsDead())
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            gameObject.SetActive(false);
        }
    }
}
