using Nekoyume.Battle;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nekoyume.Model
{
    public class RaidBoss : Enemy
    {
        [NonSerialized]
        public RaidSimulator RaidSimulator;

        public new WorldBossCharacterSheet.Row RowData { get; }
        public WorldBossActionPatternSheet.Row PatternRowData { get; }

        private List<Skill.Skill> _orderedSkills = new List<Skill.Skill>();
        private int _actionCount;
        private int _wave;

        public RaidBoss(
            CharacterBase player,
            WorldBossCharacterSheet.Row characterRow,
            WorldBossActionPatternSheet.Row patternRow,
            WorldBossCharacterSheet.WaveStatData statData)
            : base(
                player,
                new CharacterStats(statData),
                characterRow.BossId,
                statData.ElementalType)
        {
            RaidSimulator = (RaidSimulator) player.Simulator;
            RowData = characterRow;
            PatternRowData = patternRow;
            _wave = statData.Wave;
        }

        public RaidBoss(RaidBoss value) : base(value)
        {
            RaidSimulator = value.RaidSimulator;
            RowData = value.RowData;
            PatternRowData = value.PatternRowData;
            _wave = value._wave;
        }

        public override object Clone() => new RaidBoss(this);

        protected override void SetSkill()
        {
            var pattern = PatternRowData.Patterns.First(x => x.Wave == _wave);
            var dmg = (int)(ATK * 0.3m);

            foreach (var id in pattern.SkillIds)
            {
                if (!Simulator.SkillSheet.TryGetValue(id, out var skillRow))
                {
                    throw new SheetRowNotFoundException(nameof(SkillSheet), id);
                }

                var skill = SkillFactory.Get(skillRow, dmg, 100);
                _orderedSkills.Add(skill);
            }
        }

        protected override void UseSkill()
        {
            var index = _actionCount % _orderedSkills.Count;
            var skill = _orderedSkills[index];
            var usedSkill = skill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    skill,
                    Simulator.SkillBuffSheet,
                    Simulator.BuffSheet
                )
            );

            Simulator.Log.Add(usedSkill);
            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.Target.IsDead)
                    continue;

                var target = Targets.FirstOrDefault(i => i.Id == info.Target.Id);
                target?.Die();
            }
        }

        protected override void EndTurn()
        {
            ++_actionCount;
            base.EndTurn();
        }
    }
}
