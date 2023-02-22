using System;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Craft;
using Lib9c.Tests;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Xunit;

namespace Lib9c.DevExtensions.Tests.Action.Craft
{
    public class UnlockRecipeTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly Address _recipeAddress;

        public UnlockRecipeTest()
        {
            (_tableSheets, _agentAddress, _avatarAddress, _, _initialStateV2) =
                InitializeUtil.InitializeStates(isDevEx: true);
            _recipeAddress = _avatarAddress.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void UnlockRecipeTest_Equipment(ItemSubType targetType)
        {
            var random = new Random();
            var equipmentList = _tableSheets.EquipmentItemSheet.Values.Where(
                eq => eq.ItemSubType == targetType
            ).ToList();
            var equipment = equipmentList[random.Next(equipmentList.Count)];

            var recipeRow = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == equipment.Id
            );
            var action = new UnlockRecipe
            {
                AvatarAddress = _avatarAddress,
                TargetStage = recipeRow.UnlockStage,
            };

            var stateV2 = action.Execute(new ActionContext
            {
                PreviousStates = _initialStateV2,
                Signer = _agentAddress,
                BlockIndex = 0L,
            });

            Assert.True(stateV2.TryGetState(_recipeAddress, out List rawIds));
            var unlockedRecipeIds = rawIds.ToList(StateExtensions.ToInteger);
            Assert.Contains(recipeRow.UnlockStage, unlockedRecipeIds);
        }

        [Theory]
        [InlineData(ItemSubType.Food)]
        public void UnlockRecipeTest_Consumable(ItemSubType targetType)
        {
            const int targetStage = 6;
            var random = new Random();
            var foodList = _tableSheets.ConsumableItemSheet.Values.Where(
                con => con.ItemSubType == targetType
            ).ToList();
            var targetConsumable = foodList[random.Next(foodList.Count)];
            var recipeRow = _tableSheets.ConsumableItemRecipeSheet.Values.First(
                recipe => recipe.ResultConsumableItemId == targetConsumable.Id
            );
            var action = new UnlockRecipe
            {
                AvatarAddress = _avatarAddress,
                TargetStage = targetStage,
            };

            var stateV2 = action.Execute(new ActionContext
            {
                PreviousStates = _initialStateV2,
                Signer = _agentAddress,
                BlockIndex = 0L,
            });

            Assert.True(stateV2.TryGetState(_recipeAddress, out List _));
        }
    }
}
