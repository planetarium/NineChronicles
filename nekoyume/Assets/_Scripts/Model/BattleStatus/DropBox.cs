using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class DropBox : EventBase
    {
        public List<ItemBase> items;

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoDropBox(items);
        }
    }
}
