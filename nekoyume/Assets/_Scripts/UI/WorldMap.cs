using UnityEngine;
using Nekoyume.BlockChain;

namespace Nekoyume.UI
{
    public class WorldMap : Widget
    {
        public GameObject world;
        public GameObject chapter;
        public Transform stages;

        private int _selectedStage = -1;
        public int SelectedStage { 
            get
            {
                if (_selectedStage < 0)
                {
                    _selectedStage = States.Instance.currentAvatarState.Value.worldStage;
                }
                return _selectedStage;
            }
            set
            {
                _selectedStage = value;
            }
        }

        public override void Show()
        {
            Find<Gold>().Close();
            ShowChapter();
            base.Show();
        }

        public void ShowWorldMap()
        {
            world.SetActive(true);
            chapter.SetActive(false);
        }

        public void ShowChapter()
        {
            world.SetActive(false);
            chapter.SetActive(true);

            int maxStage = States.Instance.currentAvatarState.Value.worldStage;
            for (int i = 0; i < stages.childCount; ++i)
            {
                var child = stages.GetChild(i);
                var stage = child.GetComponent<WorldMapStage>();
                if (stage)
                {
                    stage.Parent = this;
                    stage.Value = i + 1;
                    stage.label.text = stage.Value.ToString();
                    stage.icon.enabled = false;
                    stage.button.enabled = maxStage >= stage.Value;
                    if (SelectedStage == stage.Value)
                        stage.SetImage(stage.selectedImage);
                    else if (maxStage > stage.Value)
                        stage.SetImage(stage.clearedImage);
                    else if (!stage.button.enabled)
                        stage.SetImage(stage.disabledImage);
                    else
                        stage.SetImage(stage.normalImage);
                }
            }
        }

        public override void Close()
        {
            Find<Gold>().Show();
            Find<QuestPreparation>().OnChangeStage();
            base.Close();
        }

        public void OnCloseWorld()
        {
            Close();
        }

        public void OnCloseChapter()
        {
            Close();
        }
    }
}
