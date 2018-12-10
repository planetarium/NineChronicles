using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game.Skill
{
    abstract public class SkillBase : MonoBehaviour
    {
        protected Data.Table.Skill _data = null;
        protected string _targetTag = "";
        public bool Casting { get; private set; } = false;
        public float CastingTime { get; private set; } = 0.0f;
        public float CastingPercentage => Mathf.Max(1 - CastingTime / _data.CastingTime, 0);
        public bool Casted { get; private set; } = false;
        protected float _cooltime = 0.0f;
        protected float _knockBack = 0.0f;

        private void Update()
        {
            _cooltime -= Time.deltaTime;
            if (Casting)
            {
                CastingTime -= Time.deltaTime;
                if (CastingTime <= 0)
                {
                    Casting = false;
                    Casted = true;
                }
            }
        }

        public bool Init(Data.Table.Skill data)
        {
            if (data == null)
            {
                Destroy(this);
                return false;
            }
            _data = data;
            _cooltime = 0.0f;
            return true;
        }

        public List<GameObject> GetTargets()
        {
            return null;
        }

        public bool IsCooltime()
        {
            return _cooltime > 0;
        }

        public float GetCooltime()
        {
            return _cooltime;
        }

        public bool IsTargetInRange()
        {
            var stage = GetComponentInParent<Stage>();
            var characters = stage.GetComponentsInChildren<Character.CharacterBase>();
            foreach (var character in characters)
            {
                if (character.gameObject.tag != _targetTag)
                    continue;

                if (character.IsDead())
                    continue;

                float range = (float)_data.Range / (float)Game.PixelPerUnit;
                float dist = Mathf.Abs(character.transform.position.x - transform.position.x);
                if (range > dist)
                    return true;
            }
            return false;
        }

        public GameObject GetNearestTarget(string tag)
        {
            var stage = GetComponentInParent<Stage>();
            var characters = stage.GetComponentsInChildren<Character.CharacterBase>();
            GameObject nearest = null;
            float nearestDist = 9999.0f;
            foreach (var character in characters)
            {
                if (character.gameObject.tag != tag)
                    continue;

                if (character.IsDead())
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

        public void SetGlobalCooltime(float cooltime)
        {
            if (_cooltime < cooltime)
                _cooltime = cooltime;
        }

        public void Damager(Trigger.Damager damager, float range, string ani)
        {
            var owner = GetComponent<Character.CharacterBase>();
            damager.transform.position = transform.TransformPoint(range, 0.0f, 0.0f);
            int damage = Mathf.FloorToInt(owner.CalcAtk() * ((float)_data.Power * 0.01f));
            float size = (float)_data.Size / (float)Game.PixelPerUnit;
            damager.Set(ani, _targetTag, _data.AttackType, damage, size, _data.TargetCount, _knockBack);
        }

        public bool Cast()
        {
            /*
             * Returns true if casting is needed
             */
            if (Casted || Casting || IsCooltime())
                return false;

            if (_data.CastingTime <= 0)
            {
                Casted = true;
                return false;
            }

            Casting = true;
            CastingTime = _data.CastingTime;

            return true;
        }

        public void CancelCast()
        {
            Casting = false;
            _cooltime = _data.Cooltime;
        }

        public bool Use()
        {
            if (!Casted)
                return false;
            if (IsCooltime())
                return false;

            Casted = false;
            _cooltime = (float)_data.Cooltime;

            return _Use();
        }

        protected abstract bool _Use();
    }
}
