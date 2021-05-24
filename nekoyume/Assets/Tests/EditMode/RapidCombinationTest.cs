using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RapidCombinationTest
    {
        [Test]
        public void CalculateHourglassCost([Values(1, 2, 3)] int diff)
        {
            var state = new GameConfigState();
            var row = new GameConfigSheet.Row();
            row.Set(new List<string>
            {
                "hourglass_per_block", "3"
            });
            state.Update(row);
            Assert.AreEqual(1, RapidCombination0.CalculateHourglassCount(state, diff));
        }
    }
}
