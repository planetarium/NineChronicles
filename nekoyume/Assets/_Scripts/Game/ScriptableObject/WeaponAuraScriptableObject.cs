using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "VFX_WeaponAura", menuName = "Scriptable Object/Weapon Aura",
        order = int.MaxValue)]
    public class WeaponAuraScriptableObject : ScriptableObject
    {
        public List<WeaponAuraData> data;
    }

    [Serializable]
    public class WeaponAuraData
    {
        public int id;
        public List<GameObject> prefab;
    }
}
