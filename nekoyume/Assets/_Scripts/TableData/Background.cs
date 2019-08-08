using System;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class Background : Sheet<int, Background.Row>
    {
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int StageId { get; private set; }
            public string Background { get; private set; }
            public string BGM { get; private set; }

            public int Key => StageId;
            
            public void Set(string[] fields)
            {
                var index = 0;
                StageId = int.TryParse(fields[index++], out var stageId) ? stageId : 0;
                Background = fields[index++];
                BGM = string.IsNullOrEmpty(fields[index])
                    ? AudioController.MusicCode.StageGreen
                    : fields[index];
            }
        }
    }
}
