using Nekoyume.EnumType;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageTitle : Widget
    {
        public Text textStage;
        
        public override WidgetType WidgetType => WidgetType.Popup;

        public void Show(int stage)
        {
            textStage.text = $"STAGE {stage}";
            base.Show();
        }
    }
}
