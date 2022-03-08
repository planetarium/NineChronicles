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
            avatarState.hair = 2;
            avatarState.lens = 3;
            avatarState.ear = 4;
            avatarState.tail = 5;

            var costumeRow = tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(tableSheets.ItemSheet[costumeRow.CostumeId], new TestRandom());
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var weaponRow = tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon);
            var weapon = (Weapon)ItemFactory.CreateItem(tableSheets.ItemSheet[weaponRow.Id], new TestRandom());
            weapon.equipped = true;
            avatarState.inventory.AddItem(weapon);
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
