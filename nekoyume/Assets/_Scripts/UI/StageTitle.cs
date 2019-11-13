using TMPro;

namespace Nekoyume.UI
{
    public class StageTitle : PopupWidget
    {
        public TextMeshProUGUI textStage;
        
        public void Show(int stage)
        {
            textStage.text = $"STAGE {stage}";
            base.Show();
        }
    }
}
