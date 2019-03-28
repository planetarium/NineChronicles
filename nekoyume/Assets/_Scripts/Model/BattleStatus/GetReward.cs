using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class GetReward : EventBase
    {
        public List<ItemBase> rewards;
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetReward(rewards);
        }
    }
}
