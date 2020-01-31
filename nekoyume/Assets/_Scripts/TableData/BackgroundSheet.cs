using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class BackgroundSheet : Sheet<int, BackgroundSheet.Row>
    {
        // FIXME AudioController.MusicCode.StageGreen과 중복
        private const string DefaultBGM = "bgm_stage_green";
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => StageId;
            public int StageId { get; private set; }
            public string Background { get; private set; }
            public string BGM { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = int.TryParse(fields[0], out var stageId) ? stageId : 0;
                Background = fields[1];
                BGM = string.IsNullOrEmpty(fields[2])
                    ? DefaultBGM
                    : fields[2];
            }
        }

        public BackgroundSheet() : base(nameof(BackgroundSheet))
        {
        }
    }
}
