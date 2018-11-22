using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game.Skill
{
    abstract public class Skill : MonoBehaviour
    {
        public Data.Table.Skill _data;

        protected float _cooltime = 0.0f;

        abstract public bool Use();

        private void Update()
        {
            _cooltime -= Time.deltaTime;
        }

        public bool Init(string id)
        {
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Skill.TryGetValue(id, out _data))
            {
                return true;
            }
            Destroy(this);
            return false;
        }

        public List<GameObject> GetTargets()
        {
            return null;
        }

        public bool IsCooltime()
        {
            return _cooltime > 0;
        }

        public bool IsTargetInRange(string tag)
        {
            var stage = GetComponentInParent<Stage>();
            Character.Base[] characters = stage.GetComponentsInChildren<Character.Base>();
            foreach (var character in characters)
            {
                if (character.tag != tag)
                    continue;

                float range = (float)_data.Range / (float)Game.PixelPerUnit;
                float dist = character.transform.position.x - transform.position.x;
                if (range > dist)
                    return true;
            }
            return false;
        }

        public GameObject GetNearestTarget(string tag)
        {
            var stage = GetComponentInParent<Stage>();
            Character.Base[] characters = stage.GetComponentsInChildren<Character.Base>();
            GameObject nearest = null;
            float nearestDist = 9999.0f;
            foreach (var character in characters)
            {
                if (character.tag != tag)
                    continue;

                float dist = character.transform.position.x - transform.position.x;
                if (nearest == null)
                {
                    nearest = character.gameObject;
                    nearestDist = dist;
                    continue;
                }
                else if (nearestDist > dist)
                {
                    nearest = character.gameObject;
                    nearestDist = dist;
                }
            }
            return nearest;
        }
    }
}
