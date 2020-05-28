using TMPro;

namespace Nekoyume.UI
{
    public class StageTitle : PopupWidget
    {
        public TextMeshProUGUI textStage;

        public void Show(int stageId)
        {
            textStage.text = $"STAGE {stageId}";
            base.Show();
        }
    }
}
