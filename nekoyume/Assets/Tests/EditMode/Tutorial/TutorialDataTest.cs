using System.Text.Json;
using Nekoyume.UI;
using NUnit.Framework;

namespace Tests.EditMode.Tutorial
{
    public class TutorialDataTest
    {
        [Test]
        public void ScenarioDataSerialize()
        {
            ScenarioData data = MakeScenarioData();
            string json = JsonSerializer.Serialize(data);
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

        private ScenarioData MakeScenarioData()
        {
            return new ScenarioData
            {
                actionType = TutorialActionType.QuestClick,
                emojiType = DialogEmojiType.Idle,
                guideType = GuideType.Circle,
                presetId = 1,
                scriptKey = "",
                targetType = TutorialTargetType.CombinationButton,
            };
        }

        private Scenario MakeScenario()
        {
            return new Scenario
            {
                data = MakeScenarioData(),
                id = 1,
                nextId = 2,
            };
        }

        private TutorialScenario MakeTutorialScenario(int count)
        {
            Scenario[] scenarioArray = new Scenario[count];
            for (int i = 0; i < count; i++)
            {
                scenarioArray[i] = MakeScenario();
            }

            return new TutorialScenario
            {
                scenario = scenarioArray,
            };
        }
    }
}
