using System.Text.Json;
using Nekoyume.UI;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Tutorial
{
    public class TutorialDataTest
    {
        [Test]
        public void LoadJsonAndDeserialize()
        {
            string json = Resources.Load<TextAsset>("Tutorial/Data/TutorialScenario")?.text;
            Assert.DoesNotThrow(() => JsonSerializer.Deserialize<TutorialScenario>(json));
        }

        [Test]
        public void PresetSerialize()
        {
            Preset data = MakePreset();
            string json = JsonSerializer.Serialize(data);
            Preset fromJsonData = JsonSerializer.Deserialize<Preset>(json);
            Assert.AreEqual(data, fromJsonData);
        }

        [Test]
        public void TutorialPresetSerialize()
        {
            TutorialPreset data = MakeTutorialPreset(3);
            string json = JsonSerializer.Serialize(data);
            TutorialPreset fromJsonData = JsonSerializer.Deserialize<TutorialPreset>(json);
            Assert.AreEqual(data.preset.Length, fromJsonData.preset.Length);
            int count = data.preset.Length;
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(data.preset[i], fromJsonData.preset[i]);
            }
        }

        [Test]
        public void ScenarioDataSerialize()
        {
            var options = new JsonSerializerOptions()
            {
                MaxDepth = 0,
                IgnoreNullValues = true,
            };
            ScenarioData data = MakeScenarioData();
            string json = JsonSerializer.Serialize(data, options);
            ScenarioData fromJsonData = JsonSerializer.Deserialize<ScenarioData>(json);
            Assert.AreEqual(data, fromJsonData);
        }

        [Test]
        public void ScenarioSerialize()
        {
            Scenario data = MakeScenario();
            string json = JsonSerializer.Serialize(data);
            Scenario fromJsonData = JsonSerializer.Deserialize<Scenario>(json);
            Assert.AreEqual(data, fromJsonData);
        }

        [Test]
        public void TutorialScenarioSerialize()
        {
            TutorialScenario data = MakeTutorialScenario(3);
            string json = JsonSerializer.Serialize(data);
            TutorialScenario fromJsonData = JsonSerializer.Deserialize<TutorialScenario>(json);
            Assert.AreEqual(data.scenario.Length, fromJsonData.scenario.Length);
            int count = data.scenario.Length;
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(data.scenario[i], fromJsonData.scenario[i]);
            }
        }

        private static Preset MakePreset()
        {
            return new Preset
            {
                id = default,
                commaId = default,
                content = "",
                isEnableMask = default,
                isExistFadeInBackground = default,
                isSkipArrowAnimation = default,
            };
        }

        private static TutorialPreset MakeTutorialPreset(int count)
        {
            Preset[] preset = new Preset[count];
            for (int i = 0; i < count; i++)
            {
                preset[i] = MakePreset();
            }

            return new TutorialPreset
            {
                preset = preset,
            };
        }

        private static ScenarioData MakeScenarioData()
        {
            return new ScenarioData
            {
                actionType = default,
                emojiType = default,
                guideType = default,
                presetId = default,
                scriptKey = "",
                targetType = default,
                targetPositionOffset = default,
                targetSizeOffset = default,
            };
        }

        private static Scenario MakeScenario()
        {
            return new Scenario
            {
                data = MakeScenarioData(),
                id = default,
                nextId = default,
            };
        }

        private static TutorialScenario MakeTutorialScenario(int count)
        {
            Scenario[] scenario = new Scenario[count];
            for (int i = 0; i < count; i++)
            {
                scenario[i] = MakeScenario();
            }

            return new TutorialScenario
            {
                scenario = scenario,
            };
        }
    }
}
