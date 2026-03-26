using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    [Serializable]
    public class SecondaryAuraEntry
    {
        [Tooltip("장비 최고 등급이 이 값 이상일 때 VFX를 표시합니다.")]
        public int minGrade;

        [Tooltip("표시할 VFX 프리팹")]
        public GameObject prefab;
    }

    [CreateAssetMenu(fileName = "SecondaryAuraConfig",
        menuName = "Scriptable Object/SecondaryAuraConfig")]
    public class SecondaryAuraConfig : UnityEngine.ScriptableObject
    {
        [SerializeField] private List<SecondaryAuraEntry> entries;

        /// <summary>
        /// 장비/코스튬 중 최고 등급으로 조건을 평가하여
        /// 해당하는 VFX 프리팹을 반환합니다. 조건 미충족 시 null을 반환합니다.
        /// </summary>
        public GameObject GetPrefab(int maxGrade)
        {
            GameObject result = null;
            var bestGrade = -1;
            foreach (var entry in entries)
            {
                if (entry.minGrade <= maxGrade && entry.minGrade > bestGrade)
                {
                    bestGrade = entry.minGrade;
                    result = entry.prefab;
                }
            }

            return result;
        }
    }
}
