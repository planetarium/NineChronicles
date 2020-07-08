using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class WorldMapPage : MonoBehaviour
    {
        [SerializeField]
        private List<WorldMapStage> stages = null;

        public IReadOnlyList<WorldMapStage> Stages => stages;

        public void Show(List<WorldMapStage.ViewModel> stageModels)
        {
            if (stageModels is null)
            {
                Destroy(gameObject);

                return;
            }

            var modelStagesCount = stageModels.Count;
            var viewStagesCount = stages.Count;
            if (modelStagesCount > viewStagesCount)
            {
                throw new ArgumentOutOfRangeException(
                    $"Model.stages.Count({modelStagesCount}) > stages.Length({viewStagesCount})");
            }

            for (var i = 0; i < viewStagesCount; i++)
            {
                var view = stages[i];
                if (modelStagesCount > i)
                {
                    view.Show(stageModels[i]);
                }
                else
                {
                    view.Hide();
                }
            }
        }
    }
}
