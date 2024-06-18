using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using Nekoyume.Model.Skill;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "BuffVFXData", menuName = "Scriptable Object/Buff VFX Data", order = int.MaxValue)]
    public class BuffVFXScriptableObject : ScriptableObject
    {
        [field:SerializeField] public List<BuffVFXData> DataList { get; set; }
        [field:SerializeField] public List<BuffVFXOverrideData> OverrideDataList { get; set; }
        [field:SerializeField] public List<ActionBuffVFXOverrideData> ActionBuffVFXOverrideDataList { get; set; }
        
        [field:SerializeField] public List<BuffPosOverrideData>       BuffPosOverrideDataList       { get; set; }
        [field:SerializeField] public List<ActionBuffPosOverrideData> ActionBuffPosOverrideDataList { get; set; }
        
        [field:SerializeField] public Sprite     FallbackIcon       { get; set; }
        [field:SerializeField] public GameObject FallbackCastingVFX { get; set; }
        [field:SerializeField] public GameObject FallbackBuffVFX    { get; set; }
        [field:SerializeField] public Vector3    FallbackPosition   { get; set; }

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
        public class ActionBuffVFXOverrideData
        {
            public ActionBuffType Type;
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
        
        [Serializable]
        public class ActionBuffPosOverrideData
        {
            public ActionBuffType Type;
            public bool IsCasting;
            public Vector3 Position;
        }
    }
}
