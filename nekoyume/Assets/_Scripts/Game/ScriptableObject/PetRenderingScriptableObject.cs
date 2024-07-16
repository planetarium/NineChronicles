using System;
using System.Collections.Generic;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "PetRenderingData",
        menuName = "Scriptable Object/Pet rendering data",
        order = int.MaxValue)]
    public class PetRenderingScriptableObject : ScriptableObject
    {
        [Serializable]
        public struct PetRenderingData
        {
            public int id;
            public Sprite soulStoneSprite;
            public SkeletonDataAsset spineDataAsset;
            public Vector3 localPosition;
            public Vector3 localScale;
            public float3 hsv;
        }

        [Serializable]
        public struct PetUIPalette
        {
            public string key;
            public Color color;
        }

        [field: SerializeField]
        public List<PetRenderingData> PetRenderingDataList { get; private set; }

        [field: SerializeField]
        public List<PetUIPalette> PetUIPaletteList { get; private set; }
    }
}
