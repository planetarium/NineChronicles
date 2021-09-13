using TMPro;

namespace Nekoyume.UI
{
    public class StageTitle : Widget
    {
        public TextMeshProUGUI textStage;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
        }

        public void Show(int stageId)
        {
            textStage.text = $"STAGE {StageInformation.GetStageIdString(stageId, true)}";
            base.Show();
        }
    }
}
