using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;

namespace Nekoyume.Battle
{
    public class TestSimulator : Simulator
    {
        public override IEnumerable<ItemBase> Reward => new List<ItemBase>();

        public TestSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            SimulatorSheets simulatorSheets)
            : base(random, avatarState, foods, simulatorSheets)
        {
        }
    }
}
