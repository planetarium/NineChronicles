using System.Collections.Generic;
using Nekoyume.UI;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Tutorial
{
    public class TutorialDataTest
    {
        [Test]
        public void ScenarioDataSerialize()
        {
            ScenarioData data = MakeScenarioData();
            string json = JsonUtility.ToJson(data);
            ScenarioData fromJsonData = JsonUtility.FromJson<ScenarioData>(json);
            Assert.AreEqual(data, fromJsonData);
        }

        [Test]
        public void ScenarioSerialize()
        {
            Scenario data = MakeScenario();
            string json = JsonUtility.ToJson(data);
            Scenario fromJsonData = JsonUtility.FromJson<Scenario>(json);
            Assert.AreEqual(data, fromJsonData);
        }

        [Test]
        public void TutorialScenarioSerialize()
        {
            TutorialScenario data = MakeTutorialScenario(3);
            string json = JsonUtility.ToJson(data);
            TutorialScenario fromJsonData = JsonUtility.FromJson<TutorialScenario>(json);
            Assert.AreEqual(data.scenario.Count, fromJsonData.scenario.Count);
            int count = data.scenario.Count;
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
            List<Scenario> scenarioArray = new List<Scenario>(count);
            for (int i = 0; i < count; i++)
            {
                scenarioArray.Add(MakeScenario());
            }

            return new TutorialScenario
            {
                scenario = scenarioArray,
            };
        }
    }
}
