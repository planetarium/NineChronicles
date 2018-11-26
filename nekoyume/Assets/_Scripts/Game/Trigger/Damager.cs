using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class Damager : MonoBehaviour
    {
        private Stage _stage = null;
        private string _targetTag = "";
        private int _damage = 0;
        private float _size = 0.0f;
        private int _targetCount = 0;
        private List<GameObject> _hitObjects = new List<GameObject>();

        public void Start()
        {
            _stage = GetComponentInParent<Stage>();
        }

        public void Update()
        {
            int targetCount = _targetCount;
            var characters = _stage.GetComponentsInChildren<Character.Base>();
            foreach (var character in characters)
            {
                if (targetCount <= 0)
                    break;

                if (character.gameObject.tag != _targetTag)
                    continue;

                if (_hitObjects.Contains(character.gameObject))
                    continue;

                if (character.IsDead())
                    continue;

                float halfSize = _size * 0.5f;
                Debug.DrawLine(
                    transform.TransformPoint(-halfSize, 0.0f, 0.0f), 
                    transform.TransformPoint(halfSize, 0.0f, 0.0f), 
                    Color.green, 1.0f);
                if (transform.position.x - halfSize > character.transform.position.x
                    || transform.position.x + halfSize < character.transform.position.x)
                    continue;

                character.OnDamage(_damage);
                
                --targetCount;

                _hitObjects.Add(character.gameObject);
            }
        }

        public void Set(string targetTag, int damage, float size, int targetCount)
        {
            var anim = GetComponent<Util.SpriteAnimator>();
            anim.Play("hit_01");

            _targetTag = targetTag;
            _damage = damage;
            _size = size;
            _targetCount = targetCount;

            _hitObjects.Clear();
        }
    }
}
