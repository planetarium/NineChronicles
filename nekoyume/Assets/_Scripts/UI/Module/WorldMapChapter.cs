using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class WorldMapChapter : MonoBehaviour
    {
        public WorldMapStage[] stages;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public Model.WorldMapChapter Model { get; private set; }

        public void SetModel(Model.WorldMapChapter model)
        {
            if (model == null)
            {
                Clear();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            UpdateView();
        }

        public void Clear()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
            UpdateView();
        }

        private void UpdateView()
        {
            if (Model.stages.Count > stages.Length)
            {
                throw new ArgumentOutOfRangeException(
                    $"Model.stages.Count({Model.stages.Count}) > stages.Length({stages.Length})");
            }

            for (var i = 0; i < stages.Length; i++)
            {
                var view = stages[i];
                if (Model.stages.Count > i)
                {
                    view.SetModel(Model.stages[i]);
                    view.tweenMove.StartDelay = i * 0.03f;
                    view.tweenAlpha.StartDelay = i * 0.03f;
                }
                else
                {
                    view.Clear();
                }
            }
        }
    }
}
