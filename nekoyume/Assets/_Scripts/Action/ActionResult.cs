using System;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class ActionResult
    {
        public ItemUsable itemUsable;
    }
}
