using Nekoyume.EnumType;
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

        public void Show(StageType stageType, int stageId)
        {
            var stageText = StageInformation.GetStageIdString(stageType, stageId, true);
            textStage.text = $"STAGE {stageText}";
            base.Show();
        }
    }
}
