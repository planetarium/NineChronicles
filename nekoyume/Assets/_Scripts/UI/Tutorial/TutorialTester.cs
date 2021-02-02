using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialTester : MonoBehaviour
    {
        [SerializeField] private List<TutorialTestData> data;
        [SerializeField] private Tutorial tutorial;
        [SerializeField] TextAsset preset;

        private readonly List<List<ITutorialData>> _testData = new List<List<ITutorialData>>();
        private int _testDataIndex = 0;
        private bool _isPlayingTester;


#if UNITY_EDITOR
        private void Awake()
        {
            GetData();
            if (tutorial == null)
            {
                Debug.LogError("Tutorial is null!");
                return;
            }

            AudioController.instance.Initialize();

            foreach (var d in data)
            {
                var list = new List<ITutorialData>();
                if (d.type == DataType.Play)
                {
                    list.Add(new GuideBackgroundData(d.IsExistFadeInBackground, d.IsEnableMask,
                        d.TargetImage));
                    list.Add(new GuideArrowData(d.ArrowType, d.TargetImage, d.IsSkipArrowAnimation));
                    list.Add(new GuideDialogData(d.EmojiType, d.CommaType, d.Script,
                        d.TargetImage.anchoredPosition.y, tutorial.NextButton));
                }

                _testData.Add(list);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (_testData.Count <= 0 || _isPlayingTester)
                {
                    return;
                }

                var list = _testData[_testDataIndex];
                if (list.Count > 0)
                {
                    _isPlayingTester = true;
                    tutorial.Play(list, () =>
                    {
                        _isPlayingTester = false;
                    });
                }
                else
                {
                    tutorial.Stop();
                }

                _testDataIndex++;
                if (_testDataIndex >= _testData.Count)
                {
                    _testDataIndex = 0;
                }
            }
        }
#endif

        private void GetData()
        {
            var json = preset.text;
            if (!string.IsNullOrEmpty(json))
            {
                var tutorialPreset = JsonUtility.FromJson<TutorialPreset>(json);
                foreach (var i in tutorialPreset.preset)
                {
                    int a = i.id;
                }
            }
        }
    }

    [Serializable]
    public class TutorialTestData
    {
        public DataType type;
        public RectTransform TargetImage;
        // bg
        public bool IsExistFadeInBackground;
        public bool IsEnableMask;
        // arrow
        public GuideType ArrowType;
        public bool IsSkipArrowAnimation;
        // dialog
        public DialogEmojiType EmojiType;
        public DialogCommaType CommaType;
        public string Script;
    }


    public enum DataType
    {
       Play,
       Stop,
    }
}
