using BTAI;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Base : MonoBehaviour
    {
        public Root Root;
        public int HP = 0;
        public int ATK = 0;
        public int DEF = 0;

        public bool Walkable { get; set; } = false;
        public bool InStage = false;

        protected float _walkSpeed = 0.0f;

        public bool IsDead()
        {
            return HP <= 0;
        }

        protected bool IsAlive()
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

        public void Attack(Enemy target)
        {
            int dmg = this.ATK - target.DEF;
            target.OnDamage(dmg);
        }

        protected virtual bool HasTarget()
        {
            return false;
        }
    }
}
