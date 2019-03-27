using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class DropBox : EventBase
    {
        public override bool skip => false;
        public List<ItemBase> items;

        public override void Execute(IStage stage)
        {
            stage.DropBox(items);
        }
    }
}
