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
        public Sprite FallbackIcon;
        public GameObject FallbackCastingVFX;
        public GameObject FallbackBuffVFX;

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
    }
}
