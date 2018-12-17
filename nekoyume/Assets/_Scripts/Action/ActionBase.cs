using System.Collections.Generic;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public abstract class ActionBase
    {
        public abstract Context Execute(Context ctx);

        public abstract Dictionary<string, string> ToDetails();
    }
}
