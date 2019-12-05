using TMPro;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GlowingButton : NormalButton
    {
        public Image glowImage;
        public TextMeshProUGUI glowText;
        public bool isGlowing;

        public void StartGlow()
        {
            isGlowing = true;
        }

        public void StopGlow()
        {
            isGlowing = false;
        }
    }
}
