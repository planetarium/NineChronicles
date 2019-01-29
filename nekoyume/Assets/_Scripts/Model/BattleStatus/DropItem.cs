using System;

namespace Nekoyume.Model
{
    [Serializable]
    public class DropItem : EventBase
    {
        public override void Execute(IStage stage)
        {
            stage.DropItem((Monster)character);
        }
    }
}
