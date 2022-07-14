using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume
{
    public class WorldBossScriptableObject : ScriptableObject
    {
        public List<MonsterData> MonsterDatas;

        [Serializable]
        public class GradeData
        {
            public WorldBossGrade grade;
            public Sprite icon;
        }

        [Serializable]
        public class MonsterData
        {
            public int id;
            public GameObject prefab;
        }
    }
}
