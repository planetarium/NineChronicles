using Nekoyume.Battle;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model
{
    public class RaidBoss : Enemy
    {
        public new WorldBossCharacterSheet.Row RowData { get; }
        public WorldBossActionPatternSheet.Row PatternRowData { get; }

        private List<Skill.Skill> _orderedSkills = new List<Skill.Skill>();
        private Skill.Skill _enrageSkill;
        private int _actionCount;
        private int _wave;

        public bool Enraged { get; protected set; }
        public bool IgnoreLevelCorrectionOnHit { get; protected set; }

        public RaidBoss(
            CharacterBase player,
            WorldBossCharacterSheet.Row characterRow,
            WorldBossActionPatternSheet.Row patternRow,
            WorldBossCharacterSheet.WaveStatData statData,
            bool ignoreLevelCorrectionOnHit)
            : base(
                player,
                new CharacterStats(statData),
                characterRow.BossId,
                statData.ElementalType)
        {
            RowData = characterRow;
            PatternRowData = patternRow;
            IgnoreLevelCorrectionOnHit = ignoreLevelCorrectionOnHit;
            _wave = statData.Wave;
        }

        public RaidBoss(RaidBoss value) : base(value)
        {
            RowData = value.RowData;
            PatternRowData = value.PatternRowData;
            _orderedSkills = value._orderedSkills;
            _enrageSkill = value._enrageSkill;
            _actionCount = value._actionCount;
            _wave = value._wave;
            Enraged = value.Enraged;
            IgnoreLevelCorrectionOnHit = value.IgnoreLevelCorrectionOnHit;
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

            var enrageSkillId = RowData.WaveStats
                .FirstOrDefault(x => x.Wave == _wave).EnrageSkillId;
            if (!Simulator.SkillSheet.TryGetValue(enrageSkillId, out var enrageSkillRow))
            {
                throw new SheetRowNotFoundException(nameof(SkillSheet), enrageSkillId);
            }

            var enrageSkill = SkillFactory.Get(enrageSkillRow, dmg, 100);
            _enrageSkill = enrageSkill;
        }

        protected override BattleStatus.Skill UseSkill()
        {
            var index = _actionCount % _orderedSkills.Count;
            var skill = _orderedSkills[index];
            var usedSkill = skill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    ATK,
                    skill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                )
            );

            Simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        public override bool IsHit(CharacterBase caster)
        {
            if (!IgnoreLevelCorrectionOnHit)
            {
                return base.IsHit(caster);
            }

            var isHit = HitHelper.IsHitWithoutLevelCorrection(
                caster.Level,
                caster.HIT,
                Level,
                HIT,
                Simulator.Random.Next(0, 100));
            if (!isHit)
            {
                caster.AttackCount = 0;
            }

            return isHit;
        }

        public void Enrage()
        {
            Enraged = true;
            var usedSkill = _enrageSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    ATK,
                    _enrageSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
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
