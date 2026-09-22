using System;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class ItemViewData
    {
        [SerializeField] private int grade;
        [SerializeField] private Sprite gradeBackground;

        [Tooltip("아이콘 위에 덧그릴 장식 테두리. 장식이 없는 등급은 비워 둔다.")]
        [SerializeField] private Sprite gradeFrameOverlay;

        [Tooltip("Color range to affect hsv shift [0 ~ 1].")][SerializeField][Range(0, 1)]
        private float gradeHsvRange = 0.1f;

        [Header("Adjustment")][Tooltip("Hue shift [-0.5 ~ 0.5].")][SerializeField][Range(-0.5f, 0.5f)]
        private float gradeHsvHue;

        [Tooltip("Saturation shift [-0.5 ~ 0.5].")][SerializeField][Range(-0.5f, 0.5f)]
        private float gradeHsvSaturation;

        [Tooltip("Value shift [-0.5 ~ 0.5].")][SerializeField][Range(-0.5f, 0.5f)]
        private float gradeHsvValue;

        [SerializeField]
        private Material enhancementMaterial;

        [SerializeField]
        private Color itemGradeParticleColor;

        public Material EnhancementMaterial => enhancementMaterial;

        public float GradeHsvValue => gradeHsvValue;

        public float GradeHsvSaturation => gradeHsvSaturation;

        public float GradeHsvHue => gradeHsvHue;

        public float GradeHsvRange => gradeHsvRange;

        public Sprite GradeBackground => gradeBackground;

        /// <summary>
        /// 등급 배경에 장식이 있는 경우, 그 장식만 남긴 스프라이트. 없으면 null.
        /// </summary>
        public Sprite GradeFrameOverlay => gradeFrameOverlay;

        public Color ItemGradeParticleColor => itemGradeParticleColor;

        public int Grade => grade;
    }
}
