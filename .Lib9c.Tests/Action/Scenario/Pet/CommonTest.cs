namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Util;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Model.Pet;
    using Xunit;

    public class CommonTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStateV1;
        private readonly IAccountStateDelta _initialStateV2;

        public CommonTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2) =
                InitializeUtil.InitializeStates();
        }

        // Pet level range test
        [Theory]
        [InlineData(0)] // Min. level of pet is 1
        [InlineData(31)] // Max. level of pet is 30
        public void PetLevelRangeTest(int petLevel)
        {
            foreach (var petOptionType in Enum.GetValues<PetOptionType>())
            {
                Assert.Throws<KeyNotFoundException>(
                    () => _tableSheets.PetOptionSheet.Values.First(
                        pet => pet.LevelOptionMap[petLevel].OptionType == petOptionType
                    )
                );
            }
        }

        // You cannot use one pet to the multiple slots at the same time
        // You cannot use two pets to the one slot at the same time
        // You cannot use pet whose level is 0
    }
}
