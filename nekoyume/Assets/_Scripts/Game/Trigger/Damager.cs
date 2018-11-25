using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class Damager : MonoBehaviour
    {
        private Character.Base _owner = null;
        private int _damage = 0;
        private float _size = 0.0f;
        private int _targetCount = 0;
        private List<GameObject> _hitObjects = new List<GameObject>();

        public void Update()
        {
            List<GameObject> targets = _owner.Targets;
            int targetCount = _targetCount;
            foreach (var go in targets)
            {
                if (targetCount <= 0)
                    break;

                if (_hitObjects.Contains(go))
                    continue;

                float halfSize = _size * 0.5f;
                Debug.DrawLine(
                    transform.TransformPoint(-halfSize, 0.0f, 0.0f), 
                    transform.TransformPoint(halfSize, 0.0f, 0.0f), 
                    Color.green, 1.0f);
                if (transform.position.x - halfSize > go.transform.position.x
                    || transform.position.x + halfSize < go.transform.position.x)
                    continue;

                var character = go.GetComponent<Character.Base>();
                _owner.Attack(character, _damage);
                
                --targetCount;

                _hitObjects.Add(go);
            }
        }

        public void Set(Character.Base owner, int damage, float size, int targetCount)
        {
            var anim = GetComponent<Util.SpriteAnimator>();
            anim.Play("hit_01");

            _owner = owner;
            _damage = damage;
            _size = size;
            _targetCount = targetCount;

            _hitObjects.Clear();
        }
    }
}
