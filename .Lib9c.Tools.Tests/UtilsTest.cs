using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Nekoyume.Model.State;
using Xunit;

namespace Lib9c.Tools.Tests
{
    public class UtilsTest
    {
        [Fact]
        public void CreateActivationKeyTest()
        {
            uint countOfKeys = 10;
            Utils.CreateActivationKey(
                out var pendingActivationState,
                out var activationKeys,
                countOfKeys);
            
            Assert.Equal(countOfKeys, (uint)activationKeys.Count);
            Assert.Equal(activationKeys.Count, pendingActivationState.Count);

            foreach (var item in activationKeys.Select(((key, index) => (key, index))))
            {
                Assert.Equal(item.key.PendingAddress, pendingActivationState[item.index].address);
            }
        }

        [Fact]
        public void ImportSheetTest()
        {
            IDictionary<string, string> sheets = Utils.ImportSheets(Path.Join("Data", "TableCSV"));
            
            string enhancement = Assert.Contains("EnhancementCostSheet", sheets);
            string gameConfig = Assert.Contains("GameConfigSheet", sheets);

            Assert.Contains("id,item_sub_type,grade,level,cost", enhancement);
            Assert.Contains("key,value", gameConfig);
        }
    }
}
