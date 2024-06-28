using Nekoyume.EnumType;
using TMPro;

namespace Nekoyume.UI
{
    public class AdventureBossStageBuff : Widget
    {
        public TextMeshProUGUI textStageBuff;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
        }

        public void Show(string stageBuffTitle)
        {
            textStageBuff.text = stageBuffTitle;
            base.Show();
        }
    }
}
