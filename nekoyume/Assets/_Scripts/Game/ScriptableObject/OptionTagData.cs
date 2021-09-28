using System;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class OptionTagData
    {
        [SerializeField] private int grade;

        [Tooltip("Color range to affect hsv shift [0 ~ 1].")] [SerializeField] [Range(0, 1)]
        private float gradeHsvRange = 0.1f;

        [Header("Adjustment")] [Tooltip("Hue shift [-0.5 ~ 0.5].")] [SerializeField] [Range(-0.5f, 0.5f)]
        private float gradeHsvHue;

        [Tooltip("Saturation shift [-0.5 ~ 0.5].")] [SerializeField] [Range(-0.5f, 0.5f)]
        private float gradeHsvSaturation;

        [Tooltip("Value shift [-0.5 ~ 0.5].")] [SerializeField] [Range(-0.5f, 0.5f)]
        private float gradeHsvValue;

        public float GradeHsvValue => gradeHsvValue;

        public float GradeHsvSaturation => gradeHsvSaturation;

        public float GradeHsvHue => gradeHsvHue;

        public float GradeHsvRange => gradeHsvRange;

        public int Grade => grade;
    }
}
