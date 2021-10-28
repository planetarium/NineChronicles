using System;
using System.Collections.Generic;
using Libplanet.Action;
using System.Collections;
using System.Linq;
using Nekoyume.Battle;

namespace Nekoyume.Model
{
    [Serializable]
    public class Skills : IEnumerable<Skill.Skill>
    {
        private readonly List<Skill.Skill> _skills = new List<Skill.Skill>();
        private Dictionary<int, int> _skillsCooldown = new Dictionary<int, int>();

        public void Add(Skill.Skill skill)
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

        public IEnumerator<Skill.Skill> GetEnumerator()
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

        [Obsolete("ReduceCooldown")]
        public void ReduceCooldown2()
        {
#pragma warning disable LAA1002
            if (!_skillsCooldown.Any())
#pragma warning restore LAA1002
                return;

            foreach (var key in _skillsCooldown.Keys.OrderBy(i => i))
            {
                var value = _skillsCooldown[key];
                if (value <= 1)
                {
                    _skillsCooldown.Remove(key);
                    continue;
                }

                _skillsCooldown[key] = value - 1;
            }
        }

        public Skill.Skill Select(IRandom random)
        {
            return PostSelect(random, GetSelectableSkills());
        }

        [Obsolete("Use Select")]
        public Skill.Skill Select2(IRandom random)
        {
            return PostSelect2(random, GetSelectableSkills());
        }

        [Obsolete("Use Select")]
        public Skill.Skill Select3(IRandom random)
        {
            return PostSelect3(random, GetSelectableSkills());
        }

        private IEnumerable<Skill.Skill> GetSelectableSkills()
        {
            return _skills.Where(skill => !_skillsCooldown.ContainsKey(skill.SkillRow.Id));
        }

        private Skill.Skill PostSelect(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var defaultAttack = skills.FirstOrDefault(x => x.SkillRow.Id == GameConfig.DefaultAttackId);
            if (defaultAttack == null)
            {
                throw new Exception($"There is no default attack");
            }

            if (skills.Count() == 1) // If there's only a default attack in skills
            {
                return defaultAttack;
            }

            var sortedSkills = skills
                .Where(x => x.SkillRow.Id != GameConfig.DefaultAttackId)
                .OrderBy(x => x.SkillRow.Id);

            var sumChance = sortedSkills.Sum(x => x.Chance);
            if (random.Next(0, 100) >= sumChance)
            {
                return defaultAttack;
            }

            var itemSelector = new WeightedSelector<Skill.Skill>(random);
            foreach (var skill in sortedSkills)
            {
                itemSelector.Add(skill, skill.Chance);
            }

            var selectedSkill = itemSelector.SelectV3(1);
            return selectedSkill.First();
        }

        [Obsolete("Use PostSelect")]
        private Skill.Skill PostSelect2(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var selected = skills
                .Select(skill => new {skill, chance = random.Next(0, 100)})
                .Where(t => t.skill.Chance > t.chance)
                .OrderBy(t => t.skill.SkillRow.Id)
                .ThenBy(t => t.chance == 0 ? 1m : (decimal) t.chance / t.skill.Chance)
                .Select(t => t.skill)
                .ToList();

            return selected.Any()
                ? selected[random.Next(selected.Count)]
                : throw new Exception($"[{nameof(Skills)}] There is no selected skills");
        }

        [Obsolete("Use PostSelect")]
        private Skill.Skill PostSelect3(IRandom random, IEnumerable<Skill.Skill> skills)
        {
            var selected = skills
                .OrderBy(skill => skill.SkillRow.Id)
                .Select(skill => new {skill, chance = random.Next(0, 100)})
                .Where(t => t.skill.Chance > t.chance)
                .OrderBy(t => t.skill.SkillRow.Id)
                .ThenBy(t => t.chance == 0 ? 1m : (decimal) t.chance / t.skill.Chance)
                .Select(t => t.skill)
                .ToList();

            return selected.Any()
                ? selected[random.Next(selected.Count)]
                : throw new Exception($"[{nameof(Skills)}] There is no selected skills");
        }
    }
}
