using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Synopsis : Widget
    {
        public void End()
        {
            Game.Event.OnNestEnter.Invoke();
            Widget.Find<Login>()?.Show();
            Close();
        }
    }
}
