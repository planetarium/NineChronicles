using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialTester : MonoBehaviour
    {
        [SerializeField] private List<TutorialTestData> data;
        [SerializeField] private Tutorial tutorial;
        [SerializeField] TextAsset preset;

        private readonly List<TutorialData> _testData = new List<TutorialData>();
        private readonly List<Preset> _preset = new List<Preset>();
        private int _testDataIndex = 0;
        private bool _isPlayingTester;


#if UNITY_EDITOR
        private void Awake()
        {
            GetPreset();
            if (tutorial == null)
            {
                Debug.LogError("Tutorial is null!");
                return;
            }

            AudioController.instance.Initialize();

            foreach (var d in data)
            {
                var list = new List<ITutorialData>();
                if (d.Type == DataType.Play)
                {
                    var preset = _preset.First(x => x.id == d.PresetId);
                    if (preset == null)
                    {
                        Debug.LogError($"Preset is not exist. ID : {d.PresetId}");
                        return;
                    }

                    list.Add(new GuideBackgroundData(preset.isExistFadeInBackground, preset.isEnableMask,
                        d.TargetImage));
                    list.Add(new GuideArrowData(d.ArrowType, d.TargetImage, preset.isSkipArrowAnimation));
                    list.Add(new GuideDialogData(d.EmojiType,
                        (DialogCommaType)preset.commaId, d.Script,
                        d.TargetImage? d.TargetImage.anchoredPosition.y : 0, tutorial.NextButton));
                }

                _testData.Add(new TutorialData(list, d.PresetId));
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
                if (list.data.Count > 0)
                {
                    _isPlayingTester = true;
                    tutorial.Play(list.data, list.presetId, () =>
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

        private void GetPreset()
        {
            var json = preset.text;
            if (!string.IsNullOrEmpty(json))
            {
                var tutorialPreset = JsonUtility.FromJson<TutorialPreset>(json);
                _preset.AddRange(tutorialPreset.preset);
            }
        }
    }

    public class TutorialData
    {
        public List<ITutorialData> data;
        public int presetId;

        public TutorialData(List<ITutorialData> data, int presetId)
        {
            this.data = data;
            this.presetId = presetId;
        }
    }


    [Serializable]
    public class TutorialTestData
    {
        public DataType Type;
        public RectTransform TargetImage;
        public int PresetId;
        public GuideType ArrowType;
        public DialogEmojiType EmojiType;
        public string Script;
    }

    public class StringEnumConverter
    {
    }


    public enum DataType
    {
       Play,
       Stop,
    }
}
