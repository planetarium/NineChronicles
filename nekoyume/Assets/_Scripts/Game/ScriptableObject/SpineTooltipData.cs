using System;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class SpineTooltipData
    {
        [SerializeField] private int resourceID;

        [Tooltip("Spine object prefab to display.")] [SerializeField]
        private GameObject prefab;

        [Tooltip("Position of spine object.")] [SerializeField]
        private Vector3 position = new Vector3(6000f, 5999.5f, 2.15f);

        [Tooltip("Scale of spine object.")] [SerializeField]
        private Vector3 scale = new Vector3(1f, 1f, 1f);

        [Tooltip("Rotation of spine object.")] [SerializeField]
        private Vector3 rotation = Vector3.zero;

        [Tooltip("Color of grade effect.")] [SerializeField]
        private Color gradeColor = Color.white;

        public int ResourceID => resourceID;

        public GameObject Prefab => prefab;

        public Vector3 Position => position;

        public Vector3 Scale => scale;

        public Vector3 Rotation => rotation;

        public Color GradeColor => gradeColor;
    }
}
