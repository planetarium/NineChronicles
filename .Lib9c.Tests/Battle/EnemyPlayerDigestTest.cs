namespace Lib9c.Tests
{
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class EnemyPlayerDigestTest
    {
        private readonly AvatarState _avatarState;
        private readonly EnemyPlayer _enemyPlayer;

        public EnemyPlayerDigestTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = new AvatarState(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                1234,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new PrivateKey().ToAddress(),
                "test"
            );

            var costumeRow = tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(tableSheets.ItemSheet[costumeRow.CostumeId], new TestRandom());
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var weaponRow = tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon);
            var weapon = (Weapon)ItemFactory.CreateItem(tableSheets.ItemSheet[weaponRow.Id], new TestRandom());
            weapon.equipped = true;
            avatarState.inventory.AddItem(weapon);
            _avatarState = avatarState;
            _enemyPlayer = new EnemyPlayer(avatarState, tableSheets.CharacterSheet, tableSheets.CharacterLevelSheet, tableSheets.EquipmentItemSetEffectSheet);
        }

        [Fact]
        public void Serialize()
        {
            var simulationPlayer = new EnemyPlayerDigest(_enemyPlayer);

            Assert.Single(simulationPlayer.Costumes);
            Assert.Single(simulationPlayer.Equipments);

            var serialized = simulationPlayer.Serialize();
            var deserialized = new EnemyPlayerDigest((List)serialized);

            Assert.Single(deserialized.Costumes);
            Assert.Single(deserialized.Equipments);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
