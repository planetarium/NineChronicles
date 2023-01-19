using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions.Action;
using Lib9c.Tests.Action;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Xunit;
using static Lib9c.SerializeKeys;

namespace Lib9c.DevExtensions.Tests.Action
{
    public class CreateOrReplaceAvatarTest
    {
        private readonly IAccountStateDelta _initialStates;

        public CreateOrReplaceAvatarTest()
        {
            _initialStates = new Lib9c.Tests.Action.State();

#pragma warning disable CS0618
            var ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _initialStates = _initialStates.SetState(
                GoldCurrencyState.Address,
                new GoldCurrencyState(ncgCurrency).Serialize());
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialStates = _initialStates.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            _initialStates = _initialStates.SetState(
                gameConfigState.address,
                gameConfigState.Serialize());
        }

        public static IEnumerable<object[]> Get_Execute_Success_MemberData()
        {
            yield return new object[]
            {
                0, 0, "AB", 0, 0, 0, 0, 1,
                null,
            };

            yield return new object[]
            {
                long.MaxValue / 2, // long.MaxValue / 2: Avoid overflow
                GameConfig.SlotCount - 1,
                "12345678901234567890",
                int.MaxValue,
                int.MaxValue,
                int.MaxValue,
                int.MaxValue,
                int.MaxValue,
                new[]
                {
                    (10111000, 0),
                    (10111000, 21), // 21: See also EnhancementCostSheetV2.csv
                    (10211000, 0),
                    (10211000, 21),
                    (10311000, 0),
                    (10311000, 21),
                    (10411000, 0),
                    (10411000, 21),
                    (10511000, 0),
                    (10511000, 21),
                },
            };
        }

        public static IEnumerable<object[]>
            Get_Execute_Failure_MemberData()
        {
            // avatarIndex < 0
            yield return new object[]
            {
                0, -1, "AB", 0, 0, 0, 0, 1,
                null,
            };

            // avatarIndex >= GameConfig.SlotCount
            yield return new object[]
            {
                0, GameConfig.SlotCount, "AB", 0, 0, 0, 0, 1,
                null,
            };

            // name's length < 1
            yield return new object[]
            {
                0, 0, "A", 0, 0, 0, 0, 1,
                null,
            };

            // name's length > 20
            yield return new object[]
            {
                0, 0, "123456789012345678901", 0, 0, 0, 0, 1,
                null,
            };

            // hair < 0
            yield return new object[]
            {
                0, 0, "AB", -1, 0, 0, 0, 1,
                null,
            };

            // lens < 0
            yield return new object[]
            {
                0, 0, "AB", 0, -1, 0, 0, 1,
                null,
            };

            // ear < 0
            yield return new object[]
            {
                0, 0, "AB", 0, 0, -1, 0, 1,
                null,
            };

            // tail < 0
            yield return new object[]
            {
                0, 0, "AB", 0, 0, 0, -1, 1,
                null,
            };

            // level < 1
            yield return new object[]
            {
                0, 0, "AB", 0, 0, 0, 0, 0,
                null,
            };

            // equipment's itemId < 0
            yield return new object[]
            {
                0, 0, "AB", 0, 0, 0, 0, 1,
                new[]
                {
                    (-1, 0),
                },
            };

            // equipment's enhancement < 0
            yield return new object[]
            {
                0, 0, "AB", 0, 0, 0, 0, 1,
                new[]
                {
                    (0, -1),
                },
            };
        }

        [Theory]
        [MemberData(nameof(Get_Execute_Success_MemberData))]
        public void Serialize(
            long blockIndex,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int itemId, int enhancement)[] equipments)
        {
            var action = new CreateOrReplaceAvatar(
                avatarIndex,
                name,
                hair,
                lens,
                ear,
                tail,
                level,
                equipments);
            var ser = action.PlainValue;
            var de = new CreateOrReplaceAvatar();
            de.LoadPlainValue(ser);
            Assert.Equal(action.AvatarIndex, de.AvatarIndex);
            Assert.Equal(action.Name, de.Name);
            Assert.Equal(action.Hair, de.Hair);
            Assert.Equal(action.Lens, de.Lens);
            Assert.Equal(action.Ear, de.Ear);
            Assert.Equal(action.Tail, de.Tail);
            Assert.Equal(action.Level, de.Level);
            Assert.True(action.Equipments.SequenceEqual(de.Equipments));
        }

