using System;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class BackgroundSheet : Sheet<int, BackgroundSheet.Row>
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
                StageId = int.TryParse(fields[0], out var stageId) ? stageId : 0;
                Background = fields[1];
                BGM = string.IsNullOrEmpty(fields[2])
                    ? AudioController.MusicCode.StageGreen
                    : fields[2];
            }
        }
    }
}
