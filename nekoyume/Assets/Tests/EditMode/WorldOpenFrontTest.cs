using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [Test]
        public void FetchFailure_KeepsLastResult()
        {
            var cache = new WorldOpenCache();
            var worldSheet = MakeWorldSheet(11, 501, 550);
            var opened = WorldOpenFront.CreateState((Text)(Header + "1,11,550,,0,100,0\n"), null);
            Assert.IsTrue(cache.FetchAsync(200, () => Task.FromResult(opened), worldSheet).Result);
            Assert.AreEqual(550, cache.GetOpenedStageEnd(worldSheet[11]));

            Assert.IsFalse(cache.FetchAsync(
                210,
                () => Task.FromException<IWorldState>(new InvalidOperationException()),
                worldSheet).Result);
            Assert.AreEqual(550, cache.GetOpenedStageEnd(worldSheet[11]));
            Assert.AreEqual(200, cache.FetchedTipIndex);
            Assert.AreSame(opened, cache.State);
        }

        [Test]
        public void NeverFetched_LocksGatedStages()
        {
            var cache = new WorldOpenCache();
            var worldSheet = MakeWorldSheet(11, 501, 550);
            Assert.AreEqual(520, cache.GetOpenedStageEnd(worldSheet[11]));
            Assert.AreEqual(520, WorldOpenFront.GetUngatedStageEnd(worldSheet[11]));
        }

        [Test]
        public void Evaluate_LaterBlock_OpensByFallbackWithoutRefetch()
        {
            var cache = new WorldOpenCache();
            var worldSheet = MakeWorldSheet(11, 501, 550);
            var state = WorldOpenFront.CreateState((Text)(Header + "1,11,550,,0,1000,0\n"), null);
            var fetchCount = 0;
            cache.FetchAsync(
                990,
                () =>
                {
                    fetchCount++;
                    return Task.FromResult(state);
                },
                worldSheet).Wait();
            Assert.AreEqual(520, cache.GetOpenedStageEnd(worldSheet[11])); // 991

            cache.Evaluate(999, worldSheet);
            Assert.AreEqual(520, cache.GetOpenedStageEnd(worldSheet[11]));
            cache.Evaluate(1000, worldSheet);
            Assert.AreEqual(550, cache.GetOpenedStageEnd(worldSheet[11]));
            Assert.AreEqual(1, fetchCount);
        }

        private static WorldSheet MakeWorldSheet(int id, int begin, int end)
        {
            var sheet = new WorldSheet();
            sheet.Set($"id,name,stage_begin,stage_end\n{id},w,{begin},{end}\n");
            return sheet;
        }

        private static WorldSheet.Row World(int id, int begin, int end)
        {
            var sheet = new WorldSheet();
            sheet.Set($"id,name,stage_begin,stage_end\n{id},w,{begin},{end}\n");
            return sheet[id];
        }
    }
}
