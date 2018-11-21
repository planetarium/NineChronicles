using System.Collections;
using System.Linq;
using BTAI;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Base : MonoBehaviour
    {
        public Root Root { get; set; }
        public int HP { get; set; }

        protected bool Walkable { get; set; } = false;

        protected float _walkSpeed = 0.0f;

        protected virtual bool IsDead()
        {
            return false;
        }

        protected virtual void Walk()
        {
            Vector2 position = transform.position;
            position.x += Time.deltaTime * _walkSpeed;
            transform.position = position;
        }

        private void Update()
        {
            if (Root != null)
            {
                Root.Tick();
            }
        }
    }
}
