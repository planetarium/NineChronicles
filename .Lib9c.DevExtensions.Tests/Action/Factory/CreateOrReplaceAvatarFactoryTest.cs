#nullable enable

using System;
using System.Linq;
using Lib9c.DevExtensions.Action.Factory;
using Xunit;

namespace Lib9c.DevExtensions.Tests.Action.Factory
{
    public class CreateOrReplaceAvatarFactoryTest
    {
        [Theory]
        [MemberData(
            nameof(CreateOrReplaceAvatarTest.Get_Execute_Success_MemberData),
            MemberType = typeof(CreateOrReplaceAvatarTest))]
        public void TryGetByBlockIndex_Success(
            long blockIndex,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int equipmentId, int level)[]? equipments,
            (int consumableId, int count)[]? foods,
            int[]? costumeIds,
            (int runeId, int level)[]? runes,
            (int stageId, int[] crystalRandomBuffIds)? crystalRandomBuff)
        {
            var (e, r) = CreateOrReplaceAvatarFactory
                .TryGetByBlockIndex(
                    blockIndex,
                    avatarIndex,
                    name,
                    hair,
                    lens,
                    ear,
                    tail,
                    level,
                    equipments,
                    foods,
                    costumeIds,
                    runes,
                    crystalRandomBuff);
            Assert.Null(e);
            Assert.NotNull(r);
            Assert.Equal(avatarIndex, r!.AvatarIndex);
            Assert.Equal(name, r.Name);
            Assert.Equal(hair, r.Hair);
            Assert.Equal(lens, r.Lens);
            Assert.Equal(ear, r.Ear);
            Assert.Equal(tail, r.Tail);
            Assert.Equal(level, r.Level);
            if (equipments is null)
            {
                Assert.Empty(r.Equipments);
            }
            else
            {
                Assert.True(equipments.SequenceEqual(r.Equipments));
            }

            if (foods is null)
            {
                Assert.Empty(r.Foods);
            }
            else
            {
                Assert.True(foods.SequenceEqual(r.Foods));
            }

            if (costumeIds is null)
            {
                Assert.Empty(r.CostumeIds);
            }
            else
            {
                Assert.True(costumeIds.SequenceEqual(r.CostumeIds));
            }

            if (runes is null)
            {
                Assert.Empty(r.Runes);
            }
            else
            {
                Assert.True(runes.SequenceEqual(r.Runes));
            }

            if (crystalRandomBuff is null)
            {
                Assert.Null(r.CrystalRandomBuff);
            }
            else
            {
                Assert.NotNull(r.CrystalRandomBuff);
                Assert.Equal(crystalRandomBuff.Value.stageId, r.CrystalRandomBuff!.Value.stageId);
                Assert.True(crystalRandomBuff.Value.crystalRandomBuffIds.SequenceEqual(
                    r.CrystalRandomBuff.Value.crystalRandomBuffIds));
            }
        }

        [Theory]
        [MemberData(
            nameof(CreateOrReplaceAvatarTest.Get_Execute_Failure_MemberData),
            MemberType = typeof(CreateOrReplaceAvatarTest))]
        public void TryGetByBlockIndex_Failure(
            long blockIndex,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int equipmentId, int level)[]? equipments,
            (int consumableId, int count)[]? foods,
            int[]? costumeIds,
            (int runeId, int level)[]? runes,
            (int stageId, int[] crystalRandomBuffIds)? crystalRandomBuff)
        {
            var (e, r) = CreateOrReplaceAvatarFactory
                .TryGetByBlockIndex(
                    blockIndex,
                    avatarIndex,
                    name,
                    hair,
                    lens,
                    ear,
                    tail,
                    level,
                    equipments,
                    foods,
                    costumeIds,
                    runes,
                    crystalRandomBuff);
            Assert.NotNull(e);
            Assert.IsType<ArgumentException>(e);
            Assert.Null(r);
        }
    }
}
