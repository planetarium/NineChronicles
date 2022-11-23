namespace Lib9c.Tests.Model.Arena
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class PlayerDigestTest
    {
        private readonly AvatarState _avatarState;
        private readonly ArenaAvatarState _arenaAvatarState;
        private readonly TableSheets _tableSheets;

        public PlayerDigestTest()
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
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeRow.CostumeId], new TestRandom(1));
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var costume2Row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var costume2 = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costume2Row.CostumeId], new TestRandom(2));
            avatarState.inventory.AddItem(costume2);

            var weaponRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon);
            var weapon = (Weapon)ItemFactory.CreateItem(_tableSheets.ItemSheet[weaponRow.Id], new TestRandom(3));
            weapon.equipped = true;
            avatarState.inventory.AddItem(weapon);

            var armorRow =
                _tableSheets.EquipmentItemSheet.Values.First(
                    r => r.ItemSubType == ItemSubType.Armor);
            var armor = (Armor)ItemFactory.CreateItem(_tableSheets.ItemSheet[armorRow.Id], new TestRandom(4));
            avatarState.inventory.AddItem(armor);
            _avatarState = avatarState;

            _arenaAvatarState = new ArenaAvatarState(_avatarState);
            _arenaAvatarState.UpdateEquipment(new List<Guid>() { weapon.ItemId, armor.ItemId });
            _arenaAvatarState.UpdateCostumes(new List<Guid>() { costume.ItemId, costume2.ItemId });
        }

        [Fact]
        public void Constructor()
        {
            var digest = new ArenaPlayerDigest(_avatarState, _arenaAvatarState);

            Assert.Equal(_avatarState.NameWithHash, digest.NameWithHash);
            Assert.Equal(_avatarState.characterId, digest.CharacterId);
            Assert.Equal(2, digest.HairIndex);
            Assert.Equal(3, digest.LensIndex);
            Assert.Equal(4, digest.EarIndex);
            Assert.Equal(5, digest.TailIndex);
            Assert.Equal(2, digest.Equipments.Count);
            Assert.Equal(2, digest.Costumes.Count);

            var enemyPlayer = new EnemyPlayer(digest, _tableSheets.GetArenaSimulatorSheetsV1());
            var player = new Player(digest, _tableSheets.GetArenaSimulatorSheetsV1());

            Assert.Equal(2, enemyPlayer.Equipments.Count);
            Assert.Equal(2, enemyPlayer.Costumes.Count);
            Assert.Equal(2, player.Costumes.Count);
            Assert.Equal(2, player.Costumes.Count);
            Assert.Equal(enemyPlayer.Equipments.FirstOrDefault(), player.Equipments.FirstOrDefault());
            Assert.Equal(enemyPlayer.Costumes.FirstOrDefault(), player.Costumes.FirstOrDefault());
        }

        [Fact]
        public void SerializeWithoutRune()
        {
            var digest = new ArenaPlayerDigest(_avatarState, _arenaAvatarState);
            var serialized = digest.Serialize();
            var deserialized = new ArenaPlayerDigest((List)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }

        [Fact]
        public void Serialize()
        {
            var digest = new ArenaPlayerDigest(
                _avatarState,
                _arenaAvatarState.Equipments,
                _arenaAvatarState.Costumes,
                new List<RuneState>());
            var serialized = digest.Serialize();
            var deserialized = new ArenaPlayerDigest((List)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
