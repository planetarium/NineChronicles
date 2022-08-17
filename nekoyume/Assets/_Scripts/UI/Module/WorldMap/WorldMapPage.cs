using Nekoyume.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class WorldMapPage : MonoBehaviour
    {
        [SerializeField]
        private List<WorldMapStage> stages = null;
        [SerializeField]
        private Image line = null;
        [SerializeField]
        private Image line2 = null;
        [SerializeField]
        private Image background = null;

        public IReadOnlyList<WorldMapStage> Stages => stages;

        public void Show(
            List<WorldMapStage.ViewModel> stageModels,
            Sprite backgroundSprite)
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

            var activeCount = stageModels.Count(i => i.State.Value == WorldMapStage.State.Normal);
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
            var enable = activeCount > 10;
            line.gameObject.SetActive(enable);
            line2.gameObject.SetActive(!enable);

            background.sprite = backgroundSprite;
        }
    }
}
