using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Module;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class WorldOpenFrontTest
    {
        private const string Header =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        private static readonly WorldSheet.Row World11 = World(11, 501, 550);

        [Test]
        public void NoSheet_LocksGatedStages()
        {
            var state = WorldOpenFront.CreateState(null, null);
            Assert.AreEqual(520, WorldOpenGate.GetOpenedStageEnd(state, World11, 100));
        }

        [Test]
        public void Confirmed_AfterDelay_OpensToStageEnd()
        {
            var state = WorldOpenFront.CreateState(
                (Text)(Header + "1,11,550,,0,100000,10\n"),
                new Dictionary<Address, IValue> { [WorldOpenModule.OpenKey(1)] = (Integer)100 });
            Assert.AreEqual(520, WorldOpenGate.GetOpenedStageEnd(state, World11, 109));
            Assert.AreEqual(550, WorldOpenGate.GetOpenedStageEnd(state, World11, 110));
        }

        [Test]
        public void Unconfirmed_BeforeFallback_StaysLocked()
        {
            var state = WorldOpenFront.CreateState((Text)(Header + "1,11,550,,0,1000,0\n"), null);
            Assert.AreEqual(520, WorldOpenGate.GetOpenedStageEnd(state, World11, 999));
            Assert.AreEqual(550, WorldOpenGate.GetOpenedStageEnd(state, World11, 1000));
        }

        [Test]
        public void UnknownKey_ReturnsNull()
        {
            var state = WorldOpenFront.CreateState(null, null);
            Assert.IsNull(state.GetAccountState(Addresses.WorldOpen).GetState(WorldOpenModule.OpenKey(1)));
            Assert.IsNull(state.GetAccountState(Addresses.GlobalCounter).GetState(new Address()));
        }

        private static WorldSheet.Row World(int id, int begin, int end)
        {
            var sheet = new WorldSheet();
            sheet.Set($"id,name,stage_begin,stage_end\n{id},w,{begin},{end}\n");
            return sheet[id];
        }
    }
}
