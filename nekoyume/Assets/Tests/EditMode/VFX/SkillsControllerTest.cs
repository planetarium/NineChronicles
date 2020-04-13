using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Helper;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.VFX
{
    // NOTE: 각 API 테스트를 위한 준비물이 많아서 아직 모든 API에 대해서 해당 API를 직접 사용하는 테스트를 작성하지는 못했어요.
    public class SkillsControllerTest
    {
        private TableSheets _tableSheets;
        private SkillVFX[] _skills;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            _skills = Resources.LoadAll<SkillVFX>("VFX/Skills");
        }

        [TearDown]
        public void TearDown()
        {
            _tableSheets = null;
        }

        // SkillCastingVFX Get(Vector3 position, Model.BattleStatus.Skill.SkillInfo skillInfo)
        [Test]
        public void CheckSkillVFXPrefabs([Values(17)] int endStageId)
        {
            var monsterIds = _tableSheets.StageWaveSheet.OrderedList
                .Where(item => item.Key <= endStageId)
                .SelectMany(item => item.Waves)
                .SelectMany(wave => wave.Monsters)
                .Select(monsterData => monsterData.CharacterId)
                .Distinct()
                .ToList();

            var skillIds = new List<int>();
            foreach (var monsterId in monsterIds)
            {
                skillIds.AddRange(
                    _tableSheets.EnemySkillSheet.OrderedList
                        .Where(item => item.characterId.Equals(monsterId))
                        .Select(item => item.skillId));
            }

            skillIds = skillIds
                .Distinct()
                .ToList();

            var skillRows = new List<SkillSheet.Row>();
            foreach (var skillId in skillIds)
            {
                Assert.IsTrue(_tableSheets.SkillSheet.TryGetValue(skillId, out var row));
                skillRows.Add(row);
            }

            foreach (var skillRow in skillRows)
            {
                var elemental = skillRow.ElementalType;
                var skillName = $"casting_{elemental}".ToLower();
                Assert.IsTrue(_skills.Any(skill => skill.name.Equals(skillName)));
            }
        }
    }
}
