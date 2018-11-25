using System.Collections.Generic;
using System.Linq;
using BTAI;
using UnityEngine;
using Time = UnityEngine.Time;


namespace Nekoyume.Game.Character
{
    public class Base : MonoBehaviour
    {
        public List<GameObject> Targets = new List<GameObject>();

        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public bool Walkable { get; set; } = false;
        public bool InStage = false;

        protected float _walkSpeed = 0.0f;

        protected List<Skill.Skill> _skills = new List<Skill.Skill>();

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
            return Walkable;
        }

        protected virtual void Walk()
        {
            Vector2 position = transform.position;
            position.x += Time.deltaTime * _walkSpeed;
            transform.position = position;
        }

        private void Update()
        {
            Root?.Tick();
        }

        public void Attack(Base target)
        {
            int dmg = this.ATK - target.DEF;
            OnDamage(target, dmg);
        }

        protected virtual bool HasTarget()
        {
            foreach (var target in Targets)
            {
                Character.Base character = target.GetComponent<Character.Base>();
                if (character.IsAlive())
                    return true;
            }
            return false;
        }

        private static void OnDamage(Base target, int dmg)
        {
            target.HP -= dmg;
            Debug.Log($"{target.name} HP: {target.HP}");
            if (target.IsDead())
            {
                target.OnDead();
            }
        }

        protected virtual void OnDead()
        {
            foreach (var target in Targets)
            {
                var character = target.GetComponent<Base>();
                character.OnTargetDead(gameObject);
            }
            Targets.Clear();
            gameObject.SetActive(false);
        }

        public void OnTargetDead(GameObject target)
        {
            Debug.Log($"Kill! ({target})");
            //Targets.Remove(target);
        }
    }
}
