using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

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
            public Sprite cardSlotSprite;
            public Sprite soulStoneSprite;
            public SkeletonDataAsset spineDataAsset;
        }

        [Serializable]
        public struct PetUIPalette
        {
            public string key;
            public Color color;
        }

        [field:SerializeField]
        public List<PetRenderingData> PetRenderingDataList { get; private set; }

        [field:SerializeField]
        public List<PetUIPalette> PetUIPaletteList { get; private set; }
    }
}
