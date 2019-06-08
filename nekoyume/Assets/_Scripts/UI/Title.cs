using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Title : Widget
    {
        public bool ready = false;

        public void OnClick()
        {
            if (!ready)
                return;

            Widget.Find<Synopsis>()?.Show();
            Close();
        }

        public void Ready()
        {
            ready = true;
        }
    }
}
