using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "BuffVFXData",
        menuName = "Scriptable Object/Buff VFX Data",
        order = int.MaxValue)]
    public class BuffVFXScriptableObject : ScriptableObject
    {
        public List<BuffVFXData> DataList;
        public List<BuffVFXOverrideData> OverrideDataList;
        public List<BuffPosOverrideData> BuffPosOverrideDataList;
        public Sprite FallbackIcon;
        public GameObject FallbackCastingVFX;
        public GameObject FallbackBuffVFX;
        public Vector3 FallbackPosition;

        [Serializable]
        public class BuffVFXData
        {
            public StatType StatType;
            public Sprite PlusIcon;
            public Sprite MinusIcon;
            public GameObject PlusCastingVFX;
            public GameObject MinusCastingVFX;
            public GameObject PlusVFX;
            public GameObject MinusVFX;
        }

        [Serializable]
        public class BuffVFXOverrideData
        {
            public int Id;
            public Sprite Icon;
            public GameObject CastingVFX;
            public GameObject BuffVFX;
        }

        [Serializable]
        public class BuffPosOverrideData
        {
            public int Id;
            public bool IsCasting;
            public Vector3 Position;
        }
    }
}
