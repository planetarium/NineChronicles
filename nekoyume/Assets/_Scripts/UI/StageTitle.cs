using Nekoyume.TableData;
using TMPro;

namespace Nekoyume.UI
{
    public class StageTitle : PopupWidget
    {
        public TextMeshProUGUI textStage;
        
        public void Show(int stageId)
        {
            if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stageId, out var worldRow) &&
                worldRow.TryGetStageNumber(stageId, out var stageNumber))
            {
                textStage.text = $"STAGE {stageNumber}";
            }
            else
            {
                textStage.text = "STAGE ?";    
            }
            
            base.Show();
        }
    }
}
