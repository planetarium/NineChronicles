using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "RecipeViewData", menuName = "Scriptable Object/Recipe View Data")]
    public class RecipeViewData : UnityEngine.ScriptableObject
    {
        [Serializable]
        public class Data
        {
            [field: SerializeField]
            public int Grade { get; private set; }

            [field: SerializeField]
            public Sprite BgSprite { get; private set; }

            [field:SerializeField]
            public Material GradeEffectBgMaterial { get; private set; }

            [field: SerializeField]
            public Material LevelTextMaterial { get; private set; }

            [field: SerializeField]
            public Color LevelBgHsvTargetColor { get; private set; }

            [field: Tooltip("Color range to affect hsv shift [0 ~ 1].")]
            [field: SerializeField]
            [field: Range(0, 1)]
            public float LevelBgHsvRange { get; private set; } = 0.1f;

            [field: Header("Adjustment")]
            [field: Tooltip("Hue shift [-0.5 ~ 0.5].")]
            [field: SerializeField]
            [field: Range(-0.5f, 0.5f)]
            public float LevelBgHsvHue { get; private set; }

            [field: Tooltip("Saturation shift [-0.5 ~ 0.5].")]
            [field: SerializeField]
            [field: Range(-0.5f, 0.5f)]
            public float LevelBgHsvSaturation { get; private set; }

            [field: Tooltip("Value shift [-0.5 ~ 0.5].")]
            [field: SerializeField]
            [field: Range(-0.5f, 0.5f)]
            public float LevelBgHsvValue { get; private set; }
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
