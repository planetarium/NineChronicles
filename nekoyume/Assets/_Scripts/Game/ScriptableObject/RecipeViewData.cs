using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "RecipeViewData", menuName = "Scriptable Object/Recipe View Data")]
    public class RecipeViewData : UnityEngine.ScriptableObject
    {
        [Serializable]
        public struct Data
        {
            [field: SerializeField]
            public int Grade { get; private set; }

            [field: SerializeField]
            public Sprite BgSprite { get; private set; }
        }

        [SerializeField]
        private List<Data> datas = null;

        [SerializeField]
        private int fallbackGrade;

        private Dictionary<int, Data> _dataMap = null;

        private void OnEnable()
        {
            _dataMap = new Dictionary<int, Data>();
            foreach (var data in datas)
            {
                _dataMap[data.Grade] = data;
            }
        }

        public Data GetData(int grade)
        {
            if (_dataMap.TryGetValue(grade, out var data))
            {
                return data;
            }

            return _dataMap[fallbackGrade];
        }
    }
}
