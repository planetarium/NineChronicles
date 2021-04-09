namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentTest
    {
        private readonly EquipmentItemSheet.Row _equipmentRow;
        private readonly TableSheets _tableSheets;

        public EquipmentTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentRow = _tableSheets.EquipmentItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_equipmentRow);

            var costume = new Equipment(_equipmentRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Equipment((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_equipmentRow);

            var costume = new Equipment(_equipmentRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Equipment)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void Deserialize_From_Legacy()
        {
            var row = _tableSheets.EquipmentItemSheet.First;
            var itemUsable = (Equipment)ItemFactory.CreateItemUsable(row, default, 0);
            var skillIds = new[] { 100001, 100003 };
            var buffIds = new[] { 200000, 210000 };
            for (var index = 0; index < skillIds.Length; index++)
            {
                var skillId = skillIds[index];
                var skillRow = _tableSheets.SkillSheet[skillId];
                var skill = SkillFactory.Get(skillRow, 1, index + 1);
                itemUsable.Skills.Add(skill);

                var buffId = buffIds[index];
                var buffRow = _tableSheets.SkillSheet[buffId];
                var buff = (BuffSkill)SkillFactory.Get(buffRow, 1, index + 1);
                itemUsable.BuffSkills.Add(buff);
            }

            var serialized = itemUsable.SerializeLegacy();
            var deserialized = new Weapon((Dictionary)serialized);

            Assert.Equal(itemUsable, deserialized);
        }
    }
}
