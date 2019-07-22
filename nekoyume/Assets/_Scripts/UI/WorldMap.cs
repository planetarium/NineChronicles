using UnityEngine;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using System.Linq;
using Assets.SimpleLocalization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldMap : Widget
    {
        public GameObject world;
        public GameObject chapter;
        public Transform stages;
        public Button btnPrevChapter;
        public Button btnNextChapter;
        public Text mainButtonText;
        public Text worldButtonText;
        public Text txtChapter;
        public Text txtStage;

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
        private int _currentChapter;

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            mainButtonText.text = LocalizationManager.Localize("UI_MAIN");
            worldButtonText.text = LocalizationManager.Localize("UI_WORLD");
        }

        #endregion
        
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
            var worldTable = Tables.instance.World;
            foreach (var worldData in worldTable)
            {
                if (worldData.Value.stageEnd > SelectedStage)
                {
                    LoadChapter(worldData.Value.id);
                    break;
                }
            }
        }

        public override void Close()
        {
            Find<Gold>().Show();
            Find<QuestPreparation>().OnChangeStage();
            base.Close();
        }

        public void CloseAll()
        {
            Find<Gold>().Show();
            base.Close();
            Find<QuestPreparation>().BackClick();
        }

        public void OnCloseWorld()
        {
            Close();
        }

        public void OnCloseChapter()
        {
            Close();
        }

        public void OnPrevChapter()
        {
            if (!Tables.instance.World.ContainsKey(_currentChapter - 1))
                return;

            LoadChapter(_currentChapter - 1);
        }

        public void OnNextChapter()
        {
            if (!Tables.instance.World.ContainsKey(_currentChapter + 1))
                return;

            LoadChapter(_currentChapter + 1);
        }

        public void LoadChapter(int chapterId)
        {
            if (!Tables.instance.World.TryGetValue(chapterId, out var worldData))
                return;

            world.SetActive(false);
            chapter.SetActive(true);

            if (stages.childCount > 0)
                Destroy(stages.GetChild(0).gameObject);

            _currentChapter = worldData.id;

            btnPrevChapter.interactable = _currentChapter > 1;
            btnNextChapter.interactable = _currentChapter < Tables.instance.World.Count;
            txtChapter.text = $"{_currentChapter} / {Tables.instance.World.Count}";
            txtStage.text = $"Stage {SelectedStage}";

            var res = Resources.Load<GameObject>($"UI/Prefabs/WorldMap/Chapter_{worldData.chapter}");
            if (!res)
                return;

            var stageData = Tables.instance.Stage.Values.ToList().GetEnumerator();
            stageData.MoveNext();

            var list = Instantiate(res, stages).transform;
            int maxStage = States.Instance.currentAvatarState.Value.worldStage;
            for (int i = 0; i < list.childCount; ++i)
            {
                var child = list.GetChild(i);
                var stage = child.GetComponent<WorldMapStage>();
                if (!stage)
                    continue;

                stage.Parent = this;
                stage.Value = worldData.stageBegin + i;
                stage.label.text = stage.Value.ToString();
                stage.icon.enabled = false;
                stage.button.enabled = maxStage >= stage.Value;

                while (stageData.Current != null
                       && !stageData.Current.isBoss)
                {
                    if (!stageData.MoveNext())
                        break;
                }
                if (stageData.Current != null
                    && stageData.Current.stage == stage.Value)
                {
                    stage.icon.enabled = true;
                    stageData.MoveNext();
                }

                if (SelectedStage == stage.Value)
                    stage.SetImage(stage.selectedImage);
                else if (maxStage > stage.Value)
                    stage.SetImage(stage.clearedImage);
                else if (!stage.button.enabled)
                    stage.SetImage(stage.disabledImage);
                else
                    stage.SetImage(stage.normalImage);

                stage.tweenMove.StartDelay = i * 0.08f;
                stage.tweenAlpha.StartDelay = i * 0.08f;
            }
        }
    }
}
