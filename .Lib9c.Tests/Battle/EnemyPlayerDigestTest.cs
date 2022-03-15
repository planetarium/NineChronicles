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
        private readonly TableSheets _tableSheets;

        public EnemyPlayerDigestTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = new AvatarState(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                1234,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new PrivateKey().ToAddress(),
                "test"
            );
            avatarState.hair = 2;
            avatarState.lens = 3;
            avatarState.ear = 4;
            avatarState.tail = 5;

            var costumeRow = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeRow.CostumeId], new TestRandom());
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var costume2Row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var costume2 = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costume2Row.CostumeId], new TestRandom());
            avatarState.inventory.AddItem(costume2);

            var weaponRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon);
            var weapon = (Weapon)ItemFactory.CreateItem(_tableSheets.ItemSheet[weaponRow.Id], new TestRandom());
            weapon.equipped = true;
            avatarState.inventory.AddItem(weapon);

            var armorRow =
                _tableSheets.EquipmentItemSheet.Values.First(
                    r => r.ItemSubType == ItemSubType.Armor);
            var armor = (Armor)ItemFactory.CreateItem(_tableSheets.ItemSheet[armorRow.Id], new TestRandom());
            avatarState.inventory.AddItem(armor);
            _avatarState = avatarState;
        }

        [Fact]
        public void Constructor()
        {
            var digest = new EnemyPlayerDigest(_avatarState);

            Assert.Equal(_avatarState.NameWithHash, digest.NameWithHash);
            Assert.Equal(_avatarState.characterId, digest.CharacterId);
            Assert.Equal(_avatarState.level, digest.Level);
            Assert.Equal(2, digest.HairIndex);
            Assert.Equal(3, digest.LensIndex);
            Assert.Equal(4, digest.EarIndex);
            Assert.Equal(5, digest.TailIndex);
            Assert.Single(digest.Equipments);
            Assert.Single(digest.Costumes);

            var enemyPlayer = new EnemyPlayer(
                digest,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);

            Assert.Single(enemyPlayer.Equipments);
            Assert.Single(enemyPlayer.Costumes);
        }

        [Fact]
        public void Serialize()
        {
            var digest = new EnemyPlayerDigest(_avatarState);
            var serialized = digest.Serialize();
            var deserialized = new EnemyPlayerDigest((List)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