        [Theory]
        [MemberData(nameof(Get_Execute_Success_MemberData))]
        public void Execute_Success(
            long blockIndex,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int itemId, int enhancement)[] equipments)
        {
            var agentAddr = new PrivateKey().ToAddress();
            Execute(
                _initialStates,
                blockIndex,
                agentAddr,
                avatarIndex,
                name,
                hair,
                lens,
                ear,
                tail,
                level,
                equipments);
        }

        [Theory]
        [MemberData(nameof(Get_Execute_Failure_MemberData))]
        public void Execute_Failure(
            long blockIndex,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int itemId, int enhancement)[] equipments)
        {
            var agentAddr = new PrivateKey().ToAddress();
            Assert.Throws<ArgumentException>(() => Execute(
                _initialStates,
                blockIndex,
                agentAddr,
                avatarIndex,
                name,
                hair,
                lens,
                ear,
                tail,
                level,
                equipments));
        }

        private static void Execute(
            IAccountStateDelta previousStates,
            long blockIndex,
            Address agentAddr,
            int avatarIndex,
            string name,
            int hair,
            int lens,
            int ear,
            int tail,
            int level,
            (int itemId, int enhancement)[] equipments,
            Address? avatarAddr = null)
        {
            var action = new CreateOrReplaceAvatar(
                avatarIndex,
                name,
                hair,
                lens,
                ear,
                tail,
                level,
                equipments);
            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = agentAddr,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = blockIndex,
            });
            var agent = new AgentState((Dictionary)nextStates.GetState(agentAddr));
            Assert.Single(agent.avatarAddresses);
            Assert.True(agent.avatarAddresses.ContainsKey(action.AvatarIndex));
            avatarAddr ??= agent.avatarAddresses[action.AvatarIndex];
            var avatar = new AvatarState((Dictionary)nextStates.GetState(avatarAddr.Value));
            Assert.Equal(action.Name, avatar.name);
            Assert.Equal(action.Hair, avatar.hair);
            Assert.Equal(action.Lens, avatar.lens);
            Assert.Equal(action.Ear, avatar.ear);
            Assert.Equal(action.Tail, avatar.tail);
            Assert.Equal(action.Level, avatar.level);

            var inventoryAddr = avatarAddr.Value.Derive(LegacyInventoryKey);
            var inventory =
                new Inventory((List)nextStates.GetState(inventoryAddr));
            var inventoryEquipments = inventory.Equipments.ToArray();
            var equipmentItemRecipeSheet =
                nextStates.GetSheet<EquipmentItemRecipeSheet>();
            var equipmentItemSubRecipeSheetV2 =
                nextStates.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var equipmentItemOptionSheet =
                nextStates.GetSheet<EquipmentItemOptionSheet>();
            for (var i = 0; i < action.Equipments.Length; i++)
            {
                var (itemId, enhancement) = action.Equipments[i];
                var equipment = inventoryEquipments.First(e =>
                    e.Id == itemId &&
                    e.level == enhancement);
                var recipe = equipmentItemRecipeSheet.OrderedList!.First(r =>
                    r.ResultEquipmentId == equipment.Id);
                Assert.NotNull(recipe);
                Assert.Equal(3, recipe.SubRecipeIds.Count);
                Assert.True(equipmentItemSubRecipeSheetV2.TryGetValue(
                    recipe.SubRecipeIds[1],
                    out var subRecipe));
                var options = subRecipe.Options
                    .Select(e => equipmentItemOptionSheet[e.Id])
                    .ToArray();
                var statOptions = options
                    .Where(e => e.StatType != StatType.NONE)
                    .Aggregate(
                        new Dictionary<StatType, int>(),
                        (dict, row) =>
                        {
                            if (dict.ContainsKey(row.StatType))
                            {
                                dict[row.StatType] += row.StatMax;
                            }
                            else
                            {
                                dict[row.StatType] = row.StatMax;
                            }

                            return dict;
                        });
                foreach (var statOption in statOptions)
                {
                    Assert.Contains(
                        equipment.StatsMap.GetAdditionalStats(),
                        stat => stat.StatType == statOption.Key &&
                                stat.AdditionalValue == statOption.Value);
                }

                var skillOptions = options
                    .Where(e => e.StatType == StatType.NONE)
                    .ToArray();
                foreach (var skillOption in skillOptions)
                {
                    Assert.Contains(
                        equipment.Skills,
                        e =>
                            e.SkillRow.Id == skillOption.SkillId &&
                            e.Power == skillOption.SkillDamageMax &&
                            e.Chance == skillOption.SkillChanceMax);
                }
            }
        }
    }
}
