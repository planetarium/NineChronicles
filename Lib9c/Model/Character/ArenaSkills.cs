using System;
using System.Collections.Generic;
using Libplanet.Action;
using System.Collections;
using System.Linq;
using Nekoyume.Model.Skill.Arena;

namespace Nekoyume.Model
{
    [Serializable]
    public class ArenaSkills : IEnumerable<ArenaSkill>
    {
        private readonly List<ArenaSkill> _skills = new List<ArenaSkill>();
        private Dictionary<int, int> _skillsCooldown = new Dictionary<int, int>();

        public void Add(ArenaSkill skill)
        {
            if (skill is null)
            {
                return;
            }

            _skills.Add(skill);
        }

        public void Clear()
        {
            _skills.Clear();
        }

        public IEnumerator<ArenaSkill> GetEnumerator()
        {
            return _skills.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetCooldown(int skillId, int cooldown)
        {
            if (_skills.All(e => e.SkillRow.Id != skillId))
                throw new Exception(
                    $"[{nameof(Skills)}.{nameof(SetCooldown)}()] Not found {nameof(skillId)}({skillId})");

            _skillsCooldown[skillId] = cooldown;
        }

        public int GetCooldown(int skillId)
        {
            return _skillsCooldown.ContainsKey(skillId)
                ? _skillsCooldown[skillId]
                : 0;
        }

        public void ReduceCooldown()
        {
#pragma warning disable LAA1002
            if (!_skillsCooldown.Any())
#pragma warning restore LAA1002
                return;

            foreach (var key in _skillsCooldown.Keys.OrderBy(i => i))
            {
                var value = _skillsCooldown[key];
                if (value < 1)
                {
                    _skillsCooldown.Remove(key);
                    continue;
                }

                _skillsCooldown[key] = value - 1;
            }
        }

        public ArenaSkill Select(IRandom random)
        {
            return PostSelect(random, GetSelectableSkills());
        }

        public ArenaSkill SelectWithoutDefaultAttack(IRandom random)
        {
            return PostSelectWithoutDefaultAttack(random, GetSelectableSkills());
        }

        private IEnumerable<ArenaSkill> GetSelectableSkills()
        {
            return _skills.Where(skill => !_skillsCooldown.ContainsKey(skill.SkillRow.Id));
        }

        private ArenaSkill PostSelect(IRandom random, IEnumerable<ArenaSkill> skills)
        {
            var skillList = skills.ToList();
            var defaultAttack = skillList.FirstOrDefault(x => x.SkillRow.Id == GameConfig.DefaultAttackId);
            if (defaultAttack == null)
            {
                throw new Exception("There is no default attack");
            }

            if (skillList.Count == 1) // If there's only a default attack in skills
            {
                return defaultAttack;
            }

            var chance = random.Next(0, 100);
            var selectedSkills  = skillList
                .Where(x => x.SkillRow.Id != GameConfig.DefaultAttackId && chance < x.Chance)
                .OrderBy(x => x.Power)
                .ToList();

            return selectedSkills.Any() ? selectedSkills.First() : defaultAttack;
        }

        private ArenaSkill PostSelectWithoutDefaultAttack(IRandom random, IEnumerable<ArenaSkill> skills)
        {
            if (!skills.Any())
            {
                return null;
            }

            var skillList = skills.ToList();
            var chance = random.Next(0, 100);
            var selectedSkills = skillList
                .Where(x => x.SkillRow.Id != GameConfig.DefaultAttackId && chance < x.Chance)
                .OrderBy(x => x.Power)
                .ToList();
            return selectedSkills.FirstOrDefault();
        }
    }
}
