using System.Collections.Generic;

namespace Nekoyume.Action
{
    public abstract class ActionBase
    {
        public abstract Model.Avatar Execute();

        public abstract Dictionary<string, string> ToDetails();
    }
}
