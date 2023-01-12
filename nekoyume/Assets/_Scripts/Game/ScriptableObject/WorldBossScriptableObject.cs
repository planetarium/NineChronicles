using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_WorldBossData", menuName = "Scriptable Object/World Boss Data",
        order = int.MaxValue)]
    public class WorldBossScriptableObject : ScriptableObject
    {
        public List<MonsterData> Monsters;
        public List<GradeData> Grades;
        public List<GameObject> Rank;

        [Serializable]
        public class GradeData
        {
            public WorldBossGrade grade;
            public GameObject prefab;
            public GameObject smallPrefab;
        }

        [Serializable]
        public class MonsterData
        {
            public int id;
            public string name;
            public Sprite illustration;
            public GameObject namePrefab;
            public GameObject nameWithBackgroundPrefab;
            public GameObject spinePrefab;
            public GameObject backgroundPrefab;
            public string entranceMusicName;
            public string battleMusicName;
        }
    }
}
