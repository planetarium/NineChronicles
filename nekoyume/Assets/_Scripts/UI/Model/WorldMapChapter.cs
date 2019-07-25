using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Nekoyume.UI.Model
{
    public class WorldMapChapter : IDisposable
    {
        public readonly List<WorldMapStage> stages;

        public WorldMapChapter(IEnumerable<WorldMapStage> stageModels)
        {
            stages = new List<WorldMapStage>(stageModels);
        }

        public void Dispose()
        {
            foreach (var stage in stages)
            {
                stage.Dispose();
            }
        }
    }
}
