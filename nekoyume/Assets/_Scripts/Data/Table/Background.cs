using Nekoyume.Game.Controller;

namespace Nekoyume.Data.Table
{
    public class Background : Row
    {
        public int stageId = 0;
        public string background = "";
        public string bgm = AudioController.MusicCode.StageGreen;
    }
}
